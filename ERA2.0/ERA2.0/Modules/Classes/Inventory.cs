using System;
using System.Collections.Generic;
using System.Text;
using Discord;
using System.Linq;
using LiteDB;

namespace ERA20.Modules.Classes
{
    public class Inventory
    {
        [BsonIgnore]
        public LiteDatabase Database { get; set; }

        public List<Item> Items { get; set; } = new List<Item>() { };

        public IEnumerable<Item> GetItem(string Name)
        {
            var col = Database.GetCollection<Item>("Items");
            var I = col.Find(x => x.Name.ToLower().StartsWith(Name.ToLower()));
            if (I.Count() == 0) { throw new Exception("No items with this name could be found on the Database!"); }
            else
            {
                return I;
            }
        }

        public void Add(Item Item)
        {
            if (Items.Exists(x => x.ItemId == Item.ItemId))
            {
                var i = Items.FindIndex(x => x.ItemId == Item.ItemId);
                Items.ElementAt(i).Quantity += Item.Quantity;
            }
            else
            {
                Items.Add(Item);
            }
        }
        public Item Use(string Item)
        {
            var I = Items.FindIndex(x => x.Name.ToLower().StartsWith(Item.ToLower()));
            return Items.ElementAt(I);
        }
        public Item Consume(string Item)
        {
            if (Items.Exists(x => x.Name.ToLower().StartsWith(Item.ToLower())))
            {
                var I = Items.FindIndex(x => x.Name.ToLower().StartsWith(Item.ToLower()));
                Items.ElementAt(I).Quantity--;
                var r = Items.ElementAt(I);
                if (Items.ElementAt(I).Quantity == 0)
                {
                    Item.Remove(I);
                }
                return r;
            }
            else
            {
                throw new Exception("This item could not be found!");
            }
        }
        public void Give(Item item, Character character, bool Override = false)
        {
            var chars = Database.GetCollection<Character>("Characters");
            var items = Database.GetCollection<Item>("Items");
            var T = chars.FindOne(x => x.Name == character.Name);
            if (Override == true)
            {
                T.Inventory.Add(item);
            }
            else
            {
                var I = Items.FindIndex(x => x.Name == item.Name);
                T.Inventory.Add(Items.ElementAt(I));
                Items.RemoveAt(I);
            }
        }
        public void Take(string Item)
        {
            var I = Items.FindIndex(x => x.Name.ToLower().StartsWith(Item.ToLower()));
            Item.Remove(I);
        }
    }
    public class BaseItem
    {
        [BsonIgnore]
        public LiteDatabase Database { get; set; }

        public ObjectId ItemId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public Emoji Emote { get; set; } = new Emoji("💎");
        public string ImageUrl { get; set; } = "https://cdn.discordapp.com/attachments/314912846037254144/382001646403584020/img-thing.png";

        public void Save()
        {
            var col = Database.GetCollection<BaseItem>("Items");
            col.Insert(this);
        }
        public void Delete()
        {
            var col = Database.GetCollection<BaseItem>("Items");
            col.Delete(ItemId);
        }
    }
    public class Item : BaseItem
    {
        public int Quantity { get; set; } = 1;
    }
}
