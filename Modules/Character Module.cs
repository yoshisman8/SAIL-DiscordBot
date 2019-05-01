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
        [Summary("Create a new character.")] [RequireGuildSettings] [RequireContext(ContextType.Guild)]
        public async Task CreateCharacter(string Name, string bio = null)
        {
            var col = Database.GetCollection<Character>("Characters");
            var All = col.Find(x=>x.Guild==Context.Guild.Id);
            if (All.Any(x=>x.Name.ToLower() == Name.ToLower()))
            {
                var msg2 = await ReplyAsync("There's already a character whose name is \""+Name+"\", please choose a different name.");
                CommandCache.Add(Context.Message.Id,msg2.Id);
                return;
            }
            var character = new Character()
            {
                Name = Name,
                Owner = Context.User.Id,
                Guild = Context.Guild.Id
            };
            if(bio !=null)
            {
                new CharPage()
                {
                    Fields = new Field[] 
                    {
                        new Field() {Title="Bio",Content=bio}
                    }.ToList()
                };
            }
            col.Insert(character);
            col.EnsureIndex("Name","LOWER($.Name)");

            var plrs = Database.GetCollection<SysUser>("Users");
            if (!plrs.Exists(x=>x.Id==Context.User.Id)) plrs.Insert(new SysUser(){Id=Context.User.Id});
            var plr = plrs.FindById(Context.User.Id);

            plr.Active=character;
            plrs.Update(plr);

            var msg = await ReplyAsync("Created character **"+Name+"**. This character has also been assigned as your active character for all edit commands.");
            CommandCache.Add(Context.Message.Id,msg.Id);
        }
        [Command("AddField"),Alias("NewField")]
        [RequireGuildSettings] [RequireContext(ContextType.Guild)]
        [Summary("Adds a field to your active character sheet. By default it adds it to the first page.")]
        public async Task CreateField(string Name, string Contents, bool Inline = false,int page = 1)
        {
            var guild = Database.GetCollection<SysGuild>("Guilds").FindById(Context.Guild.Id);
            var plrs = Database.GetCollection<SysUser>("Users").IncludeAll();
            if (!plrs.Exists(x=>x.Id==Context.User.Id)) plrs.Insert(new SysUser(){Id=Context.User.Id});
            var plr = plrs.FindById(Context.User.Id);
            if(plr.Active == null)
            {
                var msg1 = await ReplyAsync("You have no active character. Please set your active character by using `"+guild.Prefix+"SetActive CharacterName`.");
                CommandCache.Add(Context.Message.Id,msg1.Id);
                return;
            }
            var character = plr.Active;

            if(character.Pages[page-1].Fields.Count>=20)
            {
                var msg1 = await ReplyAsync("You already have too many fields on page "+page+" of "+character.Name+"'s sheet. Try making a new using `"+guild.Prefix+"NewPage PageName`.");
                CommandCache.Add(Context.Message.Id,msg1.Id);
                return;
            }
            character.Pages[page].Fields.Add(
                new Field()
                {
                    Title = Name,
                    Content = Contents,
                    Inline = Inline
                }
            );
        }
    }
}