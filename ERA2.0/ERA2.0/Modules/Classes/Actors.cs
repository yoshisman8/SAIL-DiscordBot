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
        public LiteDatabase Database { get; set; }

        public string Name { get; set; }
        public string ImageUrl { get; set; } = "https://cdn.discordapp.com/attachments/314912846037254144/373911023754805250/32438.png";
        public int MaxStress { get; set; } = 3;
        public int Stress { get; set; } = 0;
        public Dictionary<string, string> Afflictions { get; set; } = new Dictionary<string, string>() { };
    }

    public class Character : Actor
    {
        public ObjectId CharacterId { get; set; }
        public List<Gear> Equipment { get; set; } = new List<Gear>() { };
        public Trait ITrait { get; set; } = new Trait();
        public List<Trait> Traits { get; set; } = new List<Trait>() { };
        public List<Skill> Skills { get; set; } = new List<Skill>() { };
        [BsonRef("Items")]
        public List<Item> Inventory { get; set; } = new List<Item>() { };
        public double Money { get; set; } = 0;

        public void Update()
        {
            var col = Database.GetCollection<Character>("Characters");
            col.Update(this);
        }
        public void Add()
        {
            var col = Database.GetCollection<Character>("Characters");
            col.Insert(this);
        }
        public void Delete()
        {
            var col = Database.GetCollection<Character>("Characters");
            col.Delete(CharacterId);
        }
    }
    public class Trait
    {
        public string Name { get; set; } = "None";
        public string Description { get; set; } = "N/A";
    }
    public class Skill
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public int Level { get; set; } = 1;
        public Emoji Emote { get; set; } = new Emoji("⭐");

        public void LevelUp()
        {
            if (Level == 5)
            {
                throw new Exception("This skill is already mastered!");
            }
            Level++;
        }
    }
    public class Gear
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string ImageURL { get; set; } = "https://cdn.discordapp.com/attachments/314912846037254144/382001646403584020/img-thing.png";
        public Emoji Emote { get; set; } = new Emoji("👕");
    }

}
