using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Discord;
using Discord.WebSocket;
using Discord.Commands;
using Discord.Addons.Interactive;
using System.Text;
using Discord.Rest;

namespace SAIL.Classes
{
    public class Controller
    {
        public int Index {get;set;}
        public List<Embed> Pages {get;set;} = new List<Embed>();
        
        public async Task Next(SocketCommandContext c, SocketReaction r, IUserMessage msg)
        {
            if(Index+1 >= Pages.Count) 
            {
                Index = 0;
                await msg.RemoveReactionAsync(r.Emote,r.User.Value);
                return;
            }
            else
            {
                Index++;
                await msg.ModifyAsync(x => x.Embed = Pages.ElementAt(Index));
                await msg.RemoveReactionAsync(r.Emote,r.User.Value);
            }
        }
        public async Task Previous(SocketCommandContext c, SocketReaction r, IUserMessage msg)
        {
            if(Index-1 < 0) 
            {
                Index = Pages.Count()-1;
                await msg.RemoveReactionAsync(r.Emote,r.User.Value);
                return;
            }
            else
            {
                Index--;
                await msg.ModifyAsync(x => x.Embed = Pages.ElementAt(Index));
                await msg.RemoveReactionAsync(r.Emote,r.User.Value);
            }
        }
        public async Task First(SocketReaction r, IUserMessage msg)
        {
            await msg.ModifyAsync(x=> x.Embed = Pages.First());
            await msg.RemoveReactionAsync(r.Emote,r.User.Value);
        }
        public async Task Last(SocketReaction r, IUserMessage msg)
        {
            await msg.ModifyAsync(x=> x.Embed = Pages.Last());
            await msg.RemoveReactionAsync(r.Emote,r.User.Value);
        }
        public async Task Kill(InteractiveService interactive, IUserMessage msg)
        {
            await msg.RemoveAllReactionsAsync();
            interactive.RemoveReactionCallback(msg.Id);
        }
    }
    public class Menu
    {
        public Menu(string _Name, string _Message, MenuOption[] _Options,object obj = null)
        {
            Name = _Name;
            MenuInfo = _Message;
            Options  = _Options;
            Storage = obj;
        }
        public string Name {get;set;}
        public string MenuInfo {get;set;}
        public Menu PreviousMenu {get;set;} = null;
        public MenuOption[] Options {get;set;}
        public bool Active {get; private set;} = true;

        public SocketCommandContext Context {get;private set;}
        public InteractiveService Interactive {get;private set;}

        public object Storage {get;private set;}
        private int Index {get; set;} = 0;
        private Emoji NextButton = new Emoji("⏭");
        private Emoji SelectButton = new Emoji("⏏");
        private Emoji PrevButton = new Emoji("⏮");
        private RestUserMessage Message {get;set;}
        private object Result {get;set;} = null;
        
        public class MenuOption
        {
            public MenuOption(string _Name, Func<Menu,int,object> _Logic = null, string _summary = "", bool _EndsMenu = true)
            {
                Name = _Name;
                Logic = _Logic;
                EndsMenu = _EndsMenu;
                Description = _summary;
            }
            public string Name {get;set;}
            public string Description {get;set;}
            public bool EndsMenu {get;set;} = true;
            public Func<Menu,int,object> Logic {get;set;}
        }
        public async Task<object> StartMenu(SocketCommandContext _Context,InteractiveService _Interactive)
        {
            Context = _Context;
            Interactive = _Interactive;
            Message = await Context.Channel.SendMessageAsync(BuildMenu());
            await Message.AddReactionsAsync(new Emoji[] {PrevButton,SelectButton,NextButton});
            Interactive.AddReactionCallback(Message, new InlineReactionCallback(Interactive,Context,
                new ReactionCallbackData("",null,false,false,TimeSpan.FromMinutes(2),
                async (x) => {await Message.RemoveAllReactionsAsync(); Result = null;})
                    .WithCallback(PrevButton,(x,y)=>SelectPrevious(y))
                    .WithCallback(SelectButton,(x,y) => SelectOption(y,Storage))
                    .WithCallback(NextButton,(x,y) => SelectNext(y))));

            while (Active)
            {
                await Task.Delay(100);
            }
            await Message.DeleteAsync();
            return Result; 
        }
        public async Task ReloadMenu()
        {
            if(Options.Any(x=>x.Description!="")) await Message.ModifyAsync(x=>x.Embed = BuildEmbeddedMenu());
            else await Message.ModifyAsync(x => x.Content = BuildMenu());
        }
        public async Task SelectNext(SocketReaction r)
        {
            await Message.RemoveReactionAsync(r.Emote,r.User.Value);
            if(Index+1 >= Options.Length)
            {
                Index = 0;
            }
            else Index++;
            await ReloadMenu();
        }
        public async Task SelectPrevious(SocketReaction r)
        {
            await Message.RemoveReactionAsync(r.Emote,r.User.Value);
            if(Index-1 <= 0)
            {
                Index = Options.Length-1;
            }
            else Index--;
            await ReloadMenu();
        }
        public async Task SelectOption(SocketReaction r,object input = null)
        {
            await Message.RemoveReactionAsync(r.Emote,r.User.Value);
            Result = Options[Index].Logic?.Invoke(this,Index);
            await ReloadMenu();
            Active = Options[Index].EndsMenu? false : true;
        }
        public string BuildMenu()
        {
            var returnstring = new StringBuilder().AppendLine(MenuInfo);
            for (int i = 0; i < Options.Length;i++)
            {
                returnstring.AppendLine((Index==i?"💠 ":"🔹 ") + Options[i].Name);
            }
            return returnstring.ToString();
        }
        public Embed BuildEmbeddedMenu()
        {
            var Embed = new EmbedBuilder()
                .WithTitle(Name)
                .WithColor(new Color(88, 196, 239))
                .WithDescription(MenuInfo)
                .WithFooter("Use "+PrevButton+" and "+NextButton+" to move the cursor and "+SelectButton+" to select the selected item.");
            for (int i = 0; i < Options.Length;i++)
            {
                Embed.AddField((Index==i?"💠 ":"")+Options[i].Name,Options[i].Description==""?Options[i].Name:Options[i].Description,true);
            }
            return Embed.Build();
        }
    }
}