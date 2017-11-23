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
        public List<Affliction> Afflictions = new List<Affliction>() { };

        public void Afflict(string Affliction, string Description, Emoji emoji = null)
        {
            var A = new Affliction
            {
                Name = Affliction,
                Description = Description,
            };
            if (emoji == null)
            {
                Afflictions.Add(A);
                return;
            }
            else
            {
                A.Emoji = emoji;
                Afflictions.Add(A);
                return;
            }
        }
        public void RemAffliction(string affliction)
        {
            if (Afflictions.Exists(x => x.Name.ToLower() == Name.ToLower()))
            {
                Afflictions.RemoveAll(x => x.Name.ToLower() == Name.ToLower());
            }
            else
            {
                throw new Exception("This character is not afflicted with this!");
            }
        }
        public Actor(LiteDatabase _database)
        {
            Database = _database;
        }
    }

    public class Character : Actor
    {
        public Character(LiteDatabase _database) : base(_database)
        {
            Database = _database;
        }

        public ObjectId CharacterId { get; set; }
        public string Class { get; set; }
        public string Race { get; set; }
        public string Description { get; set; } = "";
        [BsonRef("Items")]
        public List<Item> Equipment { get; set; } = new List<Item>() { };
        public Trait ITrait { get; set; } = new Trait();
        public List<Trait> Traits { get; set; } = new List<Trait>() { };
        public List<Skill> Skills { get; set; } = new List<Skill>() { };
        public Inventory Inventory { get; set; } = new Inventory(Database);
        public double Money { get; set; } = 0;
        public ulong Owner { get; set; }

        public IEnumerable<Character> GetCharacter(string Name)
        {
            var col = Database.GetCollection<Character>("Characters");
            var A = col.Find(x => x.Name.ToLower().StartsWith(Name.ToLower()));
            return A;
        }

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
        public void Equip(Item item)
        {

            var I = Inventory.Items.FindIndex(x => x.ItemId == item.ItemId);
            Equipment.Add(Inventory.Items.ElementAt(I));
            Inventory.Items.RemoveAt(I);
            var col = Database.GetCollection<Character>("Characters");
            col.Update(this);
        }
        public void DeEquip(Item item)
        {
            var I = Equipment.FindIndex(x => x.ItemId == item.ItemId);
            Inventory.Add(Equipment.ElementAt(I));
            Equipment.RemoveAt(I);
            var col = Database.GetCollection<Character>("Characters");
            col.Update(this);
        }
        public void Pay(int Amount, Character Target, bool Override = false)
        {
            
            if (Money < Math.Abs(Amount))
            {
                throw new Exception("You don't have this much money!");
            }
            else if (Override == true)
            {
                Money -= Math.Abs(Amount);
                return;
            } 
            else
            {
                Money -= Math.Abs(Amount);
                Target.Money += Math.Abs(Amount);
            }
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
    public class Affliction
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public Emoji Emoji { get; set; } = new Emoji("⚠");
    }
}
