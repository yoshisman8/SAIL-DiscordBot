using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using Discord.Commands;
using Discord.WebSocket;
using Discord.Addons.Interactive;
using Discord;
using Newtonsoft.Json;

namespace ERA.Modules
{
    public class PlayerStorage: InteractiveBase<SocketCommandContext>
    {
        //[Command("Player")]
        //[RequireContext(ContextType.DM)]
        //[Summary("Loads the player sheet editor menu. Usage: `$Player`. **Can only be used by DMing the bot directly.**")]
        //public async Task Register(string _Name)
        //{
        //    Directory.CreateDirectory(@"Data/Players/");
        //}
        //[Command("Player")]
        //[RequireContext(ContextType.DM)]
        //[Summary("Displays a player sheet. Usage: `$Player <name>`.")]
        //public async Task LoadPlayer(string _Name)
        //{

        //}

        public Embed BuildSheet(Player player)
        {
            var user = Context.Guild.GetUser(player.Owner);
            var builder = new EmbedBuilder()
                .WithColor(new Color(0xE90FCD))
            .WithFooter(footer => {
                footer
                    .WithText(user.Nickname)
                    .WithIconUrl(user.GetAvatarUrl());
                })
	        .WithThumbnailUrl(player.ImagURL)
            .WithAuthor(author => {
                author
                    .WithName(player.Name)
                    .WithUrl(player.ImagURL);
            })
            .AddInlineField(":bar_chart: Vitals", ":wrench: "+ player.Class+"\n:heart: "+player.CurrHP+"/"+player.MaxHP+" HP\n:shield: "+player.Armor+" Armor")
            .AddInlineField(":tools: Gear", Buildlist(player, 1))
            .AddInlineField(":star2: Traits", Buildlist(player, 3))
            .AddInlineField(":star: Skills", Buildlist(player,2))
            .AddField(":school_satchel: Inventory", Buildlist(player,4));
            var embed = builder.Build();
            return embed;
        }
        private string Buildlist(Player player, int arg)
        {
            string msg = "";
            switch (arg)
            {
                case 1:
                    foreach (Gear x in player.Gear)
                    {
                        msg += x.Icon + x.Name + "\n";
                    }
                    break;
                case 2:
                    foreach (Skill x in player.Skills)
                    {
                        msg += x.Icon + x.Name + " ["+ToRoman(x.Level)+"]\n";
                    }
                    break;
                case 3:
                    foreach (Trait x in player.Traits)
                    {
                        msg += x.Icon + x.Name+"\n";
                    }
                    break;
                case 4:
                    msg += ":moneybag: " + player.Money+"\n";
                    foreach (Item x in player.Inventory)
                    {
                        msg += x.Icon + x.Name + "\n";
                    }
                    break;
            }
            return msg;
        }
        public static string ToRoman(int number)
        {
            if ((number < 0) || (number > 3999)) throw new ArgumentOutOfRangeException("insert value betwheen 1 and 3999");
            if (number < 1) return string.Empty;
            if (number >= 1000) return "M" + ToRoman(number - 1000);
            if (number >= 900) return "CM" + ToRoman(number - 900); //EDIT: i've typed 400 instead 900
            if (number >= 500) return "D" + ToRoman(number - 500);
            if (number >= 400) return "CD" + ToRoman(number - 400);
            if (number >= 100) return "C" + ToRoman(number - 100);
            if (number >= 90) return "XC" + ToRoman(number - 90);
            if (number >= 50) return "L" + ToRoman(number - 50);
            if (number >= 40) return "XL" + ToRoman(number - 40);
            if (number >= 10) return "X" + ToRoman(number - 10);
            if (number >= 9) return "IX" + ToRoman(number - 9);
            if (number >= 5) return "V" + ToRoman(number - 5);
            if (number >= 4) return "IV" + ToRoman(number - 4);
            if (number >= 1) return "I" + ToRoman(number - 1);
            throw new ArgumentOutOfRangeException("something bad happened");
        }
    }
    
    public class Player
    {
        public string Name { get; set; }
        public ulong Owner { get; set; }
        public string Race { get; set; }
        public string Class { get; set; }
        public int Armor { get; set; }
        public int MaxHP { get; set; }
        public int CurrHP { get; set; }
        public string ImagURL { get; set; }
        public int Money { get; set; }
        public List<Gear> Gear { get; set; } = new List<Gear> { };
        public List<Skill> Skills { get; set; } = new List<Skill> { };
        public List<Trait> Traits { get; set; } = new List<Trait> { };
        public List<Item> Inventory { get; set; } = new List<Item> { };

        public IEnumerable<Player> GetPlayer(string _query)
        {
            Directory.CreateDirectory(@"Data/Players/");
            var folder = Directory.EnumerateFiles(@"Data/Players/");
            IList<Player> players = new List<Player> { };
            foreach (string X in folder)
            {
                players.Add(JsonConvert.DeserializeObject<Player>(File.ReadAllText(X)));
            }
            var query = players.Where(x => x.Name.ToLower().Contains(_query.ToLower()));
            var query2 = query.OrderByDescending(x => x.Name);
            return query2;
        }
    }
    public class Gear
    {
        public string Name { get; set; }
        public string Icon { get; set; }
        public string Description { get; set; }
    }
    public class Skill
    {
        public string Name { get; set; }
        public int Level { get; set; }
        public string Icon { get; set; }
        public string Description { get; set; }
    }
    public class Trait
    {
        public string Name { get; set; }
        public string Icon { get; set; }
        public string Description { get; set; }
    }
    public class Item
    {
        public string Name { get; set; }
        public string Icon { get; set; }
        public string Description { get; set; }
    }
}
