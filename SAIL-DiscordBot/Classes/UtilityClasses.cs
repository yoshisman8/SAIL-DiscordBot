using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using LiteDB;
using Discord.Addons.CommandCache;
using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.WebSocket;
using Discord.Rest;
using Discord;
using System.Net;
using System.Globalization;
using SAIL.Modules;
using System.Text;

namespace SAIL.Classes
{
    public static class StaticMethods
    {
        public static bool IsImageUrl(this string URL)
        {
            var req = (HttpWebRequest)HttpWebRequest.Create(URL);
            req.Method = "HEAD";
            using (var resp = req.GetResponse())
            {
                return resp.ContentType.ToLower(CultureInfo.InvariantCulture)
                        .StartsWith("image/");
            }
        }
        public static bool NullorEmpty(this string _string)
        {
            if(_string == null) return true;
            if(_string =="") return true;
            else return false;
        }
        public static string ToRoman(this int number)
        {
            if ((number < 0) || (number > 3999)) throw new ArgumentOutOfRangeException("insert value betwheen 1 and 3999");
            if (number < 1) return string.Empty;
            if (number >= 1000) return "M" + ToRoman(number - 1000);
            if (number >= 900) return "CM" + ToRoman(number - 900); //EDIT: i've typed 400 instead 900
            if (number >= 500) return "D" + ToRoman(number - 500);
            if (number >= 400) return "CD" + ToRoman(number - 400);
            if (number >= 100) return "C" + ToRoman(number - 100);
            if (number >= 90) return "XC" + ToRoman(number - 90);
            if (number >= 50) return "L" + ToRoman(number - 50);
            if (number >= 40) return "XL" + ToRoman(number - 40);
            if (number >= 10) return "X" + ToRoman(number - 10);
            if (number >= 9) return "IX" + ToRoman(number - 9);
            if (number >= 5) return "V" + ToRoman(number - 5);
            if (number >= 4) return "IV" + ToRoman(number - 4);
            if (number >= 1) return "I" + ToRoman(number - 1);
            throw new ArgumentOutOfRangeException("something bad happened");
        }
        public static Embed EmbedMessage(SocketCommandContext context, SocketTextChannel channel, IUserMessage message)
        {
            if (channel == null) throw new Exception("Channel not found. It might have been deleted or I may no longer have the \"Read Message\" and \"Read Message History\" Permissions.");
            if (message == null) throw new Exception("Message not found. It might have been deleted or I may no longer have the \"Read Message History\" permission");
            
            if(channel.IsNsfw == true && (context.Channel as SocketTextChannel).IsNsfw == false)
            return new EmbedBuilder()
                .WithAuthor(context.Client.CurrentUser)
                .WithDescription("This quote is NSFW so it cannot be displayed here!")
                .Build();

            var embed = new EmbedBuilder()
                .WithTitle("SAIL Message Storage System")
                .WithTimestamp(message.Timestamp)
                .WithFooter("On #"+channel.Name)
                .WithUrl(message.GetJumpUrl());
            if (message.Content != "")
            {
                embed.WithDescription(message.Content);
            }
            if(message.Author == null)
            {
                embed.WithAuthor("[User out of Reach]");
            }
            else
            {
                embed.WithAuthor(message.Author);
            }
            if (message.Attachments.Count > 0)
            {
                if (message.Attachments.ToList().Exists(x=>x.Url.IsImageUrl()))
                {
                    embed.WithImageUrl(message.Attachments.First().Url);
                }
                else
                {
                    foreach (var x in message.Attachments.Take(10))
                    {
                        embed.AddField(x.Filename,"[Download]("+x.Url+")",true);
                    }
                }
            }
            if(message.Reactions.Where(x=> x.Key.Name != "📌"&& x.Key.Name != "🔖").Count() > 0)
            {
                var sb = new StringBuilder();
                foreach (var x in message.Reactions.Where(x=> x.Key != new Emoji("📌")))
                {
                    sb.Append("["+x.Key+" ("+x.Value.ReactionCount+")] ");
                }
                embed.AddField("Reactions",sb.ToString());
            }
            return embed.Build();
        }
        public static string ToPlacement(this int number)
        {
            string text;
            switch (Math.Abs(number))
            {
                case 1:
                    text = number+"st";
                    break;
                case 2:
                    text = number+"nd";
                    break;
                case 3:
                    text = number+"rd";
                    break;
                default:
                    text = number+"th";
                    break;
            }
            return text;
        }
    }
    public static class DateTimeExtension 
    {
        public static DateTime RoundUp(this DateTime dt, TimeSpan d)
        {
            return new DateTime(((dt.Ticks + d.Ticks - 1) / d.Ticks) * d.Ticks, dt.Kind);
        }
        public static HourOfTheDay ToHourOfTheDay(this DateTime dt)
        {
            return new HourOfTheDay(){Hours=dt.Hour,Minutes=dt.Minute,Seconds=dt.Second};
        }
        public static DayOfWeek GetDayOfWeek(this DateTime dt)
        {
            DayOfWeek day;
            switch(dt.Day)
            {
                case 0:
                    day = DayOfWeek.Sunday;
                break;
                case 1:
                    day = DayOfWeek.Monday;
                break;
                case 2:
                    day = DayOfWeek.Tuesday;
                break;
                case 3:
                    day = DayOfWeek.Wednesday;
                break;
                case 4:
                    day = DayOfWeek.Thursday;
                break;
                case 5:
                    day = DayOfWeek.Friday;
                break;
                case 6:
                    day = DayOfWeek.Sunday;
                break;
                default:
                    day = DayOfWeek.Sunday;
                    break;
            }
            return day;
        }
        public class HourOfTheDay 
        {
            public int Hours;
            public int Minutes;
            public int Seconds;
            public string Get12h()
            {
                string d = (Hours>=12)? " PM":" AM";
                int h = (Hours>=12)? Hours-12:Hours;
                return (h == 12 || h-12 == 0)? 12+":"+Minutes+d:h+":"+Minutes+d;
            }
        }
    }
    public class RequireGuildSettings : PreconditionAttribute
    {
        public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            //If the message is being sent in DMs or Groups, Pass.
            if(context.Guild == null) return Task.FromResult(PreconditionResult.FromSuccess());
            var usr = context.User as SocketGuildUser;
            //If the user has the ManageGuild permission, Pass.
            if(usr.Roles.ToList().Exists(x=>x.Permissions.ManageGuild)) return Task.FromResult(PreconditionResult.FromSuccess());
            var G = Program.Database.GetCollection<SysGuild>("Guilds").FindOne(x=>x.Id == context.Guild.Id);
            //If the module this command is from is disabled, Fail.
            var module = G.CommandModules.Find(x=>x.Name == command.Module.Name).Value;
            if(!module) return Task.FromResult(PreconditionResult.FromError("This module is Disabled."));
            //If the server is in Blacklist mode and the channel this is being sent in is in the list, Fail.
            if(G.ListMode==ListMode.Blacklist && G.Channels.Contains(context.Channel.Id)) return Task.FromResult(PreconditionResult.FromError("This Channel is Blacklisted."));
            //If the server is in Whitelist mode and the channel this is being sent in isn't in the list, fail.
            if(G.ListMode==ListMode.Whitelist && !G.Channels.Contains(context.Channel.Id)) return Task.FromResult(PreconditionResult.FromError("This Channel isn't in the whitelist."));
            return Task.FromResult(PreconditionResult.FromSuccess());
        }
    }
    public class CharacterTypeReader : TypeReader
    {
        public async override Task<TypeReaderResult> ReadAsync(ICommandContext context, string input, IServiceProvider services)
        {
            var collection = Program.Database.GetCollection<Character>("Characters");
            var results = collection.Find(x=>x.Name.StartsWith(input.ToLower()) && (x.Guild==context.Guild.Id || x.Owner == context.User.Id));
            if (results.Count()<=0) return TypeReaderResult.FromError(CommandError.ObjectNotFound,"Could not find any Character whose name started with \""+input+"\".");
            else return TypeReaderResult.FromSuccess(results.ToArray());
        }
    }
    public class ModuleTypeReader : TypeReader
    {
        public async override Task<TypeReaderResult> ReadAsync(ICommandContext context, string input, IServiceProvider services)
        {
            var commands = services.GetService<CommandService>();
            var results = commands.Modules.Where(x=>x.Name.ToLower().StartsWith(input.ToLower())
            && !x.Attributes.Any(y=>y.GetType()==typeof(Exclude)));
            if (results.Count()<=0) return TypeReaderResult.FromError(CommandError.ObjectNotFound,"Could not find any modules whose name started with \""+input+"\".");
            else return TypeReaderResult.FromSuccess(results.ToArray());
        }
    }
    public class Exclude : Attribute {}
    public class Untoggleable : Attribute {}
}
