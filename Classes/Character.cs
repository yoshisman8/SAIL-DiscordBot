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
        public string Name {get;set;}
        public List<CharPage> Pages {get;set;} = new List<CharPage>();
    }
    public class CharPage
    {
        public Field[] Fields {get;set;} = new Field[24];
        public string Image {get;set;} = "";
        public string Thumbnail {get;set;} = "";
        public string Title {get;set;} = "";
    }

    public class Field
    {
        public string Title {get;set;}
        public string Content {get;set;}
        public bool Inline {get;set;} = false;
    }
}