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
        public Controller(IEnumerable<Embed> _Pages,string _endmsg = "",RestUserMessage _Message = null)
        {
            if(_Message!=null) Message = _Message;
            if(_Pages!=null)Pages = _Pages.ToList();
            if(!_endmsg.NullorEmpty()) EndMessage =_endmsg;
        }
        public int Index {get;set;} = 0;
        public List<Embed> Pages {get;set;} = new List<Embed>();
        public RestUserMessage Message {get; private set;} = null;
        public string EndMessage {get;set;} = "Menu closed";
        private Emoji First =  new Emoji("⏮");
        private Emoji Previous =  new Emoji("⏪");
        private Emoji End = new Emoji("⏹");
        private Emoji Next =  new Emoji("⏩");
        private Emoji Last = new Emoji("⏭");

        private Emoji[] Buttons {
            get
            {
                return new Emoji[]{First,Previous,End,Next,Last};
            }}
        public async Task<RestUserMessage> Start(SocketCommandContext Context,InteractiveService Interactive)
        {
            if(Message==null) Message = await Context.Channel.SendMessageAsync("Loading...");

            await Message.AddReactionsAsync(Buttons);
            await Message.ModifyAsync(x=>x.Content = " ");
            await Message.ModifyAsync(x=> x.Embed = Pages.First());

            Interactive.AddReactionCallback(Message,new InlineReactionCallback(Interactive,Context,
            new ReactionCallbackData("",null,false,false,TimeSpan.FromMinutes(3),(ctx)=>Kill(Interactive,Message))
                .WithCallback(First,(ctx,rea)=>FirstP(rea,Message))
                .WithCallback(Previous,(ctx,rea)=>PreviousP(ctx,rea,Message))
                .WithCallback(End,(ctx,rea)=>Kill(Interactive,Message))
                .WithCallback(Next,(ctx,rea)=>NextP(ctx,rea,Message))
                .WithCallback(Last,(ctx,rea)=>LastP(rea,Message))));
            return Message;
        }
        public async Task NextP(SocketCommandContext c, SocketReaction r, IUserMessage msg)
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
        public async Task PreviousP(SocketCommandContext c, SocketReaction r, IUserMessage msg)
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
        public async Task FirstP(SocketReaction r, IUserMessage msg)
        {
            await msg.ModifyAsync(x=> x.Embed = Pages.First());
            await msg.RemoveReactionAsync(r.Emote,r.User.Value);
        }
        public async Task LastP(SocketReaction r, IUserMessage msg)
        {
            await msg.ModifyAsync(x=> x.Embed = Pages.Last());
            await msg.RemoveReactionAsync(r.Emote,r.User.Value);
        }
        public async Task Kill(InteractiveService interactive, IUserMessage msg)
        {
            await msg.RemoveAllReactionsAsync();
            interactive.RemoveReactionCallback(msg.Id);
            await msg.ModifyAsync(x=>x.Embed = null);
            await msg.ModifyAsync(x=>x.Content = EndMessage);
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
        public MenuOption[] Options {get;set;}
        public bool Active {get; private set;} = true;

        public SocketCommandContext Context {get;private set;}
        public InteractiveService Interactive {get;private set;}

        public object Storage {get;private set;}
        private int Index {get; set;} = 0;
        private Emoji NextButton = new Emoji("⏬");
        private Emoji SelectButton = new Emoji("⏏");
        private Emoji PrevButton = new Emoji("⏫");
        private RestUserMessage Message {get;set;}
        private Task<object> Result {get;set;} = null;
        
        public class MenuOption
        {
            public MenuOption(string _Name, Func<Menu,int,Task<object>> _Logic = null, string _summary = "", bool _EndsMenu = true)
            {
                Name = _Name;
                Logic = _Logic;
                EndsMenu = _EndsMenu;
                Description = _summary;
            }
            public string Name {get;set;}
            public string Description {get;set;}
            public bool EndsMenu {get;set;} = true;
            public Func<Menu,int,Task<object>> Logic {get;set;}
        }
        public async Task<object> StartMenu(SocketCommandContext _Context,InteractiveService _Interactive)
        {
            Context = _Context;
            Interactive = _Interactive;
            Message = await Context.Channel.SendMessageAsync("Loading...");
            await ReloadMenu();
            await Message.AddReactionsAsync(new Emoji[] {PrevButton,SelectButton,NextButton});
            Interactive.AddReactionCallback(Message, new InlineReactionCallback(Interactive,Context,
                new ReactionCallbackData("",null,false,false,TimeSpan.FromMinutes(2),
                async (x) => {await Message.RemoveAllReactionsAsync(); Result = null;})
                    .WithCallback(PrevButton,async (x,y) => await SelectPrevious(y))
                    .WithCallback(SelectButton,(x,y) => SelectOption(y,Storage))
                    .WithCallback(NextButton,async (x,y) => await SelectNext(y))));

            while (Active)
            {
                await Task.Delay(100);
            }
            await Message.DeleteAsync();
            return Result.Result; 
        }
        public async Task ReloadMenu()
        {
            if(Options.Any(x=>x.Description!="")) 
            {
                await Message.ModifyAsync(x=>x.Content = " ");
                await Message.ModifyAsync(x=>x.Embed = BuildEmbeddedMenu());
            }
            else 
            {
                await Message.ModifyAsync(x=>x.Embed = null);
                await Message.ModifyAsync(x => x.Content = BuildMenu());
            }
        }
        public async Task SelectNext(SocketReaction r)
        {
            await Task.Delay(100);
            await Message.RemoveReactionAsync(r.Emote,r.User.Value);
            await Task.Delay(100);
            if(Index+1 >= Options.Length)
            {
                Index = 0;
            }
            else Index++;
            await ReloadMenu();
        }
        public async Task SelectPrevious(SocketReaction r)
        {
            await Task.Delay(100);
            await Message.RemoveReactionAsync(r.Emote,r.User.Value);
            await Task.Delay(100);
            if(Index-1 < 0)
            {
                Index = Options.Length-1;
            }
            else Index--;
            await ReloadMenu();
        }
        public async Task SelectOption(SocketReaction r,object input = null)
        {
            await Message.RemoveReactionAsync(r.Emote,r.User.Value);
            Result = Options[Index].Logic.Invoke(this,Index);
            Active = Options[Index].EndsMenu? false : true;
            
        }
        public string BuildMenu()
        {
            var returnstring = new StringBuilder().AppendLine(MenuInfo);
            for (int i = 0; i < Options.Length;i++)
            {
                returnstring.AppendLine(Options[i].Name + (Index == i ? "💠 " : "🔹 "));
            }
            return returnstring.ToString();
        }
        public Embed BuildEmbeddedMenu()
        {
            var Embed = new EmbedBuilder()
                .WithTitle(Name)
                .WithColor(new Color(88, 196, 239))
                .WithDescription(MenuInfo)
                .WithFooter("Use "+PrevButton+"/"+NextButton+" to move the cursor and "+SelectButton+" to select.");
            for (int i = 0; i < Options.Length;i++)
            {
                Embed.AddField((Index==i?"💠 ":"")+Options[i].Name,Options[i].Description==""?Options[i].Name:Options[i].Description);
            }
            return Embed.Build();
        }
    }
	public class PagedMenu
	{
		public PagedMenu(string _Name, string _Message, List<MenuOption[]> _Options, object obj = null)
		{
			Name = _Name;
			MenuInfo = _Message;
			Storage = obj;
			Options = new MenuOption[_Options.Count][];
			for (int i = 0;i<_Options.Count;i++)
			{
				Options[i] = _Options[i];
			}
		}
		public string Name { get; set; }
		public string MenuInfo { get; set; }
		public MenuOption[][] Options { get; set; }
		public bool Active { get; private set; } = true;

		public SocketCommandContext Context { get; private set; }
		public InteractiveService Interactive { get; private set; }

		public object Storage { get; private set; }

		private int Index { get; set; } = 0;
		private int PageIndex { get; set; } = 0;
		private Emoji NextPageButton = new Emoji("⏪");
		private Emoji NextButton = new Emoji("⏬");
		private Emoji SelectButton = new Emoji("⏏");
		private Emoji PrevButton = new Emoji("⏫");
		private Emoji PrevPageButton = new Emoji("⏩");
		private Emoji[] Buttons
		{
			get
			{
				return new Emoji[] { PrevPageButton, PrevButton, SelectButton, NextButton, NextPageButton };
			}
		}
		private RestUserMessage Message { get; set; }
		private Task<object> Result { get; set; } = null;

		public class MenuOption
		{
			public MenuOption(string _Name, Func<PagedMenu, int,int, Task<object>> _Logic = null, string _summary = "", bool _EndsMenu = true)
			{
				Name = _Name;
				Logic = _Logic;
				EndsMenu = _EndsMenu;
				Description = _summary;
			}
			public string Name { get; set; }
			public string Description { get; set; }
			public bool EndsMenu { get; set; } = true;
			public Func<PagedMenu,int,int, Task<object>> Logic { get; set; }
		}
		public async Task<object> StartMenu(SocketCommandContext _Context, InteractiveService _Interactive)
		{
			Context = _Context;
			Interactive = _Interactive;
			Message = await Context.Channel.SendMessageAsync("Loading...");
			await ReloadMenu();
			await Message.AddReactionsAsync(Buttons);

			Interactive.AddReactionCallback(Message, new InlineReactionCallback(Interactive, Context,
				new ReactionCallbackData("", null, false, false, TimeSpan.FromMinutes(2),
				async (x) => { await Message.RemoveAllReactionsAsync(); Result = null; })
					.WithCallback(PrevButton, async (x, y) => await SelectPrevious(y))
					.WithCallback(SelectButton, (x, y) => SelectOption(y, Storage))
					.WithCallback(NextButton, async (x, y) => await SelectNext(y))));

			while (Active)
			{
				await Task.Delay(100);
			}
			await Message.DeleteAsync();
			return Result.Result;
		}
		public async Task ReloadMenu()
		{
			if (Options.Any(x => x.Any(y=>y.Description != "")))
			{
				await Message.ModifyAsync(x => x.Content = " ");
				await Message.ModifyAsync(x => x.Embed = BuildEmbeddedMenu());
			}
			else
			{
				await Message.ModifyAsync(x => x.Embed = null);
				await Message.ModifyAsync(x => x.Content = BuildMenu());
			}
		}
		public async Task SelectNext(SocketReaction r)
		{
			await Task.Delay(100);
			await Message.RemoveReactionAsync(r.Emote, r.User.Value);
			await Task.Delay(100);
			if (Index + 1 >= Options[PageIndex].Length)
			{
				Index = 0;
			}
			else Index++;
			await ReloadMenu();
		}
		public async Task SelectPrevious(SocketReaction r)
		{
			await Task.Delay(100);
			await Message.RemoveReactionAsync(r.Emote, r.User.Value);
			await Task.Delay(100);
			if (Index - 1 < 0)
			{
				Index = Options[PageIndex].Length;
			}
			else Index--;
			await ReloadMenu();
		}
		public async Task NextPage(SocketReaction r)
		{
			await Task.Delay(100);
			await Message.RemoveReactionAsync(r.Emote, r.User.Value);
			await Task.Delay(100);
			if (PageIndex + 1 >= Options.GetLength(0))
			{
				Index = 0;
			}
			else PageIndex++;
			await ReloadMenu();
		}
		public async Task PrevPage(SocketReaction r)
		{
			await Task.Delay(100);
			await Message.RemoveReactionAsync(r.Emote, r.User.Value);
			await Task.Delay(100);
			if (PageIndex -1 < 0)
			{
				Index = 0;
			}
			else PageIndex--;
			await ReloadMenu();
		}
		public async Task SelectOption(SocketReaction r, object input = null)
		{
			await Message.RemoveReactionAsync(r.Emote, r.User.Value);
			Result = Options[PageIndex][Index].Logic.Invoke(this, PageIndex, Index);
			Active = Options[PageIndex][Index].EndsMenu ? false : true;

		}
		public string BuildMenu()
		{
			var returnstring = new StringBuilder().AppendLine(MenuInfo);
			for (int i = 0; i < Options.Length; i++)
			{
				returnstring.AppendLine(Options[PageIndex][i].Name + (Index == i ? "💠 " : "🔹 "));
			}
			return returnstring.ToString();
		}
		public Embed BuildEmbeddedMenu()
		{
			var Embed = new EmbedBuilder()
				.WithTitle(Name)
				.WithColor(new Color(88, 196, 239))
				.WithDescription(MenuInfo)
				.WithFooter("Use " + PrevButton + "/" + NextButton + " to move the cursor and " + SelectButton + " to select.");
			for (int i = 0; i < Options.Length; i++)
			{
				Embed.AddField((Index == i ? "💠 " : "") + Options[PageIndex][i].Name, Options[PageIndex][i].Description == "" ? Options[PageIndex][i].Name : Options[PageIndex][i].Description);
			}
			return Embed.Build();
		}
	}
}