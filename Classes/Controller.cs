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
        public Emoji NextButton = new Emoji("⏭");
        public Emoji PrevButton = new Emoji("⏮");
        public Emoji SelectButton = new Emoji("⏏");
        public Menu PreviousMenu {get;set;} = null;
        private int Index {get; set;} = 0;
        public MenuOption[] Options {get;set;}

        private RestUserMessage Message {get;set;}
        private object Storage {get;set;}
        private object Result {get;set;} = new Exclude();
        private bool Active;
        
        public class MenuOption
        {
            public MenuOption(string Name, Func<int,object,object> _Logic = null, bool _EndsMenu = true)
            {
                Option = Name;
                Logic = _Logic;
                EndsMenu = _EndsMenu;
            }
            public string Option {get;set;}
            public bool EndsMenu {get;set;}
            public Func<int,object,object> Logic {get;set;}
        }
        public async Task<object> StartMenu(SocketCommandContext Context,InteractiveService Interactive)
        {
            
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
                await Task.Delay(-1);
            }
            return Result; 
        }
        public async Task ReloadMenu()
        {
            await Message.ModifyAsync(x => x = new MessageProperties(){Content = BuildMenu()});
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
            Result = Options[Index].Logic?.Invoke(Index,input);
            Active = Options[Index].EndsMenu? false : true;
        }
        public string BuildMenu()
        {
            var returnstring = new StringBuilder().Append(MenuInfo);
            for (int i = 0; i > Options.Length-1;i++)
            {
                returnstring.Append((Index==i?"• ":"> ")+Options[i].Option);
            }
            return returnstring.ToString();
        }
    }
}