using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using LiteDB;
using Discord.Addons.CommandCache;
using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.WebSocket;
using Discord.Rest;
using Discord;
using System.Net;
using System.Globalization;
using SAIL.Classes;
using System.Text;

namespace SAIL_DiscordBot.Modules
{
    [Name("Schedule Module")]
    [Summary("This module allows for the use of the Weekly Scheduler. It requires a channel to be set as the Notification Channel and for said function to be turned on. Users need the Manage Roles permission in order to use the add/remove event commands.")]
    public class ScheduleModule : InteractiveBase<SocketCommandContext>
    {
        public LiteDatabase Database {get;set;}
        public CommandCacheService Cache {get;set;}

        [Command("CreateEvent"), Alias("NewEvent","AddEvent")]
        [RequireGuildSettings]
        [RequireContext(ContextType.Guild), RequireUserPermission(GuildPermission.ManageRoles)]
        [Summary("Creates a new event and adds to it to the Guild Calendar.")]
        public async Task NewEvent([Remainder] string EventName)
        {
            var col = Database.GetCollection<SysGuild>("Guilds");
            var guild = col.FindOne(x=>x.Id == Context.Guild.Id);
            
            if(guild.Events.Exists(x=>x.Name.ToLower()==EventName.ToLower()))
            {
                var msg2 = await ReplyAsync("There's an event with that exact name already.");
                Cache.Add(Context.Message.Id,msg2.Id);
                return;
            }
            var evnt = new GuildEvent(){Name = EventName};
            var msg = await ReplyAsync("What's the event's Description? Please Reply with the descroption of event.");
            var reply = await NextMessageAsync(true,true,TimeSpan.FromMinutes(3));
            evnt.Description = reply.Content;
            await reply.DeleteAsync();
            bool Lock = true;
            
            await msg.ModifyAsync(x=>x.Content="Does this event repeat on a weekly basis?");
            await msg.AddReactionsAsync(new Emoji[]{new Emoji("✅"),new Emoji("❎")});
            Interactive.AddReactionCallback(msg,new InlineReactionCallback(Interactive,Context,
                new ReactionCallbackData("",null,true,true,TimeSpan.FromMilliseconds(3),(c) => Task.Run(()=>{msg.ModifyAsync(x=>x.Content="You took too long to respond and the process has been terminated.").Start();return;}))
                .WithCallback(new Emoji("✅"),(c,r) => Task.Run( () => {Lock=false; evnt.OneTime=false;}))
                .WithCallback(new Emoji("❎"),(c,r) => Task.Run( () => {Lock=false; evnt.OneTime=false;}))
                )
            );
            while(Lock)
            {
                await Task.Delay(1000);   
            }
            
        }
    }
}