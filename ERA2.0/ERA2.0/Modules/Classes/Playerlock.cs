using System;
using System.Collections.Generic;
using System.Text;
using Discord;
using LiteDB;

namespace ERA20.Modules.Classes
{
    public class Playerlock
    {
        public LiteDatabase Database { get; set; }

        public Character GetPlayer(ulong playerID)
        {
            var player = Database.GetCollection<Player>("Players").FindOne(x => x.User == playerID);
            var character = Database.GetCollection<Character>("Characters").FindOne(x => x.CharacterId == player.CharacterID);
            return character;
        }
    }
    public class Player
    {
        public ulong User { get; set; }
        public ObjectId CharacterID { get; set; }
    }
}
