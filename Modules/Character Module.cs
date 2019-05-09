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
    [Summary("Create and store character sheets for roleplay and similar purposes!\nUsers with the permission to Manage Messages are considered Character Admins who can delete other people's characters.")]
    public class CharacterModule : InteractiveBase<SocketCommandContext>
    {
        public CommandCacheService CommandCache {get;set;}
        private Controller Controller {get;set;} = new Controller();

        #region CoreCommands
        [Command("Character"),Alias("Char")]
        [RequireGuildSettings] [RequireContext(ContextType.Guild)]
        [Summary("Find a character from this server using their name.")]
        public async Task GetCharacter([Remainder] Character[] Name)
        {
            Character character = null;
            if(Name.Length >1)
            {
                var options = new List<Menu.MenuOption>();
                foreach(var x in Name)
                {
                    options.Add(new Menu.MenuOption(x.Name,async(Menu,index) =>
                    {
                        var list = (Character[])Menu.Storage;
                        return list.ElementAt(index); 
                    }));
                }
                var menu = new Menu("Multiple Characters found.",
                    "Multiple results were found, please specify which one you're trying to see:",
                    options.ToArray(),Name);
                character = (Character)await menu.StartMenu(Context,Interactive);
               
            }
            else
            {
                character=Name.FirstOrDefault();
            }
            
            var msg = await ReplyAsync("Loading"+character.Name+"'s sheet...");

            var prev = new Emoji("⏮");
            var kill = new Emoji("⏹");
            var next = new Emoji("⏭");
            await msg.AddReactionsAsync(new Emoji[]{prev,kill,next});

            Controller.Pages.Clear();
            Controller.Pages = character.PagesToEmbed(Context);
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
            var col = Program.Database.GetCollection<Character>("Characters");
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
            character.Pages.Add(new CharPage());
            if(bio !=null)
            {
                character.Pages[0].Fields.Add(new Field() 
                {
                    Title="Description",Content=bio
                });
            }
            col.Insert(character);
            col.EnsureIndex("Name","LOWER($.Name)");

            var plrs = Program.Database.GetCollection<SysUser>("Users");
            if (!plrs.Exists(x=>x.Id==Context.User.Id)) 
            {
                var usr=new SysUser(){Id=Context.User.Id};
                plrs.Insert(usr);
            }
            var plr = plrs.FindOne(x=>x.Id==Context.User.Id);

            plr.Active=character;
            plrs.Update(plr);

            var msg = await ReplyAsync("Created character **"+Name+"**. This character has also been assigned as your active character for all edit commands.");
            CommandCache.Add(Context.Message.Id,msg.Id);
        }

        [Command("DeleteCharater"),Alias("DelCharacter","RemoveCharacter","DelChar","RemoveChar","RemChar")] [RequireGuildSettings]
        [RequireContext(ContextType.Guild)]
        [Summary("Deletes a character you own. Administrators can delete other people's characters.")]
        public async Task DeleteCharater([Remainder] Character[] Name)
        {
            Character character = null;
            if(Name.Length >1)
            {
                var options = new List<Menu.MenuOption>();
                foreach(var x in Name)
                {
                    options.Add(new Menu.MenuOption(x.Name, async(Menu,index) =>
                    {
                        var list = (Character[])Menu.Storage;
                        return list.ElementAt(index); 
                    }));
                }
                var menu = new Menu("Multiple Characters found.",
                    "Multiple results were found, please specify which one you're trying to see:",
                    options.ToArray(),Name);
                character = (Character)await menu.StartMenu(Context,Interactive);
            }
            else
            {
                character=Name.FirstOrDefault();
            }
            var user = (SocketGuildUser)Context.User;
            if(character.Owner!=user.Id && !user.GuildPermissions.ManageMessages)
            {
                var msg1 = await ReplyAsync("This isn't your character, you cannot delete it.");
                CommandCache.Add(Context.Message.Id,msg1.Id);
                return;
            }
            var confirm = new Emoji("✅"); 
            var cancel = new Emoji("❎");
            object Confirmed = null;
            var msg = await InlineReactionReplyAsync(new ReactionCallbackData("Are you sure you want to delete "+character.Name+"?",null,true,true,TimeSpan.FromMinutes(1),async (ctx)=>{Confirmed= false;})
                .WithCallback(confirm, async (ctx,r) =>
                {
                    Confirmed = true;
                })
                .WithCallback(cancel,async (ctx,r) =>
                {
                    Confirmed = false;
                }));
            while(Confirmed==null)
            {
                await Task.Delay(100);
            }
            if((bool)Confirmed)
            {
                var col = Program.Database.GetCollection<Character>("Characters");
                col.Delete(character.Id);
                await msg.RemoveAllReactionsAsync();
                await msg.ModifyAsync(x=>x.Content ="Deleted character "+character.Name+"!");
                CommandCache.Add(Context.Message.Id,msg.Id);
            }
            else
            {
                await msg.RemoveAllReactionsAsync();
                await msg.ModifyAsync(x=>x.Content ="Character was **not** deleted.");
                CommandCache.Add(Context.Message.Id,msg.Id);
            }
        }

        [Command("SetActive"),Alias("ActiveCharacter","ActiveChar")]
        [RequireGuildSettings] [RequireContext(ContextType.Guild)]
        [Summary("Set your current active character in order to edit its sheet.")]
        public async Task SetActive([Remainder] Character[] Name)
        {
            Character character = null;
            if(Name.Length >1)
            {
                var options = new List<Menu.MenuOption>();
                foreach(var x in Name)
                {
                    options.Add(new Menu.MenuOption(x.Name,async (Menu,index) =>
                    {
                        var list = (Character[])Menu.Storage;
                        return list.ElementAt(index); 
                    }));
                }
                var menu = new Menu("Multiple Characters found.",
                    "Multiple results were found, please specify which one you're trying to assign:",
                    options.ToArray(),Name);
                character = (Character)await menu.StartMenu(Context,Interactive);
               
            }
            else
            {
                character=Name.FirstOrDefault();
            }
            var plrs = Program.Database.GetCollection<SysUser>("Users");
            if (!plrs.Exists(x=>x.Id==Context.User.Id)) 
            {
                var usr=new SysUser(){Id=Context.User.Id};
                plrs.Insert(usr);
            }
            var plr = plrs.FindOne(x=>x.Id==Context.User.Id);
            
            plr.Active = character;
            plrs.Update(plr);

            var msg1 = await ReplyAsync("You have assigned **"+character.Name+"** as your active character.");
            CommandCache.Add(Context.Message.Id,msg1.Id);
        }

        #endregion

        #region FieldManagement

        [Command("AddField"),Alias("NewField")]
        [RequireGuildSettings] [RequireContext(ContextType.Guild)]
        [Summary("Adds a field to your active character sheet. By default, it adds it to the first page.")]
        public async Task CreateField(string Name, string Contents, bool Inline = false,int page = 1)
        {
            page = Math.Abs(page);
            var index = page-1;
            var guild = Program.Database.GetCollection<SysGuild>("Guilds").FindOne(x=>x.Id==Context.Guild.Id);
            var plrs = Program.Database.GetCollection<SysUser>("Users").IncludeAll();
            if (!plrs.Exists(x=>x.Id==Context.User.Id)) plrs.Insert(new SysUser(){Id=Context.User.Id});
            var plr = plrs.FindOne(x=>x.Id==Context.User.Id);
            if(plr.Active == null)
            {
                var msg1 = await ReplyAsync("You have no active character. Please set your active character by using `"+guild.Prefix+"SetActive CharacterName`.");
                CommandCache.Add(Context.Message.Id,msg1.Id);
                return;
            }
            var character = plr.Active;

            if(character.Pages[index].Fields.Count>=20)
            {
                var msg1 = await ReplyAsync("You already have too many fields on page "+(page+1)+" of "+character.Name+"'s sheet. Try making a new using `"+guild.Prefix+"NewPage PageName`.");
                CommandCache.Add(Context.Message.Id,msg1.Id);
                return;
            }
            try 
            {
                character.Pages[index].Fields.Add(
                    new Field()
                    {
                        Title = Name,
                        Content = Contents,
                        Inline = Inline
                    }
                );
                var col = Program.Database.GetCollection<Character>("Characters");
                col.Update(character);
                var msg2 = await ReplyAsync("Created new field "+Name+" on page "+page+" of "+character.Name+"'s sheet",false,character.GetPage(index,Context));
                CommandCache.Add(Context.Message.Id,msg2.Id);
            }
            catch (Exception e)
            {
                var msg2 = await ReplyAsync("Error! You're trying to add field to a page that doesn't exist. "+character.Name+"'s sheet only has "+character.Pages.Count+" page(s).");
                CommandCache.Add(Context.Message.Id,msg2.Id);
            }
        }

        [Command("EditFields"),Alias("ModifyFields","UpdateFields")]
        [RequireGuildSettings] [RequireContext(ContextType.Guild)]
        [Summary("Opens a menu that lets you edit the fields in a page of your character's sheet. Defaults to page 1.")]
        public async Task EditField(int Page = 1)
        {
            Page = Math.Abs(Page);
            var guild = Program.Database.GetCollection<SysGuild>("Guilds").FindOne(x=>x.Id==Context.Guild.Id);
            var plrs = Program.Database.GetCollection<SysUser>("Users").IncludeAll();
            if (!plrs.Exists(x=>x.Id==Context.User.Id)) plrs.Insert(new SysUser(){Id=Context.User.Id});
            var plr = plrs.FindOne(x=>x.Id==Context.User.Id);
            if(plr.Active == null)
            {
                var msg1 = await ReplyAsync("You have no active character. Please set your active character by using `"+guild.Prefix+"SetActive CharacterName`.");
                CommandCache.Add(Context.Message.Id,msg1.Id);
                return;
            }
            var character = plr.Active;
            if (Page>character.Pages.Count)
            {
                var msg1 = await ReplyAsync("Your character doesn't have as that many pages. "+character.Name+" only has "+character.Pages.Count+" page(s).");
                CommandCache.Add(Context.Message.Id,msg1.Id);
                return;
            }
            var EditPage = character.Pages[Page-1];
            var Options = new List<Menu.MenuOption>();

            foreach(var x in EditPage.Fields)
            {
                Options.Add(new Menu.MenuOption(x.Title,
                async (menu,idx) =>
                {
                    var prompt =  await menu.Context.Channel.SendMessageAsync("Please reply with the new contents of this field.");
                    SocketMessage result = await menu.Interactive.NextMessageAsync(menu.Context,true,true,TimeSpan.FromMinutes(3));
                    
                    await prompt.DeleteAsync();
                    
                    ((Field[])menu.Storage)[idx].Content = result.Content;
                    
                    await result.DeleteAsync();

                    menu.Options[idx].Description = result.Content;
                    menu.ReloadMenu().RunSynchronously();

                    return null;
                },x.Content,false));
            }
            Options.Add(new Menu.MenuOption("Save Changes",
            async (Menu,idx)=>
            {
                return Menu.Storage;
            },"Save Changes and update your Character",true));
            Options.Add(new Menu.MenuOption("Discard Changes",
            async (Menu,idx) =>
            {
                return null;
            },"Discard all changes and stop editing.",true));
            
            Menu editmenu = new Menu("Editing Page "+Page+" of "+character.Name+"'s sheet.","Use the cursor to select the field you want to edit, or press Save/Discard to either save the chanes or discard all changes.",Options.ToArray(),EditPage.Fields.ToArray());
            Field[] fields = (Field[])await editmenu.StartMenu(Context,Interactive);

            if (fields==null)
            {
                var msg1 = await ReplyAsync("You have discarded all changes to "+character.Name+"'s sheet.");
                CommandCache.Add(Context.Message.Id,msg1.Id);
                return;
            }
            else
            {
                character.Pages[Page-1].Fields = fields.ToList();
                var col = Program.Database.GetCollection<Character>("Characters");
                col.Update(character);

                var msg1 = await ReplyAsync("💾 Saved chanes to "+character.Name+"'s sheet!");
                CommandCache.Add(Context.Message.Id,msg1.Id);
                return;
            }
        }
        [Command("DeleteField"), Alias("RemoveField","DelField","RemField")]
        [RequireGuildSettings] [RequireContext(ContextType.Guild)]
        [Summary("Deletes one of the fields in a page. Defaults to page 1.")]
        public async Task DelField(string Name, int Page = 1)
        {
            Page = Math.Abs(Page);
            var guild = Program.Database.GetCollection<SysGuild>("Guilds").FindById(Context.Guild.Id);
            var plrs = Program.Database.GetCollection<SysUser>("Users").IncludeAll();
            if (!plrs.Exists(x=>x.Id==Context.User.Id)) plrs.Insert(new SysUser(){Id=Context.User.Id});
            var plr = plrs.FindOne(x=> x.Id == Context.User.Id);
            if(plr.Active == null)
            {
                var msg1 = await ReplyAsync("You have no active character. Please set your active character by using `"+guild.Prefix+"SetActive CharacterName`.");
                CommandCache.Add(Context.Message.Id,msg1.Id);
                return;
            }
            var character = plr.Active;
            if (Page>character.Pages.Count)
            {
                var msg1 = await ReplyAsync("Your character doesn't have as that many pages. "+character.Name+" only has "+character.Pages.Count+" page(s).");
                CommandCache.Add(Context.Message.Id,msg1.Id);
                return;
            }
            if(!character.Pages[Page-1].Fields.Exists(x=>x.Title.ToLower().StartsWith(Name.ToLower())))
            {
                var msg1 = await ReplyAsync("There is no field on page "+Page+" of "+character.Name+"'s sheet.");
                CommandCache.Add(Context.Message.Id,msg1.Id);
                return;
            }
            var fields = character.Pages[Page-1].Fields.FindAll(x=>x.Title.ToLower().StartsWith(Name.ToLower()));
            Field field = null;
            if (fields.Count>1)
            {
                var options = new List<Menu.MenuOption>();
                foreach(var x in fields)
                {
                    options.Add(new Menu.MenuOption(x.Content,
                    async (Menu,idx)=>
                    {
                        return ((Field[])Menu.Storage)[idx];
                    },x.Content));
                }
                field = (Field)await new Menu("Multiple Fields located","There are multiple fields that start with \""+Name+"\". Select which one you want to delete or select cancel to cancel.",options.ToArray(),fields).StartMenu(Context,Interactive);
            }
            else field = fields.FirstOrDefault();

            var index = character.Pages[Page-1].Fields.IndexOf(field);
            character.Pages[Page-1].Fields.RemoveAt(index);

            var col = Program.Database.GetCollection<Character>("Characters");
            col.Update(character);
            var msg = await ReplyAsync("Deleted field "+field.Title+" from "+character.Name+"'s sheet on page "+Page+".");
            CommandCache.Add(Context.Message.Id,msg.Id);
            return;
        }
        #endregion

        #region PageManagement
        [Command("NewPage"),Alias("AddPage")] 
        [RequireGuildSettings] [RequireContext(ContextType.Guild)]
        [Summary("Adds a new page to your active character's sheet.")]
        public async Task addpage()
        {
            var guild = Program.Database.GetCollection<SysGuild>("Guilds").FindOne(x=>x.Id==Context.Guild.Id);
            var plrs = Program.Database.GetCollection<SysUser>("Users").IncludeAll();
            if (!plrs.Exists(x=>x.Id==Context.User.Id)) plrs.Insert(new SysUser(){Id=Context.User.Id});
            var plr = plrs.FindOne(x=>x.Id ==Context.User.Id);
            if(plr.Active == null)
            {
                var msg1 = await ReplyAsync("You have no active character. Please set your active character by using `"+guild.Prefix+"SetActive CharacterName`.");
                CommandCache.Add(Context.Message.Id,msg1.Id);
                return;
            }
            var character = plr.Active;
            character.Pages.Add(new CharPage());

            var col = Program.Database.GetCollection<Character>("Characters");
            col.Update(character);
            var msg2 = await ReplyAsync("Created a new page to "+character.Name+"'s sheet.");
            CommandCache.Add(Context.Message.Id,msg2.Id);
        }
        #endregion
    }
}