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
    public class SysGuild
    {
        [BsonId]
        public ulong Id {get;set;}
        public string Prefix {get;set;} = "!";
        public int ListMode {get;set;} = 0;
        public List<ulong> Channels {get;set;} = new List<ulong>();
        public List<Module> Modules {get;set;} = new List<Module>();
        public ulong ErrorChannel {get;set;} = 0;
    }
    public class Module
    {
        public string Name {get;set;}
        public bool Active {get;set;} = true;
    }
}