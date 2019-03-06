using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Discord;
using Discord.Commands;
using LiteDB;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json;

namespace SAIL.Classes.Legacy
{
    public class LegacyCharacter
    {
        public string Owner { get; set; }
        public string Name { get; set; }
        public string Sheet { get; set; }

        public IOrderedEnumerable<LegacyCharacter> Query(string _Query)
        {
            Directory.CreateDirectory(@"Data/Legacy/");
            var files = Directory.EnumerateFiles(Directory.GetCurrentDirectory()+@"Data/Legacy/");
            List<LegacyCharacter> db = new List<LegacyCharacter> { };
            foreach (string x in files)
            {
                db.Add(JsonConvert.DeserializeObject<LegacyCharacter>(File.ReadAllText(x)));
            }
            var query = db.Where(x => x.Name.ToLower().StartsWith(_Query.ToLower())).OrderBy(x => x.Name);
            return query;
        }
        public List<LegacyCharacter> GetAll()
        {
            Directory.CreateDirectory(@"Data/Legacy/");
            var files = Directory.EnumerateFiles(Directory.GetCurrentDirectory()+@"Data/Legacy/");
            List<LegacyCharacter> db = new List<LegacyCharacter> { };
            foreach (string x in files)
            {
                db.Add(JsonConvert.DeserializeObject<LegacyCharacter>(File.ReadAllText(x)));
            }
            return db;
        }
    }
    public class Actor 
    {
        [BsonIgnore]
        public static LiteDatabase Database { get; set; }

        public string Name { get; set; }
        public string ImageUrl { get; set; } = "https://cdn.discordapp.com/attachments/314912846037254144/373911023754805250/32438.png";
        public int MaxStress { get; set; } = 5;
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
    public class Inventory
    {
        [BsonIgnore]
        public LiteDatabase Database { get; set; }

        public List<Item> Items { get; set; } = new List<Item>() { };


        public void PassInstance(LiteDatabase _Database)
        {
            Database = _Database;
        }
        public void buildInv(LiteDatabase database)
        {
            Database = database;
            var col = Database.GetCollection<BaseItem>("Items");
            foreach (Item x in Items)
            {
                x.BaseItem = col.FindById(x.BaseItem.ItemId);
            }
            var I = Items.ToList();
            foreach (Item x in Items)
            {
                if (x.BaseItem == null)
                {
                    I.Remove(x);
                }
            }
            Items = I;
        }


        public void Add(Item Item, int quantity)
        {
            if (Items.Exists(x => x.BaseItem.ItemId == Item.BaseItem.ItemId))
            {
                var i = Items.FindIndex(x => x.BaseItem.ItemId == Item.BaseItem.ItemId);
                Items[i].Quantity += quantity;
                return;
            }
            else
            {
                Item.Quantity = quantity;
                Items.Add(Item);
                return;
            }
        }
        public Item Use(Item Item)
        {
            var I = Items.FindIndex(x => x.BaseItem.ItemId == Item.BaseItem.ItemId);
            return Items.ElementAt(I);
        }
        public Item Consume(BaseItem Item, int Ammount)
        {
            if (Items.Exists(x => x.BaseItem.ItemId == Item.ItemId))
            {
                var I = Items.FindIndex(x => x.BaseItem.ItemId == Item.ItemId);
                Items[I].Quantity -= Ammount;
                var r = Items.ElementAt(I);
                if (Items.ElementAt(I).Quantity <= 0)
                {
                    Items.RemoveAt(I);
                }
                return r;
            }
            else
            {
                return null;
            }
        }
        //public Item AsItem(BaseItem Base)
        //{
        //    Item item = new Item()
        //    {
        //        Name = Base.Name,
        //        Description = Base.Description,
        //        ImageUrl = Base.ImageUrl,
        //        ItemId = Base.ItemId
        //    };
        //    return item;
        //}
    }
    public class BaseItem
    {
        [BsonId]
        public int ItemId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string ImageUrl { get; set; } = "https://cdn.discordapp.com/attachments/314912846037254144/382001646403584020/img-thing.png";

    }
    public class Item 
    {
        [BsonRef("Items")]
        public BaseItem BaseItem { get; set; }
        public int Quantity { get; set; } = 1;
    }
    public class ItemEx
    {
        public int Item { get; set; }
        public int Quantity { get; set; } = 1;

        [BsonIgnore]
        public BaseItem BaseItem { get; set; }

    }
}