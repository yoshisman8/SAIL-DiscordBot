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
        public string Summary { get; set; } = "";
        [BsonRef("Characters")]
        public List<Character> Characters { get; set; } = new List<Character>() { };
        public string MapUrl { get; set; } = "";
    }
}
