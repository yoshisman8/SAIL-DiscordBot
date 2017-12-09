using System;
using System.Collections.Generic;
using System.Text;
using Discord;
using System.Linq;
using LiteDB;


namespace ERA20.Modules.Classes
{
    class Storage
    {
        [BsonId]
        public int StorageId { get; set; }
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public string ImageURL { get; set; } = "https://cdn.discordapp.com/attachments/314912846037254144/389075880565276683/XXX_Stick_Box_main.png";
        public Inventory Inventory { get; set; } = new Inventory();
        public double Money { get; set; } = 0.00;

    }
}
