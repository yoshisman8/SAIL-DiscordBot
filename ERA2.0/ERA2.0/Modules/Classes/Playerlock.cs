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

        public Playerlock(LiteDatabase database)
        {
            Database = database;
        }

        public Player GetPlayer(ulong playerID)
        {
            var col = Database.GetCollection<Player>("Players");
            if (col.Exists(x => x.User == playerID))
            {
                var P = col
                    .Include(x => x.Character)
                    .FindOne(x => x.User == playerID);
                return P;
            }
            else
            {
                return null;
            }
        }
        public void Lock(ulong Player, Character character)
        {
            var col = Database.GetCollection<Player>("Players");
            var chars = Database.GetCollection<Character>("Characters");
            var Char = chars.FindOne(x => x.CharacterId == character.CharacterId);

            if (col.Exists(x => x.User == Player))
            {
                var P = col
                    .Include(x => x.Character)
                    .Include(x => x.Character.Equipment)
                    .Include(x => x.Character.Inventory.Items)
                    .FindOne(x => x.User == Player);
                P.Character = Char;
                col.Update(P);
            }
            else
            {
                var P = new Player
                {
                    Character = Char,
                    User = Player
                };
                col.Insert(P);
            }
        }
    }
    public class Player
    {
        [BsonId]
        public ulong User { get; set; }
        [BsonRef("Characters")]
        public Character Character { get; set; }
    }
}
