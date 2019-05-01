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
using System.Threading;

namespace SAIL.Modules
{
    [Name("Schedule Module")]
    [Summary("This module allows for the use of the Weekly Scheduler. It requires a channel to be set as the Notification Channel and for said function to be turned on. Users need the Manage Roles permission in order to use the add/remove event commands.")]
    public class ScheduleModule : InteractiveBase<SocketCommandContext>
    {
        public CommandCacheService Cache {get;set;}
        private readonly System.Threading.EventWaitHandle waitHandle = new System.Threading.AutoResetEvent(false);

        [Command("CreateEvent"), Alias("NewEvent","AddEvent")]
        [RequireGuildSettings]
        [RequireContext(ContextType.Guild), RequireUserPermission(GuildPermission.ManageRoles)]
        [Summary("Creates a new event and adds to it to the Guild Calendar.")]
        public async Task NewEvent([Remainder] string EventName)
        {
            var col = Program.Database.GetCollection<SysGuild>("Guilds");
            var guild = col.FindOne(x=>x.Id == Context.Guild.Id);
            
            if(guild.Events.Exists(x=>x.Name.ToLower()==EventName.ToLower()))
            {
                var msg2 = await ReplyAsync("There's an event with that exact name already.");
                Cache.Add(Context.Message.Id,msg2.Id);
                return;
            }
            Menu.MenuOption[] Options = new Menu.MenuOption[]
            {
                new Menu.MenuOption("Set Event Description",
                (Menu,index) =>
                {
                    var prompt = Menu.Context.Channel.SendMessageAsync("Please type and send the event's description now.").GetAwaiter().GetResult();
                    var reply = Menu.Interactive.NextMessageAsync(Menu.Context).GetAwaiter().GetResult();
                    prompt.DeleteAsync().RunSynchronously();
                    ((GuildEvent)Menu.Storage).Description = reply.Content;
                    reply.DeleteAsync().RunSynchronously();
                    Menu.Options[index].Description = ((GuildEvent)Menu.Storage).Description;
                    return null;
                },"No Description set",false),
                new Menu.MenuOption("Change Repeat Frequency",
                (Menu,Index)=>
                {
                    switch (((GuildEvent)Menu.Storage).Repeating)
                    {
                        case RepeatingState.Once:
                            ((GuildEvent)Menu.Storage).Repeating = RepeatingState.Weekly;
                        break;
                        case RepeatingState.Weekly:
                            ((GuildEvent)Menu.Storage).Repeating = RepeatingState.Monhtly;
                            break;
                        case RepeatingState.Monhtly:
                            ((GuildEvent)Menu.Storage).Repeating = RepeatingState.Anually;
                            break;
                        case RepeatingState.Anually:
                            ((GuildEvent)Menu.Storage).Repeating = RepeatingState.Once;
                            break;
                    }
                    Menu.Options[Index].Description = ((GuildEvent)Menu.Storage).Repeating.ToString();
                    return null;
                },RepeatingState.Once.ToString(),false),
                new Menu.MenuOption("Set the Date",
                (Menu,Index)=>
                {
                    var prompt = Menu.Context.Channel.SendMessageAsync("Please type the event's Date in the corresponding to the event type:"+
                    "\nOnce: \"18:24 27/Febuary/2019\"."+
                    "\nWeekly: \"18:24 Monday\"."+
                    "\nMonthly: ").GetAwaiter().GetResult();
                    var reply = Menu.Interactive.NextMessageAsync(Menu.Context).GetAwaiter().GetResult();
                    prompt.DeleteAsync().RunSynchronously();
                    
                    return null;
                },DateTime.UtcNow.ToString("hh:mm tt DD/MM/YY"),false)
            };
        }
    }
}