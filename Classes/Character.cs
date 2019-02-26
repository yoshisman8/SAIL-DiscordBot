using System;
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
    public class Character
    {
        [BsonId]
        public int Id {get;set;}
        public ulong Owner {get;set;}
        public ulong Guild {get;set;}
        public string Name {get;set;}
        public List<CharPage> Pages {get;set;} = new List<CharPage>();
        public async Task<List<Embed>> PagesToEmbed()
        {
            List<Embed> embeds = new List<Embed>();
            foreach (var c in Pages)
            {
                var eb = new EmbedBuilder()
                    .WithColor(c.Color)
                    .WithTitle(Name + "("+c.Subtitle+")")
                    .WithImageUrl(c.Image)
                    .WithThumbnailUrl(c.Thumbnail);
                foreach(var f in c.Fields)
                {
                    eb.AddField(f.Title,f.Content,f.Inline);
                }
                embeds.Add(eb.Build());
            }
            return embeds;
        }
    }
    public class CharPage
    {
        public Field[] Fields {get;set;} = new Field[24];
        public Color Color {get;set;} = Color.DarkGrey;
        public string Image {get;set;} = "";
        public string Thumbnail {get;set;} = "";
        public string Subtitle {get;set;} = "";
    }

    public class Field
    {
        public string Title {get;set;}
        public string Content {get;set;}
        public bool Inline {get;set;} = false;
    }
}