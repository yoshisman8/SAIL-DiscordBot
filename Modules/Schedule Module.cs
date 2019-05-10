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
            var col = Program.Database.GetCollection<GuildEvent>("Events").IncludeAll();
            var guild = Program.Database.GetCollection<SysGuild>().FindOne(x=>x.Id == Context.Guild.Id);
            
            if(col.Exists(x=>x.Name==EventName.ToLower()&& x.Server.Id==guild.Id))
            {
                var msg2 = await ReplyAsync("There's an event with that exact name already.");
                Cache.Add(Context.Message.Id,msg2.Id);
                return;
            }
            Menu.MenuOption[] Options = new Menu.MenuOption[]
            {
                new Menu.MenuOption("Set Event Description",
                async (Menu,index) =>
                {
                    var prompt = await Menu.Context.Channel.SendMessageAsync("Please type and send the event's description now.");
                    var reply = await Menu.Interactive.NextMessageAsync(Menu.Context);
                    await prompt.DeleteAsync();
                    ((GuildEvent)Menu.Storage).Description = reply.Content;
                    await reply.DeleteAsync();
                    Menu.Options[index].Description = ((GuildEvent)Menu.Storage).Description;
                    return null;
                },"No Description set",false),
                new Menu.MenuOption("Change Repeat Frequency",
                async (Menu,Index)=>
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
                async (Menu,Index)=>
                {
                    var prompt = await Menu.Context.Channel.SendMessageAsync("Please type the event's Date in the corresponding to the event type:"+
                    "\nOnce: \"18:15 27/Febuary/2019\"."+
                    "\nWeekly: \"10:00 Monday\"."+
                    "\nMonthly: \"5:30 20\"."+
                    "\nYearly: \"20:45 November 15");
                    var reply = await Menu.Interactive.NextMessageAsync(Menu.Context);
                    await prompt.DeleteAsync();
                    switch (((GuildEvent)Menu.Storage).Repeating)
                    {
                        case RepeatingState.Once:
                            ((GuildEvent)Menu.Storage).Date = DateTime.ParseExact(reply.Content,"H:mm dd/MMMM/yyyy",null).RoundUp(TimeSpan.FromMinutes(15));
                            Menu.Options[Index].Description = ((GuildEvent)Menu.Storage).Date.ToString("hh:mm tt dd/MMM/yyyy");
                        break;
                        case RepeatingState.Weekly:
                            ((GuildEvent)Menu.Storage).Date = DateTime.ParseExact(reply.Content,"H:mm DDDD",null).RoundUp(TimeSpan.FromMinutes(15));
                            Menu.Options[Index].Description = ((GuildEvent)Menu.Storage).Date.ToString("hh:mm tt DDDD");
                            break;
                        case RepeatingState.Monhtly:
                            ((GuildEvent)Menu.Storage).Date = DateTime.ParseExact(reply.Content,"H:m d",null).RoundUp(TimeSpan.FromMinutes(15));
                            Menu.Options[Index].Description = ((GuildEvent)Menu.Storage).Date.ToString("hh:mm tt")+" on the "+((GuildEvent)Menu.Storage).Date.Day.ToPlacement()+" of every month.";
                            break;
                        case RepeatingState.Anually:
                            ((GuildEvent)Menu.Storage).Date = DateTime.ParseExact(reply.Content,"HH:mm dd/MMMM",null).RoundUp(TimeSpan.FromMinutes(15));
                            Menu.Options[Index].Description = ((GuildEvent)Menu.Storage).Date.ToString("HH:mm tt MMMM dd");
                            break;
                    }
                    return null;
                },DateTime.UtcNow.ToString("hh:mm tt DD/MM/YY"),false),
                new Menu.MenuOption("Save Changes",
                async (Menu,index)=>
                {
                    return Menu.Storage;
                },"Save the event it is being shown right now."),
                new Menu.MenuOption("Discard and Exit",
                async (Menu,Index)=>
                {
                    return null;
                },"Discard this event and cancel scheduling.",true)
            };

            var ScheduleMenu = new Menu("Creating event \""+EventName+"\"",
            "Use the options bellow to create your new event.\n"+
            "In order to ensure bot functionality, events are bound to 15 min intervals (4:15, 12:30, 8:45, 10:00, etc).\n"+
            "Current UTC time: "+DateTime.UtcNow.ToString("hh:mm tt dd/mm/yyyy")+
            "[__Timezone Converter__](https://www.timeanddate.com/worldclock/converter.html?iso=20190510T200000&p1=1440)",Options,new GuildEvent(){Name=EventName});
            var result = (GuildEvent)await ScheduleMenu.StartMenu(Context,Interactive);
            if (result == null) return;
            else 
            {
                col.Insert(result);
                var msg = await ReplyAsync("Successfully created event \""+result.Name+"\"!");
                Cache.Add(Context.Message.Id,msg.Id);
            }
        }
    }
}