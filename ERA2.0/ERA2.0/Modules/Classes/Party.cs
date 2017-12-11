using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Discord;
using Discord.Commands;
using LiteDB;
using System.Threading.Tasks;

namespace ERA20.Modules.Classes
{
    public class Party
    {
        [BsonId]
        public int PartyId { get; set; }
        public string Name { get; set; } = "";
        public string Summary { get; set; } = "This party is Brand new! The summary of their last adventure will be added here by the DM eventually!";
        [BsonRef("Characters")]
        public List<Character> Characters { get; set; } = new List<Character>() { };
        public string MapUrl { get; set; } = "https://cdn.discordapp.com/attachments/314912846037254144/389644725004533771/1600.png";
    }
}
