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
    public class Actor 
    {
        [BsonIgnore]
        public static LiteDatabase Database { get; set; }

        public string Name { get; set; }
        public string ImageUrl { get; set; } = "https://cdn.discordapp.com/attachments/314912846037254144/373911023754805250/32438.png";
        public int MaxStress { get; set; } = 3;
        public int Stress { get; set; } = 0;
        public List<Affliction> Afflictions { get; set; } = new List<Affliction> { };

        public void PassInstance(LiteDatabase _database)
        {
            Database = _database;
        }
    }

    public class Character : Actor
    {

        [BsonId]
        public int CharacterId { get; set; }
        public string Class { get; set; }
        public string Race { get; set; }
        public string Description { get; set; } = "";
        [BsonRef("Items")]
        public List<BaseItem> Equipment { get; set; } = new List<BaseItem>() { };
        public Trait ITrait { get; set; } = new Trait();
        public List<Trait> Traits { get; set; } = new List<Trait>() { };
        public List<Skill> Skills { get; set; } = new List<Skill>() { };
        public Inventory Inventory { get; set; } = new Inventory();
        public double Money { get; set; } = 0;
        public ulong Owner { get; set; }


        public void Update()
        {
            var col = Database.GetCollection<Character>("Characters");
            col.Update(this);
        }
        public void Add()
        {
            var col = Database.GetCollection<Character>("Characters");
            col.Insert(this);
            col.EnsureIndex(x => x.Name);
        }
        public void Delete()
        {
            var col = Database.GetCollection<Character>("Characters");
            col.Delete(CharacterId);
        }
        public void Equip(BaseItem Item)
        {
            Equipment.Add(Item);
            Inventory.Consume(Item, 1);
            var col = Database.GetCollection<Character>("Characters");
            col.Update(this);
        }
        public void DeEquip(BaseItem item)
        {
            Inventory.Add(new Item()
            {
                BaseItem = item
            }, 1);
            Equipment.Remove(item);
            var col = Database.GetCollection<Character>("Characters");
            col.Update(this);
        }
        public void NullBGone(LiteDatabase database)
        {
            Database = database;
            var col = Database.GetCollection<BaseItem>("Items");
            var Equips = new List<BaseItem>() { };
            foreach (BaseItem x in this.Equipment)
            {
                if (col.Exists(y => y.ItemId == x.ItemId))
                {
                    Equips.Add(col.FindOne(y => y.ItemId == x.ItemId));
                }
            }
            var Items = new Inventory();
            foreach (Item x in Inventory.Items)
            {
                if (col.Exists(y => y.ItemId == x.BaseItem.ItemId))
                {
                    Items.Add(new Item() { BaseItem = col.FindOne(y => y.ItemId == x.BaseItem.ItemId) }, x.Quantity);
                }
            }
            Equipment = Equips;
            Update();
        }

        public IEnumerable<Character> GetCharacter(string name)
        {
            var col = Database.GetCollection<Character>("Characters");
            col.EnsureIndex("Name", "LOWER($.Name)");
            var C = col.Find(Query.StartsWith("Name",name.ToLower()));
            return C;
        }
    }
    public class Trait
    {
        public string Name { get; set; } = "None";
        public string Description { get; set; } = "N/A";
    }
    public class Skill
    {
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public int Level { get; set; } = 1;
    }
    public class Affliction
    {
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
    }
}
