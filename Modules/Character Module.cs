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
        [Summary("Find a character from this server using their name.")]
        public async Task GetCharacter([Remainder] Character Name)
        {
            var All = Database.GetCollection<Character>("Characters").Find(x=>x.Guild==Context.Guild.Id);
            
            var msg = await ReplyAsync("Searching for \""+Name+"\"...");

            var prev = new Emoji("⏮");
            await msg.AddReactionAsync(prev);
            var kill = new Emoji("⏹");
            await msg.AddReactionAsync(kill);
            var next = new Emoji("⏭");
            await msg.AddReactionAsync(next);

            Controller.Pages.Clear();
            Controller.Pages = Name.PagesToEmbed();
            await msg.ModifyAsync(x=> x.Embed = Controller.Pages.ElementAt(Controller.Index));

            Interactive.AddReactionCallback(msg,new InlineReactionCallback(Interactive,Context,
            new ReactionCallbackData("",null,false,false,TimeSpan.FromMinutes(3))
                .WithCallback(prev,(ctx,rea)=>Controller.Previous(ctx,rea,msg))
                .WithCallback(kill,(ctx,rea)=>Controller.Kill(Interactive,msg))
                .WithCallback(next,(ctx,rea)=>Controller.Next(ctx,rea,msg))));
            CommandCache.Add(Context.Message.Id,msg.Id);
        }
        [Command("NewCharacter"), Alias("AddCharacter","CreateCharacter","NewChar","AddChar","CreateChar")]
        [Summary("Create a new character. ")]
        public async Task CreateCharacter(string Name, string bio)
        {
            var All = Database.GetCollection<Character>("Characters").Find(x=>x.Guild==Context.Guild.Id);
            if (All.Any(x=>x.Name.ToLower() == Name.ToLower()))
            {
                var msg = await ReplyAsync("There's already a character whose name is \""+Name+"\", please choose a different name.");
                CommandCache.Add(Context.Message.Id,msg.Id);
                return;
            }
        }
    }
}