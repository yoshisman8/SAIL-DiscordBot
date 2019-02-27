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

namespace SAIL.Classes
{
    public class SysGuild
    {
        [BsonId]
        public ulong Id {get;set;}
        public string Prefix {get;set;} = "!";
        public List<Module> Modules {get;set;} = new List<Module>();
        public ListMode ListMode {get;set;} = ListMode.None;
        public List<ulong> Channels {get;set;} = new List<ulong>();
        public ulong MessageChannel {get;set;} = 0;
        public bool Notifications {get;set;} = true;
        [BsonIgnore]
        private SocketGuild Guild {get;set;}
        [BsonIgnore]
        private List<ITextChannel> LoadedChannels {get;set;} = new List<ITextChannel>();
        public Embed GetSettingsPage(CommandService commandService)
        {
            var sb = new StringBuilder();
            var embed = new EmbedBuilder()
                .WithTitle(Guild.Name+"'s Control Panel")
                .WithThumbnailUrl(Guild.IconUrl);
                embed.AddField("Current Prefix",Prefix,true);
            switch (ListMode)
            {
                case ListMode.None:
                    if (LoadedChannels != null && LoadedChannels.Count > 0) embed.AddField("Currently not filtering based on channel.",string.Join(", ",LoadedChannels.Select(x=>x.Name),true));
                    else embed.AddField("Currently not filtering based on channel.","The list is Empty.",true);
                    break;
                case ListMode.Whitelist:
                    if (LoadedChannels != null && LoadedChannels.Count > 0) embed.AddField("Currently filtering using a Whitelist.",string.Join(", ",LoadedChannels.Select(x=>x.Name),true));
                    else embed.AddField("Currently filtering using a Whitelist.","The list is Empty.",true);
                    break;
                case ListMode.Blacklist:
                    if (LoadedChannels != null && LoadedChannels.Count > 0) embed.AddField("Currently filtering using a Blacklist.",string.Join(", ",LoadedChannels.Select(x=>x.Name),true));
                    else embed.AddField("Currently filtering using a Blacklist.","The list is Empty.",true);
                    break;
            }
            foreach(var x in Modules)
            {
                embed.AddField(x.Name+" "+(x.Active? "✅":"⛔"),commandService.Modules.Single(y=>y.Name == x.Name).Summary,true);
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
    }
    public class Module
    {
        public string Name {get;set;}
        public bool Active {get;set;} = true;
    }
    public enum ListMode {Blacklist, Whitelist, None}
}