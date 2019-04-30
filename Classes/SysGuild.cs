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

namespace SAIL.Classes
{
    public class SysGuild
    {
        [BsonId]
        public ulong Id {get;set;}
        public string Prefix {get;set;} = "!";
        public Dictionary<ModuleInfo,bool> Modules {get;set;} = new Dictionary<ModuleInfo,bool>();
        public ListMode ListMode {get;set;} = ListMode.None;
        public List<ulong> Channels {get;set;} = new List<ulong>();
        public ulong NotificationChannel {get;set;} = 0;
        public bool Notifications {get;set;} = true;
        public List<GuildEvent> Events {get;set;} = new List<GuildEvent>();

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
            foreach(var x in Modules)
            {
                embed.AddField(x.Key.Name+" "+(x.Value? "✅":"⛔"),"```"+x.Key.Summary+"```",true);
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
                .WithTitle("[EVENT] "+Event.Name)
                .WithDescription(Event.Description)
                .AddField("Event Time",Event.Repeating == RepeatingState.Repeating?"This event is set to happen once and will not be repeated.":"This event happens on "+Event.Time.Get12h()+" every "+string.Join(",",Event.Days));
            await ch.SendMessageAsync("",false,embed.Build());
        }
    }

    public class GuildEvent
    {
        public HourOfTheDay Time {get;set;}
        public List<DayOfWeek> Days {get;set;} = new List<DayOfWeek>();
        public string Name {get;set;}
        public string Description {get;set;}
        public RepeatingState Repeating {get;set;} = RepeatingState.Unset;
    }

    public class Module
    {
        public string Name {get;set;}
        public bool Active {get;set;} = true;
    }
    public enum ListMode {Blacklist, Whitelist, None}
    public enum RepeatingState {Repeating,NonRepeating, Unset}
}