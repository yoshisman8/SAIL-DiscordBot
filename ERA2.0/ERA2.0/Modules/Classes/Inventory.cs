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

        public void PassInstance(LiteDatabase _Database)
        {
            Database = _Database;
        }
        public void Add(Item Item)
        {
            if (Items.Exists(x => x.BaseItem.ItemId == Item.BaseItem.ItemId))
            {
                var i = Items.FindIndex(x => x.BaseItem.ItemId == Item.BaseItem.ItemId);
                Items.ElementAt(i).Quantity += Item.Quantity;
            }
            else
            {
                Items.Add(Item);
            }
        }
        public Item Use(Item Item)
        {
            var I = Items.FindIndex(x => x.BaseItem.ItemId == Item.BaseItem.ItemId);
            return Items.ElementAt(I);
        }
        public Item Consume(Item Item, int Ammount)
        {
            if (Items.Exists(x => x.BaseItem.ItemId == Item.BaseItem.ItemId))
            {
                var I = Items.FindIndex(x => x.BaseItem.ItemId == Item.BaseItem.ItemId);
                Items.ElementAt(I).Quantity -= Ammount;
                var r = Items.ElementAt(I);
                if (Items.ElementAt(I).Quantity == 0)
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
}
