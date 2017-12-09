using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using LiteDB;

namespace ERA20.Modules.Classes
{
    class Battle
    {
        [BsonIgnore]
        public Random Random { get; set; }
        [BsonIgnore]
        public LiteDatabase Database { get; set; }
        [BsonId]
        public int BattleId { get; set; }
        public string Name { get; set; }
        public List<BattleActor> Actors { get; set; } = new List<BattleActor>() { };

        public void PassInstance(LiteDatabase _Database)
        {
            Database = _Database;
        }

        public void GenerateTurns()
        {
            foreach (BattleActor x in Actors)
            {
                x.TurnOrder = Random.Next(1, 20);
            }
            Actors = Actors.OrderBy(x => x.TurnOrder).ToList();
            var col = Database.GetCollection<Battle>("Battles");
            col.Update(this);
        }
        public Actor Next()
        {
            List<BattleActor> Old = Actors.ToList();
            Old.Add(Old.First());
            Old.RemoveAt(0);
            Actors = Old;
            var col = Database.GetCollection<Battle>("Battles");
            col.Update(this);
            return Actors.Last();
        }
        public void Add(Actor actor)
        {
            Actors.Add(actor as BattleActor);
        }
        public void Discard()
        {
            var col = Database.GetCollection<Battle>("Battles");
            col.Delete(BattleId);
        }
    }
    class BattleActor : Actor
    {
        public int TurnOrder { get; set; }
    }
}