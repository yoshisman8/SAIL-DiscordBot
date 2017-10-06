using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using Discord.Commands;
using Discord.WebSocket;
using Discord;
using Discord.Addons.Interactive;
using Newtonsoft.Json;
using LiteDB;

namespace ERABOT.Modules
{
    public class PlayerStorage : InteractiveBase<SocketCommandContext>
    {
        [Command("user")]
        [Alias("character")]
        public async Task Playershow(string name)
        {
            Directory.CreateDirectory(@"Data/Players/");

            var query = Directory.EnumerateFiles(@"Data/Players/", name+"*");

            if (query.Count() > 1)
            {
                string msg = "Multiple characters with similar names were found! Please specify which one is your character: ";
                foreach (string x in query)
                {
                    Actor result = JsonConvert.DeserializeObject<Actor>(x);
                    msg += "`" + result.Name + "` ";
                }
                await Context.Channel.SendMessageAsync(msg);
            }
            else
            {
                Actor plr = JsonConvert.DeserializeObject<Actor>(query.First());
                if (plr.Approved == false)
                {
                    await Context.Channel.SendMessageAsync("```diff\n- WARNING: THIS CHARACTER HAS NOT BEEN APPROVED BY A " +
                        "DM YET AND AS SUCH IS SUBJECT TO CHANGE.```");
                }

                await Context.Channel.SendMessageAsync("", embed: EmbedPlayer(plr));
            }


        }
        [Command("register", RunMode = RunMode.Async)]
        [Alias("newcharacter")]
        public async Task Playermake()
        {
            Directory.CreateDirectory(@"Data/Players/");

            Actor plr = new Actor();

            IDMChannel dm = await Context.User.GetOrCreateDMChannelAsync();

            await Context.Channel.SendMessageAsync(Context.User.Mention + ", An E.R.A. Recruitment unit has been dispatched to your direct messaging inbox. Please follow the steps to complete your registration. \n(You *must* have 'Receive Direct messages from anyone' turned on)");

            var msg = await dm.SendMessageAsync("```Initializing Player Creation Protocol...```");

            await Task.Delay(1000);

            await msg.ModifyAsync(g => g.Content = "```Initializing Player Creation Protocol...\n" +
            "Establishing Connection with Federation COMM array...```");

            await Task.Delay(1000);

            await msg.ModifyAsync(g => g.Content = "```Initializing Player Creation Protocol...\n" +
            "Establishing Connection with Federation COMM array...\nLoading welcome message...```");

            await Task.Delay(1000);

            await msg.ModifyAsync(g => g.Content = "```Initializing Player Creation Protocol...\n" +
            "Establishing Connection with Federation COMM array...\nLoading welcome message...\nDownloading Terms and Conditions...```");

            await Task.Delay(1000);

            await msg.ModifyAsync(g => g.Content = "```Initializing Player Creation Protocol...\n" +
            "Establishing Connection with Federation COMM array...\nLoading welcome message...\nDownloading Terms and Conditions..." +
            "\nLoading Complete.```");

            await Task.Delay(1000);

            await msg.DeleteAsync();

            await dm.SendMessageAsync("```fix\nWelcome to the E.R.A. Program!\nPlease input the name of this user: ```");

            var x = await NextMessageAsync(true, false,timeout: TimeSpan.FromMinutes(5));

            plr.Name = x.Content;

            var files = Directory.EnumerateFiles(@"Data/Players/"); 

            if (files.Contains("*"+plr.Name+"*") == true) { await dm.SendMessageAsync("`This User already exists in the database! If you're the owner of this character and you'd like to have it deleted/redone, ask your DM to delete it for you!"); return; }

            await dm.SendMessageAsync("```fix\nWelcome User " + plr.Name + "!\nFor logging purpuses, please state your race." +
                "\nDo not worry, here at E.R.A we will not discriminate you too much for being an alien race.```");

            x = await NextMessageAsync(true, false, timeout: TimeSpan.FromMinutes(5));

            plr.Race = x.Content;

            if (plr.Race.ToLower() == "human") await dm.SendMessageAsync("```fix\nWelcome to the E.R.A. Program, fellow human! Together we shall recover earth from the machines!```");

            else await dm.SendMessageAsync("```fix\nWelcome To the E.R.A. Program, Alien. We are grateful for your colaboration with our program.```");

            await Task.Delay(TimeSpan.FromSeconds(3));

            await dm.SendMessageAsync("```fix\nPlease State your profession so that we may group you with a team needing of your expertise:```");

            x = await NextMessageAsync(true, false, timeout: TimeSpan.FromMinutes(5));

            plr.Class = x.Content;

            await dm.SendMessageAsync("```fix\nIf, you have one. Please provide an URL to picture to store for your profie. " +
                "This will help others recognize you a bit faster. \nIf you don't have one, just type 'None```");

            x = await NextMessageAsync(true, false, timeout: TimeSpan.FromMinutes(5));

            if (x.Content.ToLower() == "none") { plr.ImgURL = ""; }

            else { plr.ImgURL = x.Content; }

            bool y = false;

            while (y == false)
            {
                msg = await dm.SendMessageAsync("`Please send your 6 stats (STR, AGI, ACC, CON, TEC & SYN) separated by a coma. " +
                    "Remember your stats must add up to 18.`");

                x = await NextMessageAsync(true, false, timeout: TimeSpan.FromMinutes(5));

                if (x == null) { await msg.DeleteAsync(); continue; }

                int[] stats = x.Content.Split(',').Select(str => int.Parse(str)).ToArray();

                if (stats.Sum() != 18) { await msg.DeleteAsync(); continue; }

                plr.Stats.STR = stats[0];
                plr.Stats.AGI = stats[1];
                plr.Stats.ACC = stats[2];
                plr.Stats.CON = stats[3];
                plr.Stats.TEC = stats[4];
                plr.Stats.SYN = stats[5];
                plr.Stats.MaxHP = 10 + ModifierGet(plr.Stats.CON);
                plr.Stats.CurrHP = plr.Stats.MaxHP;

                bool y2 = false;

                while (y2 == false)
                {
                    var msg2 = await dm.SendMessageAsync("This is your sheet: Is this ok? (Y/N)/n", embed: EmbedPlayer(plr));

                    x = await NextMessageAsync(true, false, timeout: TimeSpan.FromMinutes(5));

                    if (x.Content.ToLower() == "y")
                    {
                        plr.Owner = Convert.ToUInt64(Context.User.Id.ToString());

                        plr.Approved = false;

                        Random rnd = new Random();

                        plr.Inventory.Money = rnd.Next(1, 100);
                        
                        await dm.SendMessageAsync("`Welcome to the E.R.A. Program, " + plr.Name + "!`");

                        y = true;

                        y2 = true;
                    }
                    else if (x.Content.ToLower() == "n")
                    {
                        await msg2.DeleteAsync();
                        y2 = true;
                    }
                    else
                    {
                        await msg2.DeleteAsync();
                        continue;
                    }
                }
            }
            string json = JsonConvert.SerializeObject(plr);

            File.WriteAllText("Data/Players/"+plr.Name+".json",json);

            await Context.Channel.SendMessageAsync("`Please welcome new agent "
                + plr.Name + " to the E.R.A. program! Together we shall take back earth!`");
        }
        [Command("Approve")]
        public async Task Aprove(string name)
        {
            var User = Context.User as SocketGuildUser;
            var role = User.Roles.Where(x => x.Id == 356134878661574666);
            if (User != null)
            {

                var query = Directory.EnumerateFiles(@"Data/Players/", name + "*");

                if (query.Count() > 1)
                {
                    string msg = "Multiple characters with similar names were found! Please specify which one is your character: ";
                    foreach (string x in query)
                    {
                        Actor result = JsonConvert.DeserializeObject<Actor>(x);
                        msg += "`" + result.Name + "` ";
                    }
                    await Context.Channel.SendMessageAsync(msg);
                }
                else
                {
                    Actor plr = JsonConvert.DeserializeObject<Actor>(query.First());

                    plr.Approved = true;

                    string json = JsonConvert.SerializeObject(plr);

                    File.WriteAllText("Data/Players/" + plr.Name + ".json", json);

                    await Context.Channel.SendMessageAsync("`Character Approved!`");
                }
            }
            else
            {
                await Context.Channel.SendMessageAsync("`Only Dungeon Masters can approve characters!");
            }}
        [Command("Delete", RunMode = RunMode.Async)]
        public async Task Delet(string name)
        {
            var User = Context.User as SocketGuildUser;
            var role = User.Roles.Where(x => x.Id == 356134878661574666);
            if (User != null)
            {

                var query = Directory.EnumerateFiles(@"Data/Players/", name + "*");

                if (query.Count() > 1)
                {
                    string msg = "Multiple characters with similar names were found! Please specify which one is your character: ";
                    foreach (string y in query)
                    {
                        Actor result = JsonConvert.DeserializeObject<Actor>(y);
                        msg += "`" + result.Name + "` ";
                    }
                    await Context.Channel.SendMessageAsync(msg);
                }
                else
                {
                    Actor plr = JsonConvert.DeserializeObject<Actor>(query.First());

                    await Context.Channel.SendMessageAsync("`Are you sure you want to delete " + plr.Name + "? (Y/N)`");

                    var x = await NextMessageAsync(true, false, timeout: TimeSpan.FromMinutes(5));

                    if (x.Content.ToLower() == "y")
                    {
                        File.Delete("Data/Players/"+plr.Name+".json");
                        await Context.Channel.SendMessageAsync("`Entry Deleted.`");
                    }
                    else
                    {
                        await Context.Channel.SendMessageAsync("`Operation Cancelled!`");
                    }
                }
            }
            else
            {
                await Context.Channel.SendMessageAsync("`Only Dungeon Masters can remove characters!");
            }
        }
        public int ModifierGet(int stat) => (int)Math.Floor((double)(((decimal)stat-10) / 2));
        public string Statroll()
        {
            List<int> returnvalues = new List<int>(6);
            foreach (int x in returnvalues)
            {
                List<int> values = new List<int>();
                Random random = new Random();
                for (int count = 0; count < 3; count++)
                {
                    values[count] = random.Next(1, 6);
                }
                values.Sort();
                values.RemoveAt(0);
                foreach (int y in values)
                {
                    returnvalues[x] += y;
                }
            }
            string stats = string.Join(", ", returnvalues);
            return stats;
        }
        public Embed EmbedPlayer(Actor plr)
        {
            IUser user = Context.Guild.GetUser(plr.Owner);
            var builder = new EmbedBuilder()
        .WithTitle(plr.Name)
        .WithDescription(plr.Race + " the " + plr.Class)
        .WithColor(new Color(0xffffff))
        .WithFooter(footer => {
            footer
                .WithText("Created by" + user.ToString())
                .WithIconUrl(user.GetAvatarUrl());
        }) 
	    .WithImageUrl(plr.ImgURL)
        .WithAuthor(author => {
            author
                .WithName("E.R.A. Agent Profile")
                .WithIconUrl(Context.Client.CurrentUser.GetAvatarUrl());
        })
        .AddField("Vitals", "["+plr.Stats.CurrHP+"/"+plr.Stats.MaxHP+"] HP | ["+ListAliments(plr.Aliments)+"]")
        .AddField("Weapon", plr.Weapon.Name+" ["+plr.Weapon.MinDamage+"-"+plr.Weapon.MaxDamage+" Damage.]")
        .AddField("Armor", plr.Armor.Name+" ["+plr.Armor.MaxDefense+" Armor]")
        .AddInlineField("Strength", plr.Stats.STR+" ["+ModifierGet(plr.Stats.STR)+"] MOD" )
        .AddInlineField("Agility", plr.Stats.AGI + " [" + ModifierGet(plr.Stats.AGI) + "] MOD")
        .AddInlineField("Accuracy", plr.Stats.ACC + " [" + ModifierGet(plr.Stats.AGI) + "] MOD")
        .AddInlineField("Constitution", plr.Stats.CON + " [" + ModifierGet(plr.Stats.CON) + "] MOD")
        .AddInlineField("Tech", plr.Stats.TEC + " [" + ModifierGet(plr.Stats.TEC) + "] MOD")
        .AddInlineField("Synergy", plr.Stats.SYN + " [" + ModifierGet(plr.Stats.SYN) + "] MOD");
            var embed = builder.Build();
            return embed;
        }
        public string ListAliments(List<string> aliments)
        {
            string parsed = "| ";
            foreach (string x in aliments)
            {
                parsed += "["+x+"] ";
            }
            return parsed;
        }
    }
}
public class Actor
{
    public int Id { get; set; }
    public ulong Owner { get; set; }
    public string Name { get; set; }
    public string Race { get; set; }
    public string Class { get; set; }
    public Stats Stats { get; set; }
    public Inventory Inventory { get; set; }
    public Weapon Weapon { get; set; }
    public Armor Armor { get; set; }
    public List<string> Aliments { get; set; }
    public bool Approved { get; set; }
    public string ImgURL { get; set; }
    public string DMnotes { get; set; }
}
public class Stats
{
    public int MaxHP { get; set; }
    public int CurrHP { get; set; }
    public int STR { get; set; }
    public int AGI { get; set; }
    public int ACC { get; set; }
    public int CON { get; set; }
    public int TEC { get; set; }
    public int SYN { get; set; }
}
public class Inventory
{
    public int Money { get; set; }
    public List<Item> Items { get; set; }
}
public class Item
{
    public string Name { get; set; }
    public int Quantity { get; set; }
    public string Description { get; set; }
    public string DescriptionDM { get; set; }
    public string Effect { get; set; }
    public int Value { get; set; }
}
public class Weapon
{
    public string Name { get; set; }
    public string Description { get; set; }
    public string DescriptionDM { get; set; }
    public int MaxDamage { get; set; }
    public int MinDamage { get; set; }
}
public class Armor
{
    public string Name { get; set; }
    public string Description { get; set; }
    public string DescriptionDM { get; set; }
    public int MaxDefense { get; set; }
    public int CurrDefense { get; set; }
}
public class Skill
{
    public string Name { get; set; }
    public string Description { get; set; }
    public int AP { get; set; }
    public int Cooldown { get; set; }
    public int CurrCooldown { get; set; }
    public bool IsReady { get; set; }

}
