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
