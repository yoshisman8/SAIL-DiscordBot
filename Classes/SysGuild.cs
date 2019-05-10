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
        public List<Module> CommandModules {get;set;} = new List<Module>();
        public ListMode ListMode {get;set;} = ListMode.None;
        public List<ulong> Channels {get;set;} = new List<ulong>();
        public ulong NotificationChannel {get;set;} = 0;
        public bool Notifications {get;set;} = true;
        public List<GuildEvent> Events {get;set;} = new List<GuildEvent>();
        [BsonRef("Users")]
        public List<SysUser> Users {get;set;} = new List<SysUser>();

        [BsonIgnore]
        private SocketGuild Guild {get;set;}
        [BsonIgnore]
        private List<ITextChannel> LoadedChannels {get;set;} = new List<ITextChannel>();
        public Embed GetSettingsPage(CommandService commandService)
        {
            var sb = new StringBuilder();
            var embed = new EmbedBuilder()
                .WithTitle(Guild.Name+"'s Control Panel")
                .WithDescription("Current Prefix: `"+Prefix+"`.")
                .WithThumbnailUrl(Guild.IconUrl);
            switch (ListMode)
            {
                case ListMode.None:
                    if (LoadedChannels != null && LoadedChannels.Count > 0) embed.AddField("Currently not filtering based on channel.","```"+string.Join(", ",LoadedChannels.Select(x=>x.Mention)+"```",true));
                    else embed.AddField("Currently not filtering based on channel.","The list is Empty.",true);
                    break;
                case ListMode.Whitelist:
                    if (LoadedChannels != null && LoadedChannels.Count > 0) embed.AddField("Currently filtering using a Whitelist.","```"+string.Join(", ",LoadedChannels.Select(x=>x.Mention)+"```",true));
                    else embed.AddField("Currently filtering using a Whitelist.","The list is Empty.",true);
                    break;
                case ListMode.Blacklist:
                    if (LoadedChannels != null && LoadedChannels.Count > 0) embed.AddField("Currently filtering using a Blacklist.","```"+string.Join(", ",LoadedChannels.Select(x=>x.Mention)+"```",true));
                    else embed.AddField("Currently filtering using a Blacklist.","The list is Empty.",true);
                    break;
            }
            embed.AddField(Notifications?"Notifications Active ✅":"Notifications Disabled ⛔",NotificationChannel==0?"No channel has been set.":Guild.GetTextChannel(NotificationChannel).Mention);
            foreach(var x in CommandModules)
            {
                embed.AddField(x.Name+" "+(x.Value? "✅":"⛔"),"```"+x.Summary+"```",true);
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
        }
        public async Task PrintEvent(DiscordSocketClient Client,GuildEvent Event)
        {
            var guild = Client.GetGuild(Id);
            var ch = guild.GetTextChannel(NotificationChannel);
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

    public class Module
    {
        public string Name {get;set;}
        public string Summary {get;set;}
        public bool Value {get;set;} = true;
    }
    public enum ListMode {Blacklist, Whitelist, None}
    public enum RepeatingState {Unset = -1,Weekly, Monhtly,Anually,Once}
}