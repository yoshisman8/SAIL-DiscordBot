using System;
using System.Collections.Generic;
using System.Text;
using Discord;
using LiteDB;

namespace ERA20.Modules.Classes
{
    public class Item
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public Emoji Emote { get; set; } = new Emoji("💎");
        public string ImageUrl { get; set; } = "https://cdn.discordapp.com/attachments/314912846037254144/382001646403584020/img-thing.png";
    }
}
