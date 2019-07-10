using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using LiteDB;

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
        public List<Embed> PagesToEmbed(SocketCommandContext context)
        {
            List<Embed> embeds = new List<Embed>();
            var user = context.Client.GetUser(Owner);
            foreach (var c in Pages)
            {  
                Color colr = new Color((uint)c.Color);
                var eb = new EmbedBuilder()
                    .WithColor(colr)
                    .WithDescription(c.Summary)
                    .WithTitle(Name + " ("+(Pages.IndexOf(c)+1)+"/"+Pages.Count+")")
                    .WithImageUrl(c.Image)
                    .WithThumbnailUrl(c.Thumbnail)
                    .WithFooter("Made by: "+user.ToString(),user.GetAvatarUrl());
                foreach(var f in c.Fields)
                {
                    eb.AddField(f.Title,f.Content,f.Inline);
                }
                embeds.Add(eb.Build());
            }
            return embeds;
        }
        public Embed GetPage(int PageNumber, SocketCommandContext context)
        {
            var c = Pages.ElementAt(PageNumber);
            var user = context.Client.GetUser(Owner);
            Color colr = new Color((uint)c.Color);
            var eb = new EmbedBuilder()
                    .WithColor(colr)
                    .WithTitle(Name + " ("+(Pages.IndexOf(c)+1)+"/"+Pages.Count+")")
                    .WithImageUrl(c.Image)
                    .WithThumbnailUrl(c.Thumbnail)
                    .WithFooter("Made by: "+user.ToString(),user.GetAvatarUrl());
                foreach(var f in c.Fields)
                {
                    eb.AddField(f.Title,f.Content,f.Inline);
                }
            return eb.Build();
        }
    }
    public class CharPage
    {
        public List<Field> Fields {get;set;} = new List<Field>();
        public string Summary {get;set;} = "";
        public int Color {get;set;} = (int)0x87CEFA;
        public string Image {get;set;} = "";
        public string Thumbnail {get;set;} = "";
    }

    public class Field
    {
        public string Title {get;set;}
        public string Content {get;set;}
        public bool Inline {get;set;} = false;
    }

    public class Template
    {
        public string Name {get;set;}
        public ulong Owner {get;set;}
        public List<CharPage> Pages {get;set;} = new List<CharPage>();
    }
}