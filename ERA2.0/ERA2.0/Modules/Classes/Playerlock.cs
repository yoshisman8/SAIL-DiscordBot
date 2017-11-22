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

        public Player GetPlayer(ulong playerID)
        {
            var col = Database.GetCollection<Player>("Players");
            if (col.Exists(x => x.User == playerID))
            {
                var P = col.FindOne(x => x.User == playerID);
                return P;
            }
            else
            {
                throw new Exception("This Player has no character locked!");
            }
        }
        public void Lock(ulong Player, Character character)
        {
            var col = Database.GetCollection<Player>("Players");
            if (col.Exists(x => x.User == Player))
            {
                var P = col.FindOne(x => x.User == Player);
                P.Character = character;
            }
            else
            {
                var P = new Player
                {
                    Character = character,
                    User = Player
                };
                col.Insert(P);
            }
        }
    }
    public class Player
    {
        public ObjectId PlayerId { get; set; }

        public ulong User { get; set; }
        [BsonRef("Characters")]
        public Character Character { get; set; }
    }
}
