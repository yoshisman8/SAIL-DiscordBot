using System;
using System.Net;
using System.Text;
using System.Linq;
using System.Globalization;
using System.Threading.Tasks;
using System.Collections.Generic;
using SAIL.Classes;
using LiteDB;
using Discord.Addons.CommandCache;
using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.WebSocket;
using Discord.Rest;
using Discord;

namespace SAIL.Modules 
{
    [Name("Character Module")]
    [Summary("Create and store character sheets for roleplay and similar purposes!")]
    public class CharacterModule : InteractiveBase<SocketCommandContext>
    {
        public LiteDatabase Database {get;set;}
        public CommandCacheService CommandCache {get;set;}
        private Controller Controller {get;set;} = new Controller();


        [Command("Character"),Alias("Char")]
        [RequireGuildSettings] [RequireContext(ContextType.Guild)]
        [Summary("Find a character from this server using their name. If multiple characters are found, only the first one (ordered alphabetically) will be displayed.")]
        public async Task GetCharacter([Remainder] string Name)
        {
            var All = Database.GetCollection<Character>("Characters").Find(x=>x.Guild==Context.Guild.Id);
            var guild = Database.GetCollection<SysGuild>("Guilds").FindOne(x=>x.Id==Context.Guild.Id);
            
            if (All.Count() == 0)
            {
                var msg = await ReplyAsync("Nobody in this server has made a character yet. Use "+guild.Prefix+"AddChar to be the first.");
                CommandCache.Add(Context.Message.Id,msg.Id);
                return;
            }

            var results = All.Where(x=>x.Name.ToLower().Contains(Name.ToLower()));
            if(results.Count() == 0)
            {
                var msg = await ReplyAsync("There are no characters whose names contain \""+Name+"\".");
                CommandCache.Add(Context.Message.Id,msg.Id);
                return;
            }
            else
            {
                var msg = await ReplyAsync("Searching for \""+Name+"\"...");

                var prev = new Emoji("⏮");
                await msg.AddReactionAsync(prev);
                var kill = new Emoji("⏹");
                await msg.AddReactionAsync(kill);
                var next = new Emoji("⏭");
                await msg.AddReactionAsync(next);

                var character = results.OrderBy(x=>x.Name).FirstOrDefault();
                Controller.Pages.Clear();
                Controller.Pages = character.PagesToEmbed();
                await msg.ModifyAsync(x=> x.Embed = Controller.Pages.ElementAt(Controller.Index));

                Interactive.AddReactionCallback(msg,new InlineReactionCallback(Interactive,Context,
                new ReactionCallbackData("",null,false,false,TimeSpan.FromMinutes(3))
                    .WithCallback(prev,(ctx,rea)=>Controller.Previous(ctx,rea,msg))
                    .WithCallback(kill,(ctx,rea)=>Controller.Kill(Interactive,msg))
                    .WithCallback(next,(ctx,rea)=>Controller.Next(ctx,rea,msg))));
                CommandCache.Add(Context.Message.Id,msg.Id);
            }
        }
    }
}