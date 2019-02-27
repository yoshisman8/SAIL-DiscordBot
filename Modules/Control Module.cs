using System;
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
    [Name("Control Module")]
    public class ControlModule : InteractiveBase<SocketCommandContext>
    {
        public LiteDatabase Database {get;set;}
        public CommandCacheService Cache {get;set;}
        public CommandService command {get;set;}

        [Command("AdminPanel"),Alias("Panel","Config")]
        [RequireContext(ContextType.Guild)] [RequireUserPermission(GuildPermission.ManageGuild)]
        public async Task ConfigPanel()
        {
            var col = Database.GetCollection<SysGuild>("Guilds");
            var guild = col.FindOne(x=>x.Id == Context.Guild.Id);
            guild.Load(Context);

            var msg = await ReplyAsync("",embed: guild.GetSettingsPage(command));
            Cache.Add(Context.Message.Id,msg.Id);
        }
        [Command("Prefix"),Alias("SetPrefix")]
        [RequireContext(ContextType.Guild)] [RequireUserPermission(GuildPermission.ManageGuild)]
        public async Task SetPrefix([Remainder] string prefix)
        {
            var col = Database.GetCollection<SysGuild>("Guilds");
            var guild = col.FindOne(x=>x.Id == Context.Guild.Id);
            guild.Prefix = prefix;
            col.Update(guild);
            var msg = await ReplyAsync("The prefix for all commands in this server has been changed to `"+prefix+"`.");
            Cache.Add(Context.Message.Id,msg.Id);
        }
    }
}