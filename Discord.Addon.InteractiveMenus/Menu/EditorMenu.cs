using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.Rest;
using Discord.WebSocket;

namespace Discord.Addon.InteractiveMenus
{
	public class EditorMenu : Menu
	{
		private string Title;
		public int Cursor = 0;
		private object StoredObject;
		public List<EditorOption> Options;
		private bool Active = true;
		public async override Task<bool> HandleButtonPress(SocketReaction reaction)
		{
			if (!Buttons.TryGetValue(reaction.Emote, out Task<bool> Logic))
			{
				return false;
			}
			return await Logic;
		}
		/// <summary>
		/// A menu that allows you to edit a single object.
		/// </summary>
		/// <param name="Editor_Title">The title of the embed.</param>
		/// <param name="Objec_To_Edit">The object being edited.</param>
		/// <param name="Editor_Options">The list of options which contain the logic of the editor.</param>
		public EditorMenu(string Editor_Title, object Objec_To_Edit, EditorOption[] Editor_Options)
		{
			Title = Editor_Title;
			StoredObject = Objec_To_Edit;
			Options = Editor_Options.ToList();

			Buttons.Add(new Emoji("⏫"), PreviousOptionAsync());
			Buttons.Add(new Emoji("⏏"), SelectAsync());
			Buttons.Add(new Emoji("⏬"), NextOptionAsync());

			Options.Add(new EditorOption("Save Changes","Save all changes made.",null));
			Options.Add(new EditorOption("Discard Changes", "Discard all changes made.", null));
		}
		public override async Task<RestUserMessage> Initialize(SocketCommandContext commandContext,MenuService menuService)
		{
			Message = await base.Initialize(commandContext, menuService);
			await ReloadMenu();
			return Message;
		}

		public async Task<object> GetObject()
		{
			while (Active) await Task.Delay(1000);
			return StoredObject;
		}
		private async Task<bool> NextOptionAsync()
		{
			if (Cursor + 1 >= Options.Count) Cursor = 0;
			else Cursor--;
			await ReloadMenu();
			return false;
		}

		private async Task<bool> SelectAsync()
		{
			switch (Options[Cursor].Name)
			{
				case "Save Changes":
					Active = false;
					return true;
				case "Discard Changes":
					StoredObject = null;
					Active = false;
					return true;
				default:
					var context = new OptionContext(Context, Service, StoredObject);
					StoredObject = await Options[Cursor].Logic(context);
					break;
			}
			return false;
		}
		private async Task<bool> PreviousOptionAsync()
		{
			if (Cursor - 1 < 0) Cursor = Options.Count - 1;
			else Cursor--;
			await ReloadMenu();
			return false;
		}
		private async Task ReloadMenu()
		{
			var eb = new EmbedBuilder()
				.WithTitle(Title)
				.WithFooter("Use ⏫/⏬ to move the cursor and ⏏ to select.");
			for(int i = 0;i<Options.Count; i++)
			{
				eb.AddField(((Cursor==i)? "🔹 " : "")+Options[i].Name, Options[i].Description);
			}
			await Message.ModifyAsync(x => x.Content = " ");
			await Message.ModifyAsync(x => x.Embed = eb.Build());
		}
		public class EditorOption
		{
			public Func<OptionContext, Task<object>> Logic;
			public string Name;
			public string Description;
			/// <summary>
			/// An option for the Editor menu that is displayed a field in the Editor embed.
			/// </summary>
			/// <param name="Option_Name">The title of the option for the embed.</param>
			/// <param name="Option_Description">The contents of the embedded option.</param>
			/// <param name="Option_Logic">The logic that happens when this option is selected.</param>
			public EditorOption(string Option_Name, string Option_Description,Func<OptionContext,Task<object>>Option_Logic)
			{
				Logic = Option_Logic;
				Name = Option_Name;
				Description = Option_Description;
			}
		}
		public class OptionContext
		{
			/// <summary>
			/// The Command Context of the message that triggered the menu.
			/// </summary>
			public SocketCommandContext CommandContext { get; private set; }
			/// <summary>
			/// The Menu service.
			/// </summary>
			public MenuService MenuService { get; private set; }
			/// <summary>
			/// The object being edited.
			/// </summary>
			public object EditableObject { get; set; }
			public OptionContext(SocketCommandContext commandContext, MenuService service, object editable)
			{
				CommandContext = commandContext;
				MenuService = service;
				EditableObject = editable;
			}
		}
	}
}
