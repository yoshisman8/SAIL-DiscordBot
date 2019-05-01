﻿using System;
using System.Net;
using System.Text;
using System.Linq;
using System.Globalization;
using System.Threading.Tasks;
using System.Collections.Generic;
using LiteDB;
using SAIL.Classes;
using Discord.Addons.CommandCache;
using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.WebSocket;
using Discord.Rest;
using Discord;

namespace SAIL.Modules
{
    [Name("Administrative Module")] [Untoggleable]
    [Summary("This module contains a series of Administrative commands for the bot. It cannot be dissabled.")]
    public class AdministrativeModule : InteractiveBase<SocketCommandContext>
    {
        public LiteDatabase Database {get;set;}
        public CommandCacheService Cache {get;set;}
        public IServiceProvider Provider {get;set;}
        public CommandService command {get;set;}
        private Controller Controller {get;set;} = new Controller();

        [Command("AdminPanel"),Alias("Panel","Config")]
        [Summary("Shows the current settings and what modules are on or off")]
        [RequireContext(ContextType.Guild)] [RequireUserPermission(GuildPermission.ManageGuild)]
        [RequireGuildSettings]
        public async Task ConfigPanel()
        {
            var col = Database.GetCollection<SysGuild>("Guilds");
            var guild = col.FindOne(x=>x.Id == Context.Guild.Id);
            guild.Load(Context);

            var msg = await ReplyAsync("",embed: guild.GetSettingsPage(command));
            Cache.Add(Context.Message.Id,msg.Id);
        }
        [Command("Prefix"),Alias("SetPrefix")]
        [Summary("Change the prefix that will be used for this server. Set to `!` by default. Additionally, you can also mention the bot instead of using a prefix (cannot be disabled).")]
        [RequireContext(ContextType.Guild)] [RequireUserPermission(GuildPermission.ManageGuild)]
        [RequireGuildSettings]
        public async Task SetPrefix([Remainder] string prefix)
        {
            var col = Database.GetCollection<SysGuild>("Guilds");
            var guild = col.FindOne(x=>x.Id == Context.Guild.Id);
            guild.Prefix = prefix;
            col.Update(guild);
            var msg = await ReplyAsync("The prefix for all commands in this server has been changed to `"+prefix+"`.");
            Cache.Add(Context.Message.Id,msg.Id);
        }
        [Command("ToggleModule"),Alias("TgglModule","ToggleM","Toggle")]
        [Summary("Toggles a module On or Off, The Administrative Module cannot be toggled Off. You can find the names of the modules by using the AdminPanel command.")]
        [RequireContext(ContextType.Guild)] [RequireUserPermission(GuildPermission.ManageGuild)]
        [RequireGuildSettings]
        public async Task ToggleModule([Remainder] ModuleInfo ModuleName)
        {
            var col = Database.GetCollection<SysGuild>("Guilds");
            var guild = col.FindOne(x=>x.Id == Context.Guild.Id);
            int index = guild.CommandModules.FindIndex(x => x.Name == ModuleName.Name);
            guild.CommandModules[index].Value ^= true;
            col.Update(guild);

            var msg = await ReplyAsync("The "+ModuleName.Name+" has been toggled **"+(guild.CommandModules[index].Value?"On":"Off")+"**.");
            Cache.Add(Context.Message.Id,msg.Id);
            return;
        }
        [Command("Help")][Alias("Commands")]
        [Summary("Shows all commands along with their parameters, aliases and information.")]
        [RequireGuildSettings]
        [RequireContext(ContextType.Guild)]
        public async Task GetHelp()
        {
            var col = Database.GetCollection<SysGuild>("Guilds");
            var guild = col.FindOne(x=>x.Id == Context.Guild.Id);
            guild.Load(Context);
            foreach (var x in guild.CommandModules.Where(x=>x.Value == true))
            {
                var usr = Context.User as SocketGuildUser;
                if (x.Name == "Administrative Module" &&
                    usr.Roles.Where(y=>y.Permissions.ManageGuild == true).Count() == 0) 
                    continue;
            }
            var prev = new Emoji("⏮");
            var kill = new Emoji("⏹");
            var next = new Emoji("⏭");
            var msg = await ReplyAsync(Context.User.Mention+", Here are all Available Commands you can use.");
            Interactive.AddReactionCallback(msg,new InlineReactionCallback(Interactive,Context,
            new ReactionCallbackData("",null,false,false,TimeSpan.FromMinutes(3))
                .WithCallback(prev,(ctx,rea)=>Controller.Previous(ctx,rea,msg))
                .WithCallback(kill,(ctx,rea)=>Controller.Kill(Interactive,msg))
                .WithCallback(next,(ctx,rea)=>Controller.Next(ctx,rea,msg))));
            await msg.ModifyAsync(x=> x.Embed = Controller.Pages.ElementAt(Controller.Index));
            await msg.AddReactionAsync(prev);
            await msg.AddReactionAsync(kill);
            await msg.AddReactionAsync(next);
            Cache.Add(Context.Message.Id,msg.Id);
        }
        [Command("ToggleListMode"),Alias("ToggleBlacklist","ToggleWhitelist")]
        [RequireGuildSettings]
        [RequireContext(ContextType.Guild)] [RequireUserPermission(GuildPermission.ManageGuild)]
        [Summary("Cycles between the Server channel list modes. This command will cycle between None, Blacklist and Whitelist (in that order)")]
        public async Task Cyclemode()
        {
            var col = Database.GetCollection<SysGuild>("Guilds");
            var guild = col.FindOne(x=>x.Id == Context.Guild.Id);
            
            switch (guild.ListMode)
            {
                case ListMode.None:
                guild.ListMode = ListMode.Blacklist;
                break;
                case ListMode.Blacklist:
                guild.ListMode = ListMode.Whitelist;
                break;
                case ListMode.Whitelist:
                guild.ListMode = ListMode.None;
                break;
            }
            col.Update(guild);
            if(guild.ListMode!= ListMode.None)
            {
                var msg = await ReplyAsync("The server is now "+(guild.ListMode == ListMode.Blacklist?"Blacklisting":"Whitelisting")+" the channels in the Channel List. Use "+guild.Prefix+"AddChannel and "+guild.Prefix+"RemChannel to add or remove channels from the "+guild.ListMode+".");
                Cache.Add(Context.Message.Id,msg.Id);   
            }
        }
        [Command("AddChannel")]
        [RequireGuildSettings]
        [RequireContext(ContextType.Guild)] [RequireUserPermission(GuildPermission.ManageGuild)]
        [Summary("Cycles between the Server channel list modes. This command will cycle between None, Blacklist and Whitelist (in that order)")]
        public async Task Addtolist(ITextChannel Channel)
        {
            var col = Database.GetCollection<SysGuild>("Guilds");
            var guild = col.FindOne(x=>x.Id == Context.Guild.Id);
            
            if(!guild.Channels.Contains(Channel.Id))
            {
                guild.Channels.Add(Channel.Id);
                col.Update(guild);
                var msg = await ReplyAsync("The channel "+Channel.Mention+" has been added to the List of Blacklisted/Whitelisted channels.");
                Cache.Add(Context.Message.Id,msg.Id);
                return;
            }
            else
            {
                var msg = await ReplyAsync("The channel "+Channel.Mention+" is already on the list.");
                Cache.Add(Context.Message.Id,msg.Id);
                return;
            }
        }
        [Command("RemChannel"),Alias("RemoveChannel")]
        [RequireGuildSettings]
        [RequireContext(ContextType.Guild)] [RequireUserPermission(GuildPermission.ManageGuild)]
        [Summary("Cycles between the Server channel list modes. This command will cycle between None, Blacklist and Whitelist (in that order)")]
        public async Task Remtolist(ITextChannel Channel)
        {
            var col = Database.GetCollection<SysGuild>("Guilds");
            var guild = col.FindOne(x=>x.Id == Context.Guild.Id);
            
            if(guild.Channels.Contains(Channel.Id))
            {
                guild.Channels.Remove(Channel.Id);
                col.Update(guild);
                var msg = await ReplyAsync("The channel "+Channel.Mention+" has been removed from the List of Blacklisted/Whitelisted channels.");
                Cache.Add(Context.Message.Id,msg.Id);
                return;
            }
            else
            {
                var msg = await ReplyAsync("The channel "+Channel.Mention+" is sn't on the channel list.");
                Cache.Add(Context.Message.Id,msg.Id);
                return;
            }
        }
        [Command("ToggleNotifications"),Alias("ToggleNotif","TgglNotif")]
        [RequireGuildSettings]
        [RequireContext(ContextType.Guild)] [RequireUserPermission(GuildPermission.ManageGuild)]
        [Summary("Toggles whether or not this server shows Notifications on the Notification channel. Admins can set the channel with `SetNotifChannel <Channel Name>`.")]
        public async Task ToggleNotif()
        {
            var col = Database.GetCollection<SysGuild>("Guilds");
            var guild = col.FindOne(x=>x.Id == Context.Guild.Id);
            
            guild.Notifications = !guild.Notifications;
            col.Update(guild);
            var msg = await ReplyAsync("Bot Notifications are now turned **"+(guild.Notifications?"On":"Off")+"**.");
            Cache.Add(Context.Message.Id,msg.Id);
        }
        [Command("SetNotificationChannel"),Alias("SetNotifChannel","SetNotif")]
        [RequireGuildSettings] 
        [RequireContext(ContextType.Guild),RequireUserPermission(GuildPermission.ManageGuild)]
        [Summary("Sets the Channel to be used for Notification Messages (User Joined/Left, Scheduled Messages, Raffles, Etc)")]
        public async Task SetNotifChannel(ITextChannel Channel)
        {
            var col = Database.GetCollection<SysGuild>("Guilds");
            var guild = col.FindOne(x=>x.Id == Context.Guild.Id);
            guild.NotificationChannel = Channel.Id;
            col.Update(guild);
            var msg = await ReplyAsync("Server notifications are now going to be sent to "+Channel.Mention);
        }
        private async Task<Embed> GenerateEmbedPage(SocketCommandContext ctx, CommandService cmd,IServiceProvider _provider, Module _module,SysGuild guild)
        {
            
            var module = command.Modules.Single(y => y.Name == _module.Name);
                var embed = new EmbedBuilder()
                .WithTitle("Commands Available on "+Context.Guild)
                .WithDescription("Parameters surounded by [] are optional, Parameters surrounded by <> are mandatory."
                    +"\nOnly commands you can use will be shown.")
                .WithThumbnailUrl(Context.Guild.IconUrl)
                .AddField(module.Name,module.Summary,false);
            foreach (var c in module.Commands)
            {
                var result = await c.CheckPreconditionsAsync(ctx,_provider);
                if (!result.IsSuccess) continue;
                
                string arguments = "";
                if(c.Parameters.Count > 0) {
                    foreach(var p in c.Parameters){
                    arguments += p.IsOptional? "["+p.Name+"] ":"<"+p.Name+"> ";
                    }
                }
                embed.AddField(guild.Prefix+c.Name+" "+arguments,(c.Aliases.Count > 0 ? "Aliases: "+string.Join(",",c.Aliases)+"\n":"")+c.Summary,true);
            }
            return embed.Build();
        }
    }
}