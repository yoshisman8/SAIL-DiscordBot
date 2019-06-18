using System;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using LiteDB;
using Discord.Addons.CommandCache;
using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.WebSocket;
using Discord.Rest;
using Discord;
using static SAIL.Classes.DateTimeExtension;
using System.Globalization;

namespace SAIL.Classes
{
    public class SysUser
    {
        [BsonId]
        public ulong Id {get;set;}
        [BsonRef("Characters")]
        public Character Active {get;set;}
    }

    public class SysGuild
    {
        [BsonId]
        public ulong Id {get;set;}
        public string Prefix {get;set;} = "!";
        public Dictionary<string,bool> CommandModules {get;set;} = new Dictionary<string, bool>();
        public ListMode ListMode {get;set;} = ListMode.None;
        public List<ulong> Channels {get;set;} = new List<ulong>();
		public NotificationSettings Notifications { get; set; } = new NotificationSettings();
		public List<Template> CharacterTemplates {get;set;} = new List<Template>();
		public List<ulong> AssignableRoles { get; set; } = new List<ulong>();

        [BsonIgnore]
        private SocketGuild Guild {get;set;}
        [BsonIgnore]
        public List<ITextChannel> LoadedChannels {get; private set;} = new List<ITextChannel>();
        public Embed Summary(CommandService commandService)
        {
            var sb = new StringBuilder();
            var embed = new EmbedBuilder()
                .WithTitle(Guild.Name)
                .WithDescription("Current Prefix: `"+Prefix+"`.")
                .WithThumbnailUrl(Guild.IconUrl);
			//White/Black list
			sb.Clear();
			switch (ListMode)
            {
                case ListMode.None:
                    sb.AppendLine("Currently listening for commands on all channels.");
                    break;
                case ListMode.Whitelist:
					sb.AppendLine("Currently only listening for commands on the following channels:");
					sb.AppendLine(string.Join("\n", LoadedChannels.Select(x => x.Name)));
					break;
				case ListMode.Blacklist:
					sb.AppendLine("Currently ignoring commands on the following channels:");
					sb.AppendLine(string.Join("\n", LoadedChannels.Select(x => x.Name)));
					break;
            }
			embed.AddField("Command Listening", sb.ToString(), true);

			//Notifications
			sb.Clear();
			sb.AppendLine("Notifications " + (Notifications.Module ? "Enabled." : "Disabled"));
			sb.AppendLine((Notifications.LoadedNotifChannel != null) ? "Notifications being sent to "+Notifications.LoadedNotifChannel.Name : "No channel is set to receive notifications.");
			sb.AppendLine("User Joined Message: \n" + (Notifications.JoinedMsg== "" ? "Disabled": "`\""+Notifications.JoinedMsg+ "\"`"));
			sb.AppendLine("User Left Message: \n" + (Notifications.LeftMsg == "" ? "Disabled" : "`\""+Notifications.LeftMsg+ "\"`"));
			embed.AddField("Notification Settings",sb.ToString(), true);

			//Statistics
			sb.Clear();
			var E = Program.Database.GetCollection<GuildEvent>("Events").Find(x => x.Server.Id == Id);
			sb.AppendLine("Events: "+ ((E != null) ? E.Count().ToString():"0"));

			var C = Program.Database.GetCollection<Character>("Characters").Find(x => x.Guild == Id);
			sb.AppendLine("Characters: " + ((C != null) ? C.Count().ToString() : "0"));

			sb.AppendLine("Character Templates: " + CharacterTemplates.Count);

			var Q = Program.Database.GetCollection<Quote>("Quotes").Find(x => x.Guild == Id);
			sb.AppendLine("Quotes: " + ((Q != null) ? Q.Count().ToString() : "0"));

			embed.AddField("Statistics", sb.ToString(), true);
			//Module Listing
			sb.Clear();
			foreach(var x in CommandModules)
            {
                sb.AppendLine(x.Key+" "+(x.Value? @" \✅":@" \⛔"));
            }
			embed.AddField("Command Modules", sb.ToString(),true);
			//List Templates if any
			if(CharacterTemplates.Count>0)
			{
				embed.AddField("Character Templates", string.Join("/n", CharacterTemplates.Select(x => x.Name)),true);
			}
            return embed.Build();
        }
        public void Load(SocketCommandContext context)
        {
            Guild = context.Client.GetGuild(Id);
            foreach(var x in Channels)
            {
                LoadedChannels.Add(Guild.GetTextChannel(x));
            }
			Notifications.LoadedNotifChannel = Guild.GetTextChannel(Notifications.NotificationChannel);	
        }
        public async Task PrintEvent(DiscordSocketClient Client,GuildEvent Event)
        {
            var guild = Client.GetGuild(Id);
            var ch = guild.GetTextChannel(Notifications.NotificationChannel);
			if (ch == null) return;
            var embed = new EmbedBuilder()
                .WithTitle(Event.Name)
                .WithDescription(Event.Description)
                .WithColor(Color.LightOrange);
            switch(Event.Repeating)
            {
                case RepeatingState.Anually:
                    embed.AddField("When is it happening?","Every "+Event.Date.ToString("MMMM")+" the "+Event.Date.Day.ToPlacement()+" at "+Event.Date.ToString("hh:mm tt"));
                    break;
                case RepeatingState.Monhtly:
                    embed.AddField("When is it happening?","The "+Event.Date.Day.ToPlacement()+" of every month at "+" at "+Event.Date.ToString("hh:mm tt"));
                    break;
                case RepeatingState.Weekly:
                    embed.AddField("When is it happening?","Every "+Event.Date.ToString("DDDD")+" at "+Event.Date.ToString("hh:mm tt"));
                    break;
                case RepeatingState.Once:
                    embed.AddField("When is it happening?","On "+Event.Date.ToString("DD/MMM/YYYY")+" at "+Event.Date.ToString("hh:mm tt")+"UTC");
                    break;
            }
            await ch.SendMessageAsync("",false,embed.Build());
        }
    }
	public class NotificationSettings
	{
		public bool Module { get; set; } = true;
		public ulong NotificationChannel { get; set; } = 0;
		public string JoinedMsg { get; set; } = "{user} Has joined the server!";
		public string LeftMsg { get; set; } = "";
		[BsonIgnore]
		public ITextChannel LoadedNotifChannel { get; set; }
	}
	public enum ListMode {Blacklist, Whitelist, None}
    public enum RepeatingState {Unset = -1,Weekly, Monhtly,Anually,Once}
}