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
using Discord.Rest;

namespace ERA.Modules
{
    [Name("Second Edtion")]
    [Summary("")]
    public class PlayerStorage: InteractiveBase<SocketCommandContext>
    {
        [Command("Editor")]
        [RequireContext(ContextType.Guild)]
        public async Task wrongplace()
        {
            await ReplyAsync("Because of the way this command works, it can only be used through DMs, sorry. Please DM me to use this command!");
        }
        [Command("Editor")]
        [RequireContext(ContextType.DM)]
        [Summary("Loads the player sheet editor menu. Usage: `$Editor`. **Can only be used by DMing the bot directly.**")]
        public async Task Register()
        {
            Directory.CreateDirectory(@"Data/Players/");
            var player = new Player()
            {
                Name = "name",
                Class = "Class",
                MaxHP = 10,
                CurrHP = 10,
                Owner = Context.User.Id,
                Race = "Race",
                Money = 0,
                Armor = 0,
                ImagURL = "https://cdn.discordapp.com/attachments/314912846037254144/373911023754805250/32438.png"
            };
            bool MMenu = true;
            var options = new Dictionary<string, string>() { };
            var Sheet = await Context.Channel.SendMessageAsync("```Loading Main menu...```");
            var menu = await Context.Channel.SendMessageAsync("`Please wait...`");
            while (MMenu == true)
            {

                options.Clear();
                options.Add("Create", "Creates a new character tied to your name.");
                options.Add("Load", "Loads a character you've created.");
                options.Add("Delete", "Deletes a character you've created.");
                await menu.ModifyAsync(x => x.Embed = BuildMenu(options, "Main Menu", "Welcome to the main menu of the E.R.A. Player editor module!\nPlease " +
                    "choose/say one of the following options to go to the corresponding menu! You can say 'Exit' At any time to return to the previous menu or quit the editor."));
                await menu.ModifyAsync(x => x.Content = " ");
                var reply = await Interactive.NextMessageAsync(Context, timeout: TimeSpan.FromMinutes(5));
                switch (reply.Content.ToLower())
                {
                    case "create":
                        await CreateMenu(Sheet, menu, reply, options, player);
                        break;
                    case "load":
                        await Sheet.ModifyAsync(x => x.Content = "Not Implemented yet!");
                        await Task.Delay(TimeSpan.FromSeconds(5));
                        break;
                    case "delete":
                        await Sheet.ModifyAsync(x => x.Content = "Not Implemented yet!");
                        await Task.Delay(TimeSpan.FromSeconds(5));
                        break;
                    case "exit":
                        await Sheet.DeleteAsync();
                        await menu.ModifyAsync(x => x.Embed = null);
                        await menu.ModifyAsync(x => x.Content = "Thank you for using the E.R.A. Player Editor!");
                        MMenu = false;
                        break;
                    default:
                        await reply.DeleteAsync();
                        break;
                }
            }
        }
        [Command("Player")]
        [RequireContext(ContextType.Guild)]
        [Summary("Displays a player sheet. Usage: `$Player <name>`.")]
        public async Task LoadPlayer(string _Name)
        {
            Directory.CreateDirectory(@"Data/Players/");
            var query = new Player().GetPlayer(_Name);
            if (query.Count() > 1 && query.First().Name.ToLower() != _Name)
            {
                string msg = "Multiple charactes were found! Please specify which one of the following characters is the one you're looking for: ";
                foreach (Player q in query)
                {
                    msg += "`" + q.Name + "` ";
                }
                await Context.Channel.SendMessageAsync(msg);
            }
            else if (query.Count() == 1)
            {
                await Context.Channel.SendMessageAsync("", embed: BuildSheet(query.First(), Context));
            }
            else
            {
                await Context.Channel.SendMessageAsync(Context.User.Mention + "This Character does not exist!");
            }
        }

        public Embed BuildSheet(Player player, SocketCommandContext context)
        {
            var user = context.Guild.GetUser(player.Owner);
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

        public Embed BuildMenu(Dictionary<string, string> options, string Menu, string dialogue)
        {
            var builder = new EmbedBuilder()
                .WithTitle(Menu)
                .WithDescription(dialogue);
            foreach (var option in options)
            {
                builder.AddInlineField(option.Key, option.Value);
            }
            builder.AddField("Exit", "Exits the current menu and returns to the previous one. \nIf you're on the main menu, it exits the editor. \nThe Editor will auto-close after 5 minutes of innactivity.");
            var embed = builder.Build();
            return embed;
        }

        public async Task CreateMenu(RestUserMessage Sheet, RestUserMessage menu, SocketMessage reply, Dictionary<string,string> options, Player player)
        {
            bool CMenu = true;
            while (CMenu == true)
            {
                options.Clear();
                await Sheet.ModifyAsync(x => x.Embed = BuildSheet(player, Context));
                await Sheet.ModifyAsync(x => x.Content = " ");
                await menu.ModifyAsync(x => x.Embed = BuildMenu(options, "Creation Menu", "Lets get started then! First thing first, what is name of your character?\n" +
                    "Please type your Character's name, or type 'Exit' To return to the previous menu. \nOnce this process begins you won't be able to return until you" +
                    " complete the creation process."));
                reply = await Interactive.NextMessageAsync(Context, timeout: TimeSpan.FromMinutes(5));
                if (reply.Content.ToLower() == "exit")
                {
                    await Sheet.ModifyAsync(x => x.Embed = null);
                    await Sheet.ModifyAsync(x => x.Content = "`Returning to Main Menu...`");
                    CMenu = false;
                }
                else if (player.Exists(reply.Content))
                {
                    await menu.ModifyAsync(x => x.Content = "```diff\nA character with this name already exists! Pick a different one or use the `Load` option from the main menu" +
                    "to load an existing player!");
                }
                else
                {
                    player.Name = reply.Content;
                    await reply.DeleteAsync();
                    await Sheet.ModifyAsync(x => x.Embed = BuildSheet(player, Context));
                    await menu.ModifyAsync(x => x.Embed = BuildMenu(options, "Creation Menu | Race", "Alright, lets move to the next step. What is the **race** of your character?"));
                    reply = await Interactive.NextMessageAsync(Context, timeout: TimeSpan.FromMinutes(5));
                    player.Race = reply.Content;

                    await reply.DeleteAsync();
                    await Sheet.ModifyAsync(x => x.Embed = BuildSheet(player, Context));
                    await menu.ModifyAsync(x => x.Embed = BuildMenu(options, "Creation Menu | Class", "What is your character's **class**?\nRemember: your class won't affect your stats, it is merely" +
                        " a title."));
                    reply = await Interactive.NextMessageAsync(Context, timeout: TimeSpan.FromMinutes(5));
                    player.Class = reply.Content;

                    await reply.DeleteAsync();
                    await Sheet.ModifyAsync(x => x.Embed = BuildSheet(player, Context));
                    await menu.ModifyAsync(x => x.Embed = BuildMenu(options, "Creation Menu | Armor", "What is your character's **Armor**?\nRemember: The most armor value you can get is +3 with" +
                        " heavy armor or a tower shield."));
                    reply = await Interactive.NextMessageAsync(Context, timeout: TimeSpan.FromMinutes(5));
                    player.Armor = Convert.ToInt32(reply.Content);

                    await reply.DeleteAsync();
                    await Sheet.ModifyAsync(x => x.Embed = BuildSheet(player, Context));
                    await menu.ModifyAsync(x => x.Embed = BuildMenu(options, "Creation Menu | Max HP", "What is your character's **Max HP**?\nRemember: Unless you have a skill or trait that increases it," +
                        " your initial HP will be **10**."));
                    reply = await Interactive.NextMessageAsync(Context, timeout: TimeSpan.FromMinutes(5));
                    player.MaxHP = Convert.ToInt32(reply.Content);
                    player.CurrHP = player.MaxHP;
                    
                    bool Image = true;
                    while (Image == true) 
                    {
                        await reply.DeleteAsync();
                        await Sheet.ModifyAsync(x => x.Embed = BuildSheet(player, Context));
                        await menu.ModifyAsync(x => x.Embed = BuildMenu(options, "Creation Menu | Image", "What image you'll give to your character? If you have none, say 'none' and a default one will be provided." +
                            "\nIf you do have an image, type or paste the URL (ending in .jpg/.png/.gif) of the image."));
                        reply = await Interactive.NextMessageAsync(Context, timeout: TimeSpan.FromMinutes(5));

                        if (reply.Content.EndsWith(".jpg") || reply.Content.EndsWith(".png") || reply.Content.EndsWith(".jpeg") || reply.Content.EndsWith(".gif"))
                        {
                            player.ImagURL = reply.Content;
                            Image = false;
                        }
                        else if (reply.Content.ToLower().Equals("none"))
                        {
                            Image = false;
                        }
                    }
                    options.Clear();
                    options.Add("Traits", "Proceed to the Traits management menu.");
                    options.Add("Skills", "Proceed to the SKills management menu.");
                    options.Add("Gear", "Proceed to the Gear Management menu.");
                    options.Add("Inventory", "Proceed to the Inventory management menu.");
                    options.Add("Discard", "Discard the character you just created and start all over again.");

                    await reply.DeleteAsync();
                    await Sheet.ModifyAsync(x => x.Embed = BuildSheet(player, Context));
                    await menu.ModifyAsync(x => x.Embed = BuildMenu(options, "Character Basics complete!", "You have successfully added " + player.Name + " into the system! You can now choose whether you want to move " +
                        "on to adding Traits, Skills, add gear or manage inventory. Or if you want to disard this character altogether. /n**Exit** will save your character and let you resume editing later."));
                    reply = await Interactive.NextMessageAsync(Context, timeout: TimeSpan.FromMinutes(5));
                    switch (reply.Content.ToLower())
                    {
                        case "traits":
                            break;
                        case "skills":
                            break;
                        case "gear":
                            break;
                        case "inventory":
                            break;
                        case "discard":
                            break;
                        case "exit":
                            CMenu = false;
                            string json = JsonConvert.SerializeObject(player);
                            File.WriteAllText(player.Path(), json);
                            break;
                        default:
                            await reply.DeleteAsync();
                            break;
                    }
                }
            }
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

        public string Path()
        {
            string path = @"Data/Players/" + this.Name + ".Json";
            return path;
        }
        public IEnumerable<Player> GetPlayer(string _query)
        {
            Directory.CreateDirectory(@"Data/Players/");
            var folder = Directory.EnumerateFiles(@"Data/Players/");
            IList<Player> players = new List<Player> { };
            foreach (string X in folder)
            {
                players.Add(JsonConvert.DeserializeObject<Player>(File.ReadAllText(X)));
            }
            var query = players.Where(x => x.Name.ToLower().StartsWith(_query.ToLower()));
            var query2 = query.OrderBy(x => x.Name);
            return query2;
        }
        public bool Exists(string Name)
        {
            bool exists = false;
            Directory.CreateDirectory(@"Data/Players/");
            var folder = Directory.EnumerateFiles(@"Data/Players/");
            IList<Player> players = new List<Player> { };
            foreach (string X in folder)
            {
                players.Add(JsonConvert.DeserializeObject<Player>(File.ReadAllText(X)));
            }
            var query = players.Where(x => x.Name.ToLower().StartsWith(Name.ToLower()));
            if (query.Count() == 0)
            {
                exists = false;
            }
            else if (query.Count() >= 1)
            {
                exists = true;
            }
            return exists;
        }
    }
    public class Gear
    {
        public string Name { get; set; }
        public string Icon { get; set; }
        public string ImageURL { get; set; }
        public string Description { get; set; }

        public bool Exists(string Name)
        {
            bool exists = false;
            Directory.CreateDirectory(@"Data/Gear/");
            var folder = Directory.EnumerateFiles(@"Data/Gear/");
            IList<Gear> players = new List<Gear> { };
            foreach (string X in folder)
            {
                players.Add(JsonConvert.DeserializeObject<Gear>(File.ReadAllText(X)));
            }
            var query = players.Where(x => x.Name.ToLower().StartsWith(Name.ToLower()));
            if (query.Count() == 0)
            {
                exists = false;
            }
            else if (query.Count() >= 1)
            {
                exists = true;
            }
            return exists;
        }
    }
    public class Skill
    {
        public string Name { get; set; }
        public int Level { get; set; }
        public string Icon { get; set; }
        public string Description { get; set; }

        public bool Exists(string Name)
        {
            bool exists = false;
            Directory.CreateDirectory(@"Data/Skills/");
            var folder = Directory.EnumerateFiles(@"Data/Skills/");
            IList<Skill> players = new List<Skill> { };
            foreach (string X in folder)
            {
                players.Add(JsonConvert.DeserializeObject<Skill>(File.ReadAllText(X)));
            }
            var query = players.Where(x => x.Name.ToLower().StartsWith(Name.ToLower()));
            if (query.Count() == 0)
            {
                exists = false;
            }
            else if (query.Count() >= 1)
            {
                exists = true;
            }
            return exists;
        }
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
        public string ImageURL { get; set; }
        public string Description { get; set; }

        public bool Exists(string Name)
        {
            bool exists = false;
            Directory.CreateDirectory(@"Data/Skills/");
            var folder = Directory.EnumerateFiles(@"Data/Skills/");
            IList<Item> players = new List<Item> { };
            foreach (string X in folder)
            {
                players.Add(JsonConvert.DeserializeObject<Item>(File.ReadAllText(X)));
            }
            var query = players.Where(x => x.Name.ToLower().StartsWith(Name.ToLower()));
            if (query.Count() == 0)
            {
                exists = false;
            }
            else if (query.Count() >= 1)
            {
                exists = true;
            }
            return exists;
        }
    }
}
