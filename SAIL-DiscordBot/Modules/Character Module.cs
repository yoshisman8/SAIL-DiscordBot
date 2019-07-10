using System;
using System.Net;
using System.Text;
using System.Linq;
using System.Globalization;
using System.Threading.Tasks;
using System.Collections.Generic;
using SAIL.Classes;
using LiteDB;

using Discord.Addons.Interactive;
using Discord.Addon.InteractiveMenus;
using Discord.Commands;
using Discord.WebSocket;
using Discord.Rest;
using Discord;

namespace SAIL.Modules 
{
    [Name("Character Module")]
    [Summary("Create and store character sheets for roleplay and similar purposes!\nUsers with the permission to Manage Messages are considered Character Admins who can delete other people's characters.")]
    public class CharacterModule : SailBase<SocketCommandContext>
    {
		public MenuService MenuService { get; set; }

        #region CoreCommands
        [Command("Character"),Alias("Char")]
        [RequireGuildSettings] [RequireContext(ContextType.Guild)]
        [Summary("Find a character from this server using their name.")]
        public async Task GetCharacter([Remainder] Character[] Name)
        {
            Character character = null;
            if(Name.Length >1)
            {
				string[] options = Name.Select(x => x.Name).ToArray(); 
                var menu = new SelectorMenu("Multiple results were found, please specify which one you're trying to see:",options,Name);
				await MenuService.CreateMenu(Context, menu, true);
				character = (Character)await menu.GetSelectedObject();
               
            }
            else
            {
                character=Name.FirstOrDefault();
            }
			var PagedMenu = new PagedEmbed(character.Name+"'s Sheet.",character.PagesToEmbed(Context).ToArray());
			var msg = await MenuService.CreateMenu(Context, PagedMenu, false);
            
            
        }
        [Command("Characters"),Alias("Chars")]
        [RequireGuildSettings] [RequireContext(ContextType.Guild)]
        [Summary("List all characters and who made them.")]
        public async Task GetAllCharacter()
        {
            var all = Program.Database.GetCollection<Character>("Characters").Find(x=>x.Guild==Context.Guild.Id).OrderBy(x=>x.Name);
            
            int pagecount = (int)Math.Ceiling((decimal)all.Count()/10);
            var pages = new List<Embed>();
            for (int i = 0;i<pagecount;i++)
            {
                var b = all.Skip(i*10);
                var a = b.Take(10);
                string[] names = a.Select(x=>x.Name).ToArray();
                pages.Add(new EmbedBuilder()
                .WithTitle("Characters of "+Context.Guild.Name+" (Page "+(1+i)+" of "+pagecount+")")
                .WithTimestamp(DateTime.Now)
                .WithDescription(String.Join("\n",names))
                .Build());
            }
			var menu = new PagedEmbed("All Characters.", pages.ToArray());
			var msg = await MenuService.CreateMenu(Context,menu,false);
            
        }
        [Command("CurrentCharacter"),Alias("CurrentChar","Char","Character")]
        [RequireGuildSettings] [RequireContext(ContextType.Guild)]
        [Summary("Fetch your current active character.")]
        
        public async Task GetActive()
        {
            var plrs = Program.Database.GetCollection<SysUser>("Users").IncludeAll();
            var col = Program.Database.GetCollection<Character>("Characters");
            var guild = Program.Database.GetCollection<SysGuild>("Guilds").FindOne(x=>x.Id==Context.Guild.Id);  
            if (!plrs.Exists(x=>x.Id==Context.User.Id)) 
            {
                var usr=new SysUser(){Id=Context.User.Id};
                plrs.Insert(usr);
            }
            var plr = plrs.FindOne(x=>x.Id==Context.User.Id);
            
            if(plr.Active == null)
            {
                var msg2 = await ReplyAsync("You have no active character. Please set your active character by using `"+guild.Prefix+"SetActive CharacterName`.");
                
                return;
            }

			var menu = new PagedEmbed(plr.Active.Name+"'s sheet.", plr.Active.PagesToEmbed(Context).ToArray());
			var msg = await MenuService.CreateMenu(Context, menu, false);
			
        }
        [Command("NewCharacter"), Alias("AddCharacter","CreateCharacter","NewChar","AddChar","CreateChar")]
        [Summary("Create a new character.")] [RequireGuildSettings] [RequireContext(ContextType.Guild)]
        
        public async Task CreateCharacter(string Name, [Remainder]string bio = "No page description set")
        {
            var col = Program.Database.GetCollection<Character>("Characters");
            var All = col.Find(x=>x.Guild==Context.Guild.Id);
            if (All.Any(x=>x.Name.ToLower() == Name.ToLower()))
            {
                var msg2 = await ReplyAsync("There's already a character whose name is \""+Name+"\", please choose a different name.");
                
                return;
            }
            var character = new Character()
            {
                Name = Name,
                Owner = Context.User.Id,
                Guild = Context.Guild.Id
            };
            character.Pages.Add(new CharPage());
            character.Pages[0].Summary = bio;

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
            
        }

        [Command("DeleteCharater"),Alias("DelCharacter","RemoveCharacter","DelChar","RemoveChar","RemChar")] [RequireGuildSettings]
        [RequireContext(ContextType.Guild)]
        [Summary("Deletes a character you own. Administrators can delete other people's characters.")]
        public async Task DeleteCharater([Remainder] Character[] Name)
        {
            Character character = null;
            if(Name.Length >1)
            {
				string[] options = Name.Select(x => x.Name).ToArray();
				var menu = new SelectorMenu("Multiple results were found, please specify which one you're trying to see:", options, Name);
				await MenuService.CreateMenu(Context, menu, true);
				character = (Character)await menu.GetSelectedObject();
			}
            else
            {
                character=Name.FirstOrDefault();
            }
            var user = (SocketGuildUser)Context.User;
            if(character.Owner!=user.Id && !user.GuildPermissions.ManageMessages)
            {
                var msg1 = await ReplyAsync("This isn't your character, you cannot delete it.");
                
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
                
            }
            else
            {
                await msg.RemoveAllReactionsAsync();
                await msg.ModifyAsync(x=>x.Content ="Character was **not** deleted.");
                
            }
        }

        [Command("SetActive"),Alias("ActiveCharacter","ActiveChar","Active","Lock")]
        [RequireGuildSettings] [RequireContext(ContextType.Guild)]
        [Summary("Set your current active character in order to edit its sheet.")]
        
        public async Task SetActive([Remainder] Character[] Name)
        {
            Character character = null;
            if(Name.Length >1)
            {
				string[] options = Name.Select(x => x.Name).ToArray();
				var menu = new SelectorMenu("Multiple results were found, please specify which one you're trying to see:", options, Name);
				await MenuService.CreateMenu(Context, menu, true);
				character = (Character)await menu.GetSelectedObject();
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
            
        }
        [Command("RenameCharacter"),Alias("RenameChar","RenCharacter","RenChar")]
        [RequireGuildSettings] [RequireContext(ContextType.Guild)]
        [Summary("Rename your current active character.")]
        public async Task RenameChar(string NewName)
        {
            var plrs = Program.Database.GetCollection<SysUser>("Users").IncludeAll();
            var col = Program.Database.GetCollection<Character>("Characters");
            var guild = Program.Database.GetCollection<SysGuild>("Guilds").FindOne(x=>x.Id==Context.Guild.Id);  
            if (!plrs.Exists(x=>x.Id==Context.User.Id)) 
            {
                var usr=new SysUser(){Id=Context.User.Id};
                plrs.Insert(usr);
            }
            var plr = plrs.FindOne(x=>x.Id==Context.User.Id);
            
            if(plr.Active == null)
            {
                var msg = await ReplyAsync("You have no active character. Please set your active character by using `"+guild.Prefix+"SetActive CharacterName`.");
                
                return;
            }
            if (col.Exists(x=>x.Name == NewName.ToLower()))
            {
                var msg2 = await ReplyAsync("There's already a character whose name is \""+NewName+"\", please choose a different name.");
                
                return;
            }

            var character = plr.Active;
            character.Name = NewName;
            
            col.Update(character);

            var msg1 = await ReplyAsync("You have renamed "+character.Name+" to **"+NewName+"**.");
            
        }
        
        [Command("UpdateCharacter"),Alias("UpdateChar")]
        [RequireGuildSettings] [RequireContext(ContextType.Guild)]
        [Summary("Updates your active character's page 1 summary. Useful for simpler character sheets.")]
        public async Task UpdateChar([Remainder]string Bio)
        {
            var plrs = Program.Database.GetCollection<SysUser>("Users").IncludeAll();
            var col = Program.Database.GetCollection<Character>("Characters");
            var guild = Program.Database.GetCollection<SysGuild>("Guilds").FindOne(x=>x.Id==Context.Guild.Id);  
            if (!plrs.Exists(x=>x.Id==Context.User.Id)) 
            {
                var usr=new SysUser(){Id=Context.User.Id};
                plrs.Insert(usr);
            }
            var plr = plrs.FindOne(x=>x.Id==Context.User.Id);
            
            if(plr.Active == null)
            {
                var msg = await ReplyAsync("You have no active character. Please set your active character by using `"+guild.Prefix+"SetActive CharacterName`.");
                
                return;
            }

            var character = plr.Active;
            character.Pages[0].Summary = Bio;
            
            col.Update(character);

            var msg1 = await ReplyAsync("You have updated **"+character.Name+"** sheet.");
            
        }
        #endregion

        #region FieldManagement

        [Command("AddField"),Alias("NewField")]
        [RequireGuildSettings] [RequireContext(ContextType.Guild)]
        [Summary("Adds a field to your active character sheet. By default, it adds it to the first page.")]
        public async Task CreateField(string FieldName, string Contents, bool Inline = false, int page = 1)
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
                return;
            }
            var character = plr.Active;

            if (page > character.Pages.Count)
            {
                var msg1 = await ReplyAsync("Error! You're trying to add field to a page that doesn't exist. "+character.Name+"'s sheet only has "+character.Pages.Count+" page(s).");
				return;
			}
            if(character.Pages[index].Fields.Count>=20)
            {
                var msg1 = await ReplyAsync("You already have too many fields on page "+(page+1)+" of "+character.Name+"'s sheet. Try making a new using `"+guild.Prefix+"NewPage PageName`.");
                return;
            }
            character.Pages[index].Fields.Add(
                new Field()
                {
                    Title = FieldName,
                    Content = Contents,
                    Inline = Inline
                }
            );
            var col = Program.Database.GetCollection<Character>("Characters");
            col.Update(character);
            var msg2 = await ReplyAsync("Created new field "+FieldName+" on page "+page+" of "+character.Name+"'s sheet",false,character.GetPage(index,Context));
        }

        [Command("EditFields"),Alias("ModifyFields","UpdateFields")]
        [RequireGuildSettings] [RequireContext(ContextType.Guild)]
        [Summary("Opens a menu that lets you edit the fields in a page of your character's sheet. Defaults to page 1.")]
        public async Task EditFields(int Page = 1)
        {
            Page = Math.Abs(Page);
            var guild = Program.Database.GetCollection<SysGuild>("Guilds").FindOne(x=>x.Id==Context.Guild.Id);
            var plrs = Program.Database.GetCollection<SysUser>("Users").IncludeAll();
            if (!plrs.Exists(x=>x.Id==Context.User.Id)) plrs.Insert(new SysUser(){Id=Context.User.Id});
            var plr = plrs.FindOne(x=>x.Id==Context.User.Id);
            if(plr.Active == null)
            {
                var msg1 = await ReplyAsync("You have no active character. Please set your active character by using `"+guild.Prefix+"SetActive CharacterName`.");
                
                return;
            }
            var character = plr.Active;
            if (Page>character.Pages.Count)
            {
                var msg1 = await ReplyAsync("Your character doesn't have as that many pages. "+character.Name+" only has "+character.Pages.Count+" page(s).");
                
                return;
            }
            var EditPage = character.Pages[Page-1];
            var Options = new List<EditorMenu.EditorOption>();

            foreach(var x in EditPage.Fields)
            {
                Options.Add(new EditorMenu.EditorOption(x.Title,x.Content,
                async (mContext) =>
                {
                    var prompt =  await mContext.CommandContext.Channel.SendMessageAsync("Please reply with the new contents of this field.");
                    SocketMessage result = await mContext.MenuService.NextMessageAsync(mContext.CommandContext,TimeSpan.FromMinutes(3));
                    
                    await prompt.DeleteAsync();
                    
                    ((Field[])mContext.EditableObject)[mContext.CurrentIndex].Content = result.Content;
                    
                    await result.DeleteAsync();

                    mContext.CurrentOption.Description = result.Content;
                    return mContext.EditableObject;
                }));
            }

			var menu = new EditorMenu("Editing Page " + Page + " of " + character.Name + "'s Sheet", character.Pages[Page].Fields.ToArray(), Options.ToArray());
			await MenuService.CreateMenu(Context, menu,true);
			Field[] fields = (Field[])await menu.GetObject();

            if (fields==null)
            {
                var msg1 = await ReplyAsync("You have discarded all changes to "+character.Name+"'s sheet.");
                
                return;
            }
            else
            {
                character.Pages[Page-1].Fields = fields.ToList();
                var col = Program.Database.GetCollection<Character>("Characters");
                col.Update(character);

                var msg1 = await ReplyAsync("💾 Saved chanes to "+character.Name+"'s sheet!");
                
                return;
            }
        }
        [Command("DeleteField"), Alias("RemoveField","DelField","RemField")]
        [RequireGuildSettings] [RequireContext(ContextType.Guild)]
        [Summary("Deletes one of the fields in a page. Defaults to page 1. The name of the field has to be wrapped around quotation marks \"Like this\".")]
        public async Task DelField(string FieldName, int Page = 1)
        {
            Page = Math.Abs(Page);
            var guild = Program.Database.GetCollection<SysGuild>("Guilds").FindById(Context.Guild.Id);
            var plrs = Program.Database.GetCollection<SysUser>("Users").IncludeAll();
            if (!plrs.Exists(x=>x.Id==Context.User.Id)) plrs.Insert(new SysUser(){Id=Context.User.Id});
            var plr = plrs.FindOne(x=> x.Id == Context.User.Id);
            if(plr.Active == null)
            {
                var msg1 = await ReplyAsync("You have no active character. Please set your active character by using `"+guild.Prefix+"SetActive CharacterName`.");
                
                return;
            }
            var character = plr.Active;
            if (Page>character.Pages.Count)
            {
                var msg1 = await ReplyAsync("Your character doesn't have as that many pages. "+character.Name+" only has "+character.Pages.Count+" page(s).");
                
                return;
            }
            if(!character.Pages[Page-1].Fields.Exists(x=>x.Title.ToLower().StartsWith(FieldName.ToLower())))
            {
                var msg1 = await ReplyAsync("There is no such field on page "+Page+" of "+character.Name+"'s sheet.");
                
                return;
            }
            var fields = character.Pages[Page-1].Fields.FindAll(x=>x.Title.ToLower().StartsWith(FieldName.ToLower()));
            Field field = null;
            if (fields.Count>1)
            {
				string[] options = fields.Select(x => x.Title).ToArray();
				var menu = new SelectorMenu("Multiple results were found, please specify which one you're trying to delete:", options, fields.ToArray());
				await MenuService.CreateMenu(Context, menu, true);
				field = (Field)await menu.GetSelectedObject();

            }
            else field = fields.FirstOrDefault();

            var index = character.Pages[Page-1].Fields.IndexOf(field);
            character.Pages[Page-1].Fields.RemoveAt(index);

            var col = Program.Database.GetCollection<Character>("Characters");
            col.Update(character);
            var msg = await ReplyAsync("Deleted field "+field.Title+" from "+character.Name+"'s sheet on page "+Page+".");
            
            return;
            
        }

        [Command("RenameField")] [RequireGuildSettings]
        [RequireContext(ContextType.Guild)]
        [Summary("Rename a field in a page of your character sheet. Defaulst to Page 1.")]
        public async Task renameField(string OldName,string NewName, int Page = 1)
        {
            Page = Math.Abs(Page);
            var guild = Program.Database.GetCollection<SysGuild>("Guilds").FindById(Context.Guild.Id);
            var plrs = Program.Database.GetCollection<SysUser>("Users").IncludeAll();
            if (!plrs.Exists(x=>x.Id==Context.User.Id)) plrs.Insert(new SysUser(){Id=Context.User.Id});
            var plr = plrs.FindOne(x=> x.Id == Context.User.Id);
            if(plr.Active == null)
            {
                var msg1 = await ReplyAsync("You have no active character. Please set your active character by using `"+guild.Prefix+"SetActive CharacterName`.");
                
                return;
            }
            var character = plr.Active;
            if (Page>character.Pages.Count)
            {
                var msg1 = await ReplyAsync("Your character doesn't have as that many pages. "+character.Name+" only has "+character.Pages.Count+" page(s).");
                
                return;
            }
            if(!character.Pages[Page-1].Fields.Exists(x=>x.Title.ToLower().StartsWith(OldName.ToLower())))
            {
                var msg1 = await ReplyAsync("There is no such field on page "+Page+" of "+character.Name+"'s sheet.");
                
                return;
            }
            var fields = character.Pages[Page-1].Fields.FindAll(x=>x.Title.ToLower().StartsWith(OldName.ToLower()));
            Field field = null;
            if (fields.Count>1)
            {
				string[] options = fields.Select(x => x.Title).ToArray();
				var menu = new SelectorMenu("Multiple results were found, please specify which one you're trying to delete:", options, fields.ToArray());
				await MenuService.CreateMenu(Context, menu, true);
				field = (Field)await menu.GetSelectedObject();
			}
            else field = fields.FirstOrDefault();

            var index = character.Pages[Page-1].Fields.IndexOf(field);
            character.Pages[Page-1].Fields[index].Title = NewName;

            var col = Program.Database.GetCollection<Character>("Characters");
            col.Update(character);
            var msg = await ReplyAsync("Renamed field "+field.Title+" to "+NewName+" on "+character.Name+"'s sheet on page "+Page+".");
            
            return;
        }
		[Command("EditField"),Alias("UpdateField")]
		[RequireGuildSettings] [RequireContext(ContextType.Guild)]
		[Summary("Directly edit the conents of a field on any given page. Defaults to page 1. Both the field name and contents must be wrapped around quotation marks \"like this\".")]
		public async Task EditField(string Name, string Contents, int Page = 1)
		{
			Page = Math.Abs(Page);
			var guild = Program.Database.GetCollection<SysGuild>("Guilds").FindById(Context.Guild.Id);
			var plrs = Program.Database.GetCollection<SysUser>("Users").IncludeAll();
			if (!plrs.Exists(x => x.Id == Context.User.Id)) plrs.Insert(new SysUser() { Id = Context.User.Id });
			var plr = plrs.FindOne(x => x.Id == Context.User.Id);
			if (plr.Active == null)
			{
				var msg1 = await ReplyAsync("You have no active character. Please set your active character by using `" + guild.Prefix + "SetActive CharacterName`.");
				
				return;
			}
			var character = plr.Active;
			if (Page > character.Pages.Count)
			{
				var msg1 = await ReplyAsync("Your character doesn't have as that many pages. " + character.Name + " only has " + character.Pages.Count + " page(s).");
				
				return;
			}
			var fields = character.Pages[Page - 1].Fields.FindAll(x => x.Title.ToLower().StartsWith(Name.ToLower()));
			Field field = null;
			if (fields!=null || fields.Count==0)
			{
				var msg1 = await ReplyAsync("There is no such field on page " + Page + " of " + character.Name + "'s sheet.");
				
				return;
			}
			if (fields.Count > 1)
			{
				string[] options = fields.Select(x => x.Title).ToArray();
				var menu = new SelectorMenu("Multiple results were found, please specify which one you're trying to delete:", options, fields.ToArray());
				await MenuService.CreateMenu(Context, menu, true);
				field = (Field)await menu.GetSelectedObject();
			}
			else field = fields.FirstOrDefault();

			var index = character.Pages[Page - 1].Fields.IndexOf(field);
			character.Pages[Page - 1].Fields[index].Content = Contents;

			var col = Program.Database.GetCollection<Character>("Characters");
			col.Update(character);
			var msg = await ReplyAsync("Updated field " + field.Title +" on " + character.Name + "'s sheet on page " + Page + ".");
			
		}
        #endregion

        #region PageManagement
        [Command("NewPage"),Alias("AddPage")] 
        [RequireGuildSettings] [RequireContext(ContextType.Guild)]
        [Summary("Adds a new page to your active character's sheet.")]
        public async Task addpage([Remainder]string PageDescription = "No page description set")
        {
            var guild = Program.Database.GetCollection<SysGuild>("Guilds").FindOne(x=>x.Id==Context.Guild.Id);
            var plrs = Program.Database.GetCollection<SysUser>("Users").IncludeAll();
            if (!plrs.Exists(x=>x.Id==Context.User.Id)) plrs.Insert(new SysUser(){Id=Context.User.Id});
            var plr = plrs.FindOne(x=>x.Id ==Context.User.Id);
            if(plr.Active == null)
            {
                var msg1 = await ReplyAsync("You have no active character. Please set your active character by using `"+guild.Prefix+"SetActive CharacterName`.");
                
                return;
            }
            var character = plr.Active;
            character.Pages.Add(new CharPage(){Summary=PageDescription});

            var col = Program.Database.GetCollection<Character>("Characters");
            col.Update(character);
            var msg2 = await ReplyAsync("Created a new page to "+character.Name+"'s sheet.");
            
        }
        [Command("DeletePage"),Alias("DelPage")]
        [RequireGuildSettings] [RequireContext(ContextType.Guild)]
        [Summary("Deletes a page from your active character's sheet.")]
        public async Task DelPage(int PageNumber)
        {
            PageNumber = Math.Abs(PageNumber);
            var guild = Program.Database.GetCollection<SysGuild>("Guilds").FindOne(x=>x.Id==Context.Guild.Id);
            var plrs = Program.Database.GetCollection<SysUser>("Users").IncludeAll();
            if (!plrs.Exists(x=>x.Id==Context.User.Id)) plrs.Insert(new SysUser(){Id=Context.User.Id});
            var plr = plrs.FindOne(x=>x.Id ==Context.User.Id);
            if(plr.Active == null)
            {
                var msg1 = await ReplyAsync("You have no active character. Please set your active character by using `"+guild.Prefix+"SetActive CharacterName`.");
                
                return;
            }
            var character = plr.Active;

            var cancel = new Emoji("❎");
            var confirm = new Emoji("✅");
            bool? confirmation = null;
            var msg = await ReplyAsync("Are you SURE you want to delete page "+PageNumber+" of "+character.Name+"'s sheet? (All fields will be lost and this action cannot be undone!)",false,character.GetPage(PageNumber-1,Context));
            Interactive.AddReactionCallback(msg,new InlineReactionCallback(Interactive,Context,new ReactionCallbackData("")
                .WithCallback(cancel,async(x,r)=>{confirmation = false;})
                .WithCallback(confirm,async(x,r)=>{confirmation = true;}))
                );
            await msg.AddReactionAsync(cancel);
            await Task.Delay(TimeSpan.FromSeconds(3));
            await msg.AddReactionAsync(confirm);
            while (confirmation == null)
            {
                await Task.Delay(100);
            }
            if ((bool)confirmation)
            {
                character.Pages.RemoveAt(PageNumber-1);
                var col = Program.Database.GetCollection<Character>("Characters");
                col.Update(character);

                await msg.ModifyAsync(x=>x.Embed = null);
                await msg.ModifyAsync(x=>x.Content=("Deleted page "+PageNumber+" from "+character.Name+"'s sheet."));
                
                return;
            }
            else
            {
                await msg.ModifyAsync(x=>x.Embed = null);
                await msg.ModifyAsync(x=>x.Content="You decided to keep page "+PageNumber+" of "+character.Name+"'s sheet.");
                
                return;
            }
        }

        [Command("EditPage")]
        [RequireGuildSettings] [RequireContext(ContextType.Guild)]
        [Summary("Opens the editor menu for one of the pages of your active character's sheet.")]
        public async Task Editpage([Remainder] int PageNumber = 1)
        {
            PageNumber = Math.Abs(PageNumber);
            var index = PageNumber-1;
            var guild = Program.Database.GetCollection<SysGuild>("Guilds").FindOne(x=>x.Id==Context.Guild.Id);
            var plrs = Program.Database.GetCollection<SysUser>("Users").IncludeAll();
            if (!plrs.Exists(x=>x.Id==Context.User.Id)) plrs.Insert(new SysUser(){Id=Context.User.Id});
            var plr = plrs.FindOne(x=>x.Id==Context.User.Id);
            if(plr.Active == null)
            {
                var msg1 = await ReplyAsync("You have no active character. Please set your active character by using `"+guild.Prefix+"SetActive CharacterName`.");
                
                return;
            }
            var character = plr.Active;

            if (PageNumber > character.Pages.Count)
            {
                var msg1 = await ReplyAsync("Error! You're trying to edit a page that doesn't exist. "+character.Name+"'s sheet only has "+character.Pages.Count+" page(s).");
                
                return;
            }

            var pagetoedit = character.Pages[index];

            var MenuOption = new EditorMenu.EditorOption[]
            {
                new EditorMenu.EditorOption("Change Page description",pagetoedit.Summary,
				async (m)=>
                {
                    var prompt =  await m.CommandContext.Channel.SendMessageAsync("Please reply with the new description for this page.");
                    SocketMessage result = await m.MenuService.NextMessageAsync(m.CommandContext,TimeSpan.FromMinutes(3));
                    
                    await prompt.DeleteAsync();
                    
                    ((CharPage)m.EditableObject).Summary = result.Content;
                    
                    await result.DeleteAsync();

                    m.CurrentOption.Description = "Current Description:\n"+result.Content;
                    return m.EditableObject;
                }),
                new EditorMenu.EditorOption("Set page's thumnail image","Current thumbnail: "+pagetoedit.Thumbnail,
				async (m)=>
                {
                    var prompt =  await m.CommandContext.Channel.SendMessageAsync("Please reply with the new thumbnail image url for this page or set it to any non-url text to remove it.");
                    SocketMessage result = await m.MenuService.NextMessageAsync(m.CommandContext,TimeSpan.FromMinutes(3));
                    
                    await prompt.DeleteAsync();
                    
                    ((CharPage)m.EditableObject).Thumbnail = result.Content;
                    
                    await result.DeleteAsync();

                    m.CurrentOption.Description = "Current thumbnail: "+result.Content;
                    return m.EditableObject;
                }),
                new EditorMenu.EditorOption("Set page's Large image","Current image: "+pagetoedit.Image,
				async (m)=>
                {
                    var prompt =  await m.CommandContext.Channel.SendMessageAsync("Please reply with the new image url for this page or set it to any non-url text to remove it.");
                    SocketMessage result = await m.MenuService.NextMessageAsync(m.CommandContext,TimeSpan.FromMinutes(3));
                    
                    await prompt.DeleteAsync();
                    
                    ((CharPage)m.EditableObject).Image = result.Content;
                    
                    await result.DeleteAsync();

                    m.CurrentOption.Description = "Current image: "+result.Content;
                    return m.EditableObject;
                }),
				// -------------------
                new EditorMenu.EditorOption("Set Page Color","[Color Picker](https://www.rapidtables.com/web/color/html-color-codes.html).",
				async (m)=>
                {
                    var prompt =  await m.CommandContext.Channel.SendMessageAsync("Please send the __Hex code__ (ie: 00BFFF, not including the #) of the color you want to set. Invalid Hex Codes will be ignored.");
                    SocketMessage result = await m.MenuService.NextMessageAsync(m.CommandContext,TimeSpan.FromMinutes(3));
                    
                    await prompt.DeleteAsync();

                    if(uint.TryParse(result.Content,NumberStyles.HexNumber,null,out uint colorvalue))
                    {
                        var color = new Color(colorvalue);
                        ((CharPage)m.EditableObject).Color = (int)color.RawValue;
                        m.CurrentOption.Description = "[Color Picker](https://www.rapidtables.com/web/color/html-color-codes.html).\nColor Assigned: "+color.R+", "+color.G+", "+color.B+".";
                    }
                    await result.DeleteAsync();

                    return m.EditableObject;
                })
            };

            var menu = new EditorMenu("Editing page "+PageNumber+" of "+character.Name+"'s sheet.",pagetoedit,MenuOption);
			await MenuService.CreateMenu(Context, menu, true);
			var pag = await menu.GetObject();
            if (pag == null)
            {
                var msg1 = await ReplyAsync("Discarded all changes.");
                
                return;
            }
            else
            {
                character.Pages[index] = (CharPage)pag;

                var col = Program.Database.GetCollection<Character>("Characters");
                col.Update(character);
                var msg1 = await ReplyAsync("💾 Saved chanes to "+character.Name+"'s sheet!");
                
                return;
            }
        }
        #endregion
        
		#region Templates
        [Command("SaveTemplate"),Alias("CreateTemplate")]
        [RequireGuildSettings] [RequireContext(ContextType.Guild)]
        [Summary("Saves your current character as a template for anyone in your guild to use.")]
        public async Task newtemplate([Remainder]string TemplateName)
        {
            var guild = Program.Database.GetCollection<SysGuild>("Guilds").FindOne(x=>x.Id==Context.Guild.Id);
            var plrs = Program.Database.GetCollection<SysUser>("Users").IncludeAll();
            if (!plrs.Exists(x=>x.Id==Context.User.Id)) plrs.Insert(new SysUser(){Id=Context.User.Id});
            var plr = plrs.FindOne(x=>x.Id==Context.User.Id);
            if(plr.Active == null)
            {
                var msg1 = await ReplyAsync("You have no active character. Please set your active character by using `"+guild.Prefix+"SetActive CharacterName`.");
                
                return;
            }
            var character = plr.Active;
            if(guild.CharacterTemplates.Exists(x=>x.Name.ToLower()==TemplateName.ToLower()))
            {
                var msg1 = await ReplyAsync("There's already a template in this server that has that exact name!");
                
                return;
            }
            
            var template = new Template()
            {
                Name = TemplateName,
                Owner = Context.User.Id,
                Pages = character.Pages
            };
            guild.CharacterTemplates.Add(template);
            var col = Program.Database.GetCollection<SysGuild>("Guilds");
            col.Update(guild);

            var msg = await ReplyAsync("Created new template \""+TemplateName+"\".");
            
            return;
        }
        [Command("CopyTemplate"),Alias("FromTemplate")]
        [RequireGuildSettings] [RequireContext(ContextType.Guild)]
        [Summary("Create a character based on a template.")]
        public async Task copyTemplate(string TemplateName, [Remainder]string CharacterName)
        {
            var guild = Program.Database.GetCollection<SysGuild>("Guilds").FindOne(x=>x.Id==Context.Guild.Id);
            var plrs = Program.Database.GetCollection<SysUser>("Users").IncludeAll();
            var col = Program.Database.GetCollection<Character>("Characters");
            var All = col.Find(x=>x.Guild==Context.Guild.Id);
            if (!plrs.Exists(x=>x.Id==Context.User.Id)) plrs.Insert(new SysUser(){Id=Context.User.Id});
            var plr = plrs.FindOne(x=>x.Id==Context.User.Id);
            if (All.Any(x=>x.Name.ToLower() == CharacterName.ToLower()))
            {
                var msg2 = await ReplyAsync("There's already a character whose name is \""+CharacterName+"\", please choose a different name.");
                
                return;
            }
            if(!guild.CharacterTemplates.Exists(x=>x.Name.ToLower().StartsWith(TemplateName.ToLower())))
            {
                var msg1 = await ReplyAsync("There's no template on this server whose name starts with \""+TemplateName+"\".");
                
                return;
            }
            var templates  = guild.CharacterTemplates.FindAll(x=>x.Name.ToLower().StartsWith(TemplateName.ToLower()));
            if(templates.Count>1)
            {
				string[] options = templates.Select(x => x.Name).ToArray();
				var menu = new SelectorMenu("Multiple templates were found, please specify which one you're trying to copy from:", options, templates.ToArray());
				await MenuService.CreateMenu(Context, menu, true);
				templates[0] = (Template)await menu.GetSelectedObject();
			}
            Template temp = templates[0];

            var character = new Character()
            {
                Name = CharacterName,
                Pages = temp.Pages,
				Guild = Context.Guild.Id,
				Owner = Context.User.Id
            };
			col.Insert(character);
            plr.Active=character;
            plrs.Update(plr);

            var msg = await ReplyAsync("Created character **"+character.Name+"**. This character has also been assigned as your active character for all edit commands.");
            
        }
		[Command("Templates"),Alias("ListTemplates")]
		[Summary("Shows all the templates currently in this guild.")]
		[RequireGuildSettings] [RequireContext(ContextType.Guild)]
		public async Task AllTemplates()
		{
			var guild = Program.Database.GetCollection<SysGuild>("Guilds").FindOne(x => x.Id == Context.Guild.Id);
			if (guild.CharacterTemplates.Count==0)
			{
				var msg = await ReplyAsync("There are no templates in this server.");
				
			}
			else
			{
				var sb = new StringBuilder();
				foreach (var x in guild.CharacterTemplates)
				{
					var author = Context.Guild.GetUser(x.Owner);
					sb.AppendLine(x.Name + "(By: " + author.Username+")");
				}
				var embed = new EmbedBuilder()
					.WithTitle("Character templates of " + Context.Guild.Name)
					.WithCurrentTimestamp()
					.WithDescription(sb.ToString())
					.Build();
				var msg = await ReplyAsync("", false, embed);
				
			}
		}
        #endregion
    }
}