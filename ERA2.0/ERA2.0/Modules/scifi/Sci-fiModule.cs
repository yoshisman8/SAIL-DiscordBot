using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using Discord.Commands;
using Discord.WebSocket;
using Discord;
using Newtonsoft.Json;

namespace ERA.Modules.Sci_fi
{
    public class PlayerStorage: ModuleBase<SocketCommandContext>
    {
       [Command("enroll")]
       public async Task Register(string _Name = "", string _Race = "", string _Class = "")
        {
            Directory.CreateDirectory(@"Data/Sci-fi/Players");
            if (_Name == "" || _Race == "" || _Class == "") { await Context.Channel.SendMessageAsync("Incorrect command ussage!\n Correct ussage: `$Register <Name> <race> <class>`"); }
            else if (File.Exists(@"Data/Sci-fi/Players"+_Name+".json") == true){ await Context.Channel.SendMessageAsync("This character already exists!"); }
            else
            {
                var Player = new Player()
                {
                    Name = _Name,
                    Race = _Race,
                    Class = _Class,
                    MaxHP = 10,
                    CurrHP = 10,
                    ImagURL = "",
                    Owner = Context.User.Id
                };
                string json = JsonConvert.SerializeObject(Player);
                File.WriteAllText(@"Data/Sci-fi/Players" + _Name + ".json", json);
                await Context.Channel.SendMessageAsync("Character " + _Name + " Added to the Database successfully!" +
                    "\n Use `$Addskill <character> <name>` to add a skill (remember you only get 3 at the beggining of your campaign.)" +
                    "\n You can also use $Image <Image URL> to chage the icon image of your sheet.");
            }
        }
        [Command("addskill")]
        public async Task Addskill(string _Skill = "", string _Player = "")
        {
            var player = new Player();
            if (_Player == "")
            {
                var query = new Query();
                var files = Directory.EnumerateFiles(@"Data/Sci-fi/Players");
                foreach (string x in files)
                {

                    query.Resuts.Add(JsonConvert.DeserializeObject<Player>(x));
                }
                var final = query.Resuts.Where(x => x.Owner == Context.User.Id);
                if (final == null) { await Context.Channel.SendMessageAsync("You dont have a character!"); return; }
                else
                {
                    player = final.First();
                }
            }
            if (_Skill == "")
            {
                await Context.Channel.SendMessageAsync("Incorrect commad ussage! Correct ussage is `$Addskill <Skill name>");
            }
            else
            {
                if (player.Skills.Where(x => x.Name == _Skill).Count() >= 1)
                {
                    await Context.Channel.SendMessageAsync("You already have Skill " + _Skill + "!");
                }
                else
                {
                    player.Skills.Add(new Skill()
                    {
                        Name = _Skill,
                        Level = 1
                    });
                    await Context.Channel.SendMessageAsync("Skill " + _Skill + " Added to your character's list successfully!");
                }
            }
        }
    }
    public class Query
    {
        public List<Player> Resuts { get; set; } = new List<Player> { };
    }
    public class Player
    {
        public string Name { get; set; }
        public ulong Owner { get; set; }
        public string Race { get; set; }
        public string Class { get; set; }
        public int MaxHP { get; set; }
        public int CurrHP { get; set; }
        public string ImagURL { get; set; }
        public List<Skill> Skills { get; set; } = new List<Skill> { };
        public List<Item> Inventory { get; set; } = new List<Item> { };
    }
    public class Skill
    {
        public string Name { get; set; }
        public int Level { get; set; }
    }
    public class Item
    {
        public string Name { get; set; }
        public string Description { get; set; }
    }
}
