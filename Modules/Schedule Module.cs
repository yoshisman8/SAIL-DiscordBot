// using System;
// using System.Linq;
// using System.Collections.Generic;
// using System.Threading.Tasks;
// using LiteDB;
// using Discord.Addons.CommandCache;
// using Discord.Addons.Interactive;
// using Discord.Commands;
// using Discord.WebSocket;
// using Discord.Rest;
// using Discord;
// using System.Net;
// using System.Globalization;
// using SAIL.Classes;
// using System.Text;
// using System.Threading;

// namespace SAIL.Modules
// {
//     [Name("Schedule Module")]
//     [Summary("This module allows for the use of the Weekly Scheduler. It requires a channel to be set as the Notification Channel and for said function to be turned on. Users need the Manage Roles permission in order to use the add/remove event commands.")]
//     public class ScheduleModule : InteractiveBase<SocketCommandContext>
//     {
//         public LiteDatabase Database {get;set;}
//         public CommandCacheService Cache {get;set;}
//         private readonly System.Threading.EventWaitHandle waitHandle = new System.Threading.AutoResetEvent(false);

//         [Command("CreateEvent"), Alias("NewEvent","AddEvent")]
//         [RequireGuildSettings]
//         [RequireContext(ContextType.Guild), RequireUserPermission(GuildPermission.ManageRoles)]
//         [Summary("Creates a new event and adds to it to the Guild Calendar.")]
//         public async Task NewEvent([Remainder] string EventName)
//         {
//             var col = Database.GetCollection<SysGuild>("Guilds");
//             var guild = col.FindOne(x=>x.Id == Context.Guild.Id);
            
//             if(guild.Events.Exists(x=>x.Name.ToLower()==EventName.ToLower()))
//             {
//                 var msg2 = await ReplyAsync("There's an event with that exact name already.");
//                 Cache.Add(Context.Message.Id,msg2.Id);
//                 return;
//             }
//             var evnt = new GuildEvent(){Name = EventName};
//             var msg = await ReplyAsync("What's the event's Description? Please Reply with the descroption of event.");
//             SocketMessage reply = null;
//             SpinWait.SpinUntil(()=>
//             {
//                 reply = NextMessageAsync(true,true,TimeSpan.FromMinutes(3)).GetAwaiter().GetResult();
//                 if(reply.Content != "") return false;
//                 else return true;
//             }
//             );
//             evnt.Description = reply.Content;
//             if(Context.Guild.CurrentUser.Roles.ToList().Exists(x=>x.Permissions.ManageMessages))
//                 await reply.DeleteAsync();
//             await msg.ModifyAsync(x=>x.Content="Does this event repeat on a weekly basis?");
//             await msg.AddReactionsAsync(new Emoji[]{new Emoji("✅"),new Emoji("❎")});

//             Interactive.AddReactionCallback(msg,new InlineReactionCallback(Interactive,Context,
//             new ReactionCallbackData("",null,true,true,TimeSpan.FromMilliseconds(3))
//                 .WithCallback(new Emoji("✅"),(c,r) => Task.Run( async() => 
//                     {
//                         evnt.Repeating=RepeatingState.Repeating;
//                         await msg.RemoveAllReactionsAsync();
//                     }))
//                 .WithCallback(new Emoji("❎"),(c,r) => Task.Run( async() => 
//                     {
//                         evnt.Repeating=RepeatingState.NonRepeating;
//                         await msg.RemoveAllReactionsAsync();
//                     }))
//                 )
//             );
//             SpinWait.SpinUntil(() => evnt.Repeating != RepeatingState.Unset,TimeSpan.FromMinutes(3));
//             Interactive.RemoveReactionCallback(msg);
//             if (evnt.Repeating==RepeatingState.Unset) return;
//             if(evnt.Repeating==RepeatingState.NonRepeating)
//             {
//                 guild.Events.Add(evnt);
//                 col.Update(guild);
//             }
//             await msg.ModifyAsync(x=>x.Content="");
//         }
//     }
// }