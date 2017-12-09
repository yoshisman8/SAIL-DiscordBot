using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Discord;
using Discord.WebSocket;
using Discord.Commands;
using LiteDB;
using System.Threading.Tasks;
using ERA20.Modules.Classes;

namespace ERA20.Modules
{
    [Group("Character")]
    [Alias("Char", "C")]
    public class Second_Edition : ModuleBase<SocketCommandContext>
    {
        public LiteDatabase Database { get; set; }

        [Command]
        public async Task Current()
        {
            var col = Database.GetCollection<Character>("Characters");
            var player = new Playerlock(Database).GetPlayer(Context.User.Id);
            var C = col
                .Include(x => x.Equipment)
                .Include(x => x.Inventory.Items)
                .FindById(player.Character.CharacterId);
            C.Inventory.buildInv(Database);
            C.NullBGone(Database);
            if (player == null)
            {
                await ReplyAsync("You are not locked into any character! Use `$Lock <Character name>` To lock into one!\n" +
"Or if you haven't, make a new character with `$Character Create <Name> <Race> <Class>`!");
                return;
            }
            await ReplyAsync("", embed: new Builders().BuildSheet(C, Context));
        }

        [Command]
        public async Task Char(string Name)
        {
            var col = Database.GetCollection<Character>("Characters");
            if (!col.Exists(x => x.Name == Name.ToLower()))
            {
                await ReplyAsync("I couldn't find this player on the Database!");
                return;
            }

            var Char = col
                .Include(x => x.Inventory.Items)
                .Include(x => x.Equipment)
                .Find(x => x.Name.StartsWith(Name.ToLower()));
            if (Char.Count() > 1 && !Char.ToList().Exists(x => x.Name.ToLower() == Name.ToLower()))
            {
                string msg = "Multiple characters where found with this search! Please specify from one of the following: \n";
                foreach (Character X in Char)
                {
                    msg += "`" + X.Name + "`, ";
                }
                var msg2 = msg.Substring(0, msg.Length - 1);
                msg2 += ".";
                await ReplyAsync(msg2);
            }
            else if (Char.Count() == 1 || Char.ToList().Exists(x => x.Name.ToLower() == Name.ToLower()))
            {
                var C = Char.First();
                C.Inventory.buildInv(Database);
                C.NullBGone(Database);
                await ReplyAsync("", embed: new Builders().BuildSheet(C, Context));
            }
        }

        [Command("Create")]
        [Alias("New", "Add")]
        [Summary("Creates a new character entry in the database and locks you as that character for further edits. Usage: `$Character Create <Name> <Race> <Class>`.")]
        public async Task Create(string Name, string Race, string Class)
        {

            var col = Database.GetCollection<Character>("Characters");
            col.EnsureIndex("Name","LOWER($.Name)");

            if (col.Exists(x => x.Name == Name.ToLower()))
            {
                await ReplyAsync("Someone with this name already exists in the Database!");
            }
            else
            {
                var Char = new Character()
                {
                    Name = Name,
                    Race = Race,
                    Class = Class,
                    Owner = Context.User.Id
                };
                try
                {
                    Char.PassInstance(Database);
                    Char.Add();
                    new Playerlock(Database).Lock(Context.User.Id, Char);
                    await ReplyAsync("Character **" + Char.Name + "** Has been added to the database! As an addition you've been **locked** into this character.\n" +
                        "All `$Edit` Commands will now target this character. If you wish to edit a different character, use `$Lock <character>` to change who you're locked as.");
                }
                catch (Exception e)
                {
                    await ReplyAsync(e.ToString());
                }
            }
        }

        [Command("Delete")]
        [Alias("Remove","Del")]
        [Summary("Deletes a character from the database. Usage: `$Character Delete <Name>`.")]
        public async Task Del(string Name)
        {
            var col = Database.GetCollection<Character>("Characters");
            var user = Context.User as IGuildUser;
            var roles = user.RoleIds.Where(x => x == 324320068748181504);
            
            if (!col.Exists(x => x.Name == Name.ToLower())) { await ReplyAsync("This character doesn't exist!"); return; }

            var C = col.Find(x => x.Name.StartsWith(Name.ToLower()));
            if (C.Count() > 1 && !C.ToList().Exists(x => x.Name.ToLower() == Name.ToLower()))
            {
                string msg = "Multiple characters where found with this search! Please specify from one of the following: \n";
                foreach (Character X in C)
                {
                    msg += "`" + X.Name + "`, ";
                }
                var msg2 = msg.Substring(0, msg.Length - 1);
                msg2 += ".";
                await ReplyAsync(msg2);
            }
            else if ((C.Count() == 1 || C.ToList().Exists(x => x.Name.ToLower() == Name.ToLower())) && (C.First().Owner == user.Id || roles != null))
            {
                var x = C.First();
                x.PassInstance(Database);
                x.Delete();
                await ReplyAsync("Character **" + Name + "** Deleted from the database!");
            }
            else
            {
                await ReplyAsync("You are not the owner of this character!");
            }
        }
        
    }
    
    internal class Builders
    {
        public Embed BuildSheet(Character player, SocketCommandContext context)
        {
            var user = context.Guild.GetUser(player.Owner);
            var builder = new EmbedBuilder()
            .WithFooter(user.Nickname,user.GetAvatarUrl())
            .WithTitle(player.Name)
            .WithUrl(player.ImageUrl)
            .WithDescription(player.Description)
            .WithThumbnailUrl(player.ImageUrl)
            .AddInlineField(":chart_with_upwards_trend: Basic info", "Class: " + player.Class + "\nRace: " + player.Race + "\nStress: " + BuildStress(player) + "\n")
            .AddInlineField(":tools: Gear", Buildequip(player))
            .AddField(":warning: Afflictions", BuildAfflictions(player))
            .AddField(":star2: Physical Trait: " + player.ITrait.Name, player.ITrait.Description)
            .AddInlineField(":star: Traits", BuildTraits(player))
            .AddInlineField(":muscle:  Skills", BuildSkills(player))
            .AddField(":school_satchel: Inventory", BuildInv(player));
            var embed = builder.Build();
            return embed;
        }
        private string BuildStress(Character character)
        {
            string msg = "";
            int A = character.MaxStress - character.Stress;
            
            for (int x = 0; x < character.Stress; x++)
            {
                msg += "\\⚫ ";
            }
            for (int x = 0; x < A; x++)
            {
                msg += "\\⚪ ";
            }
            return msg;
        }
        private string BuildInv(Character player)
        {
            string msg = "";
            msg += "\\💰 $" + Math.Round(player.Money, 2) + "\n";
            foreach (Item x in player.Inventory.Items)
            {
                msg += "* " + x.BaseItem.Name + " x"+x.Quantity+"\n";
            }
            return msg;
        }
        public string Buildequip(Character player)
        {
            string msg = "";
            foreach (BaseItem x in player.Equipment)
            {
                msg += "* " + x.Name + "\n";
            }
            if (msg.Length == 0) { return "None! Use `$Equip <Item>` to Equip an item!"; }
            return msg;
        }
        public string BuildTraits(Character player)
        {
            string msg = "";
            foreach (Trait x in player.Traits)
            {
                msg += "* " + x.Name + "\n";
            }
            if (msg.Length == 0) { return "None! Use `$Traits add <Name> <Description>` to add a trait!"; }
            return msg;
        }
        public string BuildAfflictions(Character player)
        {
            string msg = "";
            foreach (Affliction x in player.Afflictions)
            {
                msg += "* "+ x.Name + "\n";
            }
            if (msg.Length == 0) { return "No Afflictions so far!"; }
            return msg;
        }
        public string BuildSkills(Character player)
        {
            string msg = "";
            foreach (Skill x in player.Skills)
            {
                msg += "* " + x.Name + " [" + ToRoman(x.Level) + "]\n";
            }
            if (msg.Length == 0) { return "None! \nUse `$Skills Learn <Name> <Emote> <Description>` \nTo learn a new Skill!"; }
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
        public Embed TraitEmbed(Character character)
        {
            var builder = new EmbedBuilder()
                .WithTitle(character.Name + "'s Traits");
            foreach (Trait x in character.Traits)
            {
                builder.AddField(x.Name, x.Description);
            }
            return builder.Build();
        }
        public Embed SkillEmbed(Character character)
        {
            var builder = new EmbedBuilder()
                .WithTitle(character.Name + "'s Skills");
            foreach (Skill x in character.Skills)
            {
                builder.AddField(x.Name+" ["+ToRoman(x.Level)+"]", x.Description);
            }
            return builder.Build();
        }
        public Embed ItemBuilder(BaseItem item)
        {
            var builder = new EmbedBuilder()
                .WithTitle(item.Name)
                .WithDescription(item.Description)
                .WithThumbnailUrl(item.ImageUrl);
            return builder.Build();
        }
        public Embed AfflictionBuilder(Character character)
        {
            var builder = new EmbedBuilder()
                .WithTitle(character.Name + "'s Current Afflictions");
            foreach (Affliction x in character.Afflictions)
            {
                builder.AddField(x.Name, x.Description);
            }
            return builder.Build();
        }
    }

    [Group("Lock")]
    [RequireContext(ContextType.Guild)]
    public class Lock : ModuleBase<SocketCommandContext>
    {
        public LiteDatabase Database { get; set; }

        [Command]
        public async Task GetCurrent()
        {
            var player = new Playerlock(Database).GetPlayer(Context.User.Id);
            if (player == null) {
                await ReplyAsync("You are not locked into any character! Use `$Lock <Character name>` To lock into one!\n" +
"Or if you haven't, make a new character with `$Character Create <Name> <Race> <Class>`!");
                return;
            }
            await ReplyAsync("You're currently locked to **" + player.Character.Name + "**.");
        }
        [Command]
        public async Task SetLock(string Name)
        {
            var user = Context.User as SocketGuildUser;
            var DMs = Context.Guild.GetRole(324320068748181504);

            var col = Database.GetCollection<Character>("Characters");
            if (!col.Exists(x => x.Name == Name.ToLower()))
            {
                await ReplyAsync("I couldn't find this player on the Database!");
                return;
            }

            var Char = col.Find(x => x.Name.StartsWith(Name.ToLower()));

            if (Char.Count() > 1 && Char.First().Name.ToLower() != Name.ToLower())
            {
                string msg = "Multiple characters where found with this search! Please specify from one of the following: \n";
                foreach (Character X in Char)
                {
                    msg += "`" + X.Name + "`, ";
                }
                var msg2 = msg.Substring(0, msg.Length - 1);
                msg2 += ".";
                await ReplyAsync(msg2);
            }
            else if ((Char.Count() == 1 || Char.First().Name.ToLower() == Name.ToLower()) && (Char.First().Owner == Context.User.Id || user.Roles.Contains(DMs)))
            {
                var cha = Char.First();
                new Playerlock(Database).Lock(Context.User.Id, cha);
                await ReplyAsync("You've been locked to character **" + cha.Name + "**!");
            }
        }
        [Command("?")]
        public async Task GetOther(string Name)
        {
            var User = GetUser(Name);
            if (User == null)
            {
                await ReplyAsync("I couldn't find that User on this server!");
            }
            else
            {
                var player = new Playerlock(Database).GetPlayer(User.Id);
                if (player == null) { await ReplyAsync("This user has no character locked! Perhaps they haven't made a character yet?"); return; }
                await ReplyAsync(User.Username + " is currently locked with character " + player.Character.Name + ".");
            }
        }
        public IUser GetUser(string name)
        {
            var user = Context.Guild.Users.Where(x => x.Username.ToLower().Contains(name.ToLower()));
            if (user.Count() == 0) { return null; }
            else { return user.First() as IUser; }
        }
    }

    [Group("Edit")]
    public class Editor : ModuleBase<SocketCommandContext>
    {
        public LiteDatabase Database { get; set; }

        [Command]
        public async Task Help()
        {
            await ReplyAsync("Welcome to the Editor! All the commands I will be listing are meant to go after the `$Edit`.\n" +
                "Like so: `$Edit Name <New Name>`.\nHere's the list of all the possible things you can edit through this command:\n" +
                "- Name `Name <New name>`\n" +
                "- Class `Class <New Class>`\n" +
                "- Race `Race <New Race>` \n" +
                "- Image `Image <Image URL>`\n" +
                "- Description `Description <Character Description>`\n" +
                "- Physical Trait(s) `PTrait <Name> <Description>`.\n" +
                "- Max stress `Stress <number>`"+
                "***REMEMBER***: You can only use this commands if you have a character locked (Use `$Lock` to verify).");
        }
        [Command("Name")]
        public async Task Name( string Name)
        {
            var character = new Playerlock(Database).GetPlayer(Context.User.Id).Character;
            character.Name = Name;
            character.PassInstance(Database);
            character.Update();
            await ReplyAsync("You changed this character's name to **" + Name + "**!");
        }
        [Command("Class")]
        public async Task Class( string Name)
        {
            var character = new Playerlock(Database).GetPlayer(Context.User.Id).Character;
            character.Class = Name;
            character.PassInstance(Database);
            character.Update();
            await ReplyAsync("You changed this character's class to **" + Name + "**!");
        }
        [Command("Race")]
        public async Task Race( string Name)
        {
            var character = new Playerlock(Database).GetPlayer(Context.User.Id).Character;
            character.Race = Name;
            character.PassInstance(Database);
            character.Update();
            await ReplyAsync("You changed this character's Race to **" + Name + "**!");
        }
        [Command("Image")]
        public async Task Img( string Name)
        {
            var character = new Playerlock(Database).GetPlayer(Context.User.Id).Character;
            character.ImageUrl = Name;
            character.PassInstance(Database);
            character.Update();
            await ReplyAsync("You changed this character's Image URL!");
        }
        [Command("Description"), Alias("Desc")]
        public async Task Desc( string Name)
        {
            var character = new Playerlock(Database).GetPlayer(Context.User.Id).Character;
            character.Description = Name;
            character.PassInstance(Database);
            character.Update();
            await ReplyAsync("You changed this character's Description!");
        }
        [Command("PTrait")]
        public async Task Trait(string Name,  string Description)
        {
            var character = new Playerlock(Database).GetPlayer(Context.User.Id).Character;
            character.ITrait.Name = Name;
            character.ITrait.Description = Description;
            character.PassInstance(Database);
            character.Update();
            await ReplyAsync("You changed this character's Physical Trait!");
        }
        [Command("Stress")]
        public async Task stress(int value)
        {
            var character = new Playerlock(Database).GetPlayer(Context.User.Id).Character;
            character.MaxStress = Math.Abs(value);
            character.PassInstance(Database);
            character.Update();
            await ReplyAsync("You changed this character's max stress!");
        }
    }

    [Group("Traits")]
    public class TraitEditor : ModuleBase<SocketCommandContext>
    {
        public LiteDatabase Database { get; set; }

        [Command]
        public async Task GetCurr()
        {
            var character = new Playerlock(Database).GetPlayer(Context.User.Id).Character;
            await ReplyAsync("", embed: new Builders().TraitEmbed(character));
        }
        [Command]
        public async Task GetOther(string Name)
        {
            var col = Database.GetCollection<Character>("Characters");
            if (!col.Exists(x => x.Name == Name.ToLower()))
            {
                await ReplyAsync("I couldn't find this player on the Database!");
                return;
            }

            var Char = col
                .Include(x => x.Inventory.Items)
                .Include(x => x.Equipment)
                .Find(x => x.Name.StartsWith(Name.ToLower()));

            if (Char.Count() > 1)
            {
                string msg = "Multiple characters where found with this search! Please specify from one of the following: \n";
                foreach (Character X in Char)
                {
                    msg += "`" + X.Name + "`, ";
                }
                var msg2 = msg.Substring(0, msg.Length - 1);
                msg2 += ".";
                await ReplyAsync(msg2);
            }
            else if (Char.Count() == 1)
            {
                await ReplyAsync("", embed: new Builders().TraitEmbed(Char.First()));
            }
        }
        [Command("Help")]
        public async Task help()
        {
            await ReplyAsync("Welcome to the Trait editor! Here you can only do one of two options:\n" +
                "Either `$Trait Add <Name> <Description>` to add a trait to your *locked* character.\n" +
                "Or `$Trait Remove <Name>` To remove a trait!");
        }
        [Command("Add")]
        public async Task Add(string Name,  string Descriotion)
        {
            var character = new Playerlock(Database).GetPlayer(Context.User.Id).Character;
            character.PassInstance(Database);
            if (character.Traits.Count() >= 2)
            {
                await ReplyAsync("You already have 2 traits! Delete some before you add another one!");
                return;
            }
            character.Traits.Add(new Trait()
            {
                Name = Name,
                Description = Descriotion
            });
            character.Update();
            await ReplyAsync("You successfully added the trait **" + Name + "** to your character!");
        }
        [Command("Remove"), Alias("Del","Rem")]
        public async Task Rem(string Name)
        {
            var character = new Playerlock(Database).GetPlayer(Context.User.Id).Character;
            character.PassInstance(Database);
            var trait = character.Traits.Find(x => x.Name.ToLower().StartsWith(Name.ToLower()));
            if (trait == null) { await ReplyAsync("You dont have this trait!"); return; }
        }
    }

    [Group("Affliction"), Alias("Aff","Affs")]
    public class AfflictionMGR : ModuleBase<SocketCommandContext>
    {
        public LiteDatabase Database { get; set; }
        [Command]
        public async Task GetCurr()
        {
            var character = new Playerlock(Database).GetPlayer(Context.User.Id).Character;
            await ReplyAsync("", embed: new Builders().AfflictionBuilder(character));
        }
        [Command]
        public async Task GetOther(string Name)
        {
            var col = Database.GetCollection<Character>("Characters");
            if (!col.Exists(x => x.Name == Name.ToLower()))
            {
                await ReplyAsync("I couldn't find this player on the Database!");
                return;
            }

            var Char = col
                .Include(x => x.Inventory.Items)
                .Include(x => x.Equipment)
                .Find(x => x.Name.StartsWith(Name.ToLower()));

            if (Char.Count() > 1 && !Char.ToList().Exists(x => x.Name.ToLower() == Name.ToLower()))
            {
                string msg = "Multiple characters where found with this search! Please specify from one of the following: \n";
                foreach (Character X in Char)
                {
                    msg += "`" + X.Name + "`, ";
                }
                var msg2 = msg.Substring(0, msg.Length - 1);
                msg2 += ".";
                await ReplyAsync(msg2);
            }
            else if (Char.Count() == 1 || Char.ToList().Exists(x => x.Name.ToLower() == Name.ToLower()))
            {
                await ReplyAsync("", embed: new Builders().AfflictionBuilder(Char.First()));
            }
        }
        [Command("Give")]
        [RequireUserPermission(GuildPermission.ManageMessages)]
        public async Task Give(string Name, string Aff,  string Description)
        {
            var col = Database.GetCollection<Character>("Characters");
            if (!col.Exists(x => x.Name == Name.ToLower()))
            {
                await ReplyAsync("I couldn't find this player on the Database!");
                return;
            }

            var Char = col
                .Include(x => x.Inventory.Items)
                .Include(x => x.Equipment)
                .Find(x => x.Name.StartsWith(Name.ToLower()));

            if (Char.Count() > 1 && !Char.ToList().Exists(x => x.Name.ToLower() == Name.ToLower()))
            {
                string msg = "Multiple characters where found with this search! Please specify from one of the following: \n";
                foreach (Character X in Char)
                {
                    msg += "`" + X.Name + "`, ";
                }
                var msg2 = msg.Substring(0, msg.Length - 1);
                msg2 += ".";
                await ReplyAsync(msg2);
            }
            else if (Char.Count() == 1 || Char.ToList().Exists(x => x.Name.ToLower() == Name.ToLower()))
            {
                var c = col
                .Include(x => x.Inventory.Items)
                .Include(x => x.Equipment)
                .FindById(Char.First().CharacterId);

                c.PassInstance(Database);
                c.Afflictions.Add(new Affliction() {
                    Name = Aff,
                    Description = Description
                });
                col.Update(c);
                c.Update();
                await ReplyAsync(c.Name + " is now afflicted with **" + Aff + "**!");
            }
        }
        [Command("Take"), Alias("Del")]
        [RequireUserPermission(GuildPermission.ManageMessages)]
        public async Task take(string Name, string Aff)
        {
            var col = Database.GetCollection<Character>("Characters");
            if (!col.Exists(x => x.Name == Name.ToLower()))
            {
                await ReplyAsync("I couldn't find this player on the Database!");
                return;
            }

            var Char = col
                .Include(x => x.Inventory.Items)
                .Include(x => x.Equipment)
                .Find(x => x.Name.StartsWith(Name.ToLower()));

            if (Char.Count() > 1 && Char.First().Name.ToLower() != Name.ToLower())
            {
                string msg = "Multiple characters where found with this search! Please specify from one of the following: \n";
                foreach (Character X in Char)
                {
                    msg += "`" + X.Name + "`, ";
                }
                var msg2 = msg.Substring(0, msg.Length - 1);
                msg2 += ".";
                await ReplyAsync(msg2);
            }
            else if (Char.Count() == 1 || Char.ToList().Exists(x => x.Name.ToLower() == Name.ToLower()))
            {
                var c = Char.First();
                var affs = c.Afflictions.Where(x => x.Name.ToLower().Contains(Aff.ToLower()));
                if (!c.Afflictions.Exists(x => x.Name.ToLower().StartsWith(Aff.ToLower())))
                {
                    await ReplyAsync("You are not afflicted by " + Aff + "!");
                }
                if (affs.Count() > 1 && affs.First().Name.ToLower() != Aff.ToLower())
                {
                    var msg = "Multiple Afflictions where found with this name, please specify which of the following is the one you're tring to remove:\n";
                    foreach (Affliction x in affs)
                    {
                        msg += "`" + x.Name + "`, ";
                    }
                    var msg2 = msg.Substring(0, msg.Length - 2);
                    msg2 += ".";
                    await ReplyAsync(msg2);
                }
                else
                {
                    c.Afflictions.Remove(affs.First());
                    await ReplyAsync(c.Name + " is no longer afflicted with **" + Aff + "**!");
                }
            }
        }
        [Command("Clear")]
        [RequireUserPermission(GuildPermission.ManageMessages)]
        public async Task Clear(string Name)
        {
            var col = Database.GetCollection<Character>("Characters");
            if (!col.Exists(x => x.Name == Name.ToLower()))
            {
                await ReplyAsync("I couldn't find this player on the Database!");
                return;
            }

            var Char = col
                .Include(x => x.Inventory.Items)
                .Include(x => x.Equipment)
                .Find(x => x.Name.StartsWith(Name.ToLower()));

            if (Char.Count() > 1 && Char.First().Name.ToLower() != Name.ToLower())
            {
                string msg = "Multiple characters where found with this search! Please specify from one of the following: \n";
                foreach (Character X in Char)
                {
                    msg += "`" + X.Name + "`, ";
                }
                var msg2 = msg.Substring(0, msg.Length - 1);
                msg2 += ".";
                await ReplyAsync(msg2);
            }
            else if (Char.Count() == 1)
            {
                var c = Char.First();
                c.Afflictions.Clear();
                c.Update();
                await ReplyAsync(c.Name + " Has been cleared of Afflictions!");
            }
        }
    }

    [Group("Skills"), Alias("Skill", "S")]
    public class SkillMGR : ModuleBase<SocketCommandContext>
    {
        public LiteDatabase Database { get; set; }
        [Command]
        public async Task GetCurr()
        {
            var C = new Playerlock(Database).GetPlayer(Context.User.Id).Character;
            await ReplyAsync("", embed: new Builders().SkillEmbed(C));
        }
        [Command]
        public async Task Getother(string Name)
        {
            var col = Database.GetCollection<Character>("Characters");
            if (!col.Exists(x => x.Name == Name.ToLower()))
            {
                await ReplyAsync("I couldn't find this player on the Database!");
                return;
            }

            var Char = col
                .Include(x => x.Inventory.Items)
                .Include(x => x.Equipment)
                .Find(x => x.Name.StartsWith(Name.ToLower()));

            if (Char.Count() > 1)
            {
                string msg = "Multiple characters where found with this search! Please specify from one of the following: \n";
                foreach (Character X in Char)
                {
                    msg += "`" + X.Name + "`, ";
                }
                var msg2 = msg.Substring(0, msg.Length - 1);
                msg2 += ".";
                await ReplyAsync(msg2);
            }
            else if (Char.Count() == 1)
            {
                await ReplyAsync("", embed: new Builders().SkillEmbed(Char.First()));
            }
        }
        [Command("Learn"), Alias("Add")]
        public async Task Learn(string Name,  string Description)
        {
            var C = new Playerlock(Database).GetPlayer(Context.User.Id).Character;
            if (C.Skills.Exists(x => x.Name.ToLower() == Name.ToLower()))
            {
                await ReplyAsync("You already know **" + Name + "**!");
                return;
            }
            C.PassInstance(Database);
            C.Skills.Add(new Skill()
            {
                Name = Name,
                Description = Description
            });
            C.Update();
            await ReplyAsync("You have learned the skill **" + Name + "**!");
        }
        [Command("Forget"), Alias("Del", "Rem")]
        public async Task Forget(string Name)
        {
            var C = new Playerlock(Database).GetPlayer(Context.User.Id).Character;
            C.PassInstance(Database);
            var skills = C.Skills.FindAll(x => x.Name.ToLower() == Name.ToLower());
            if (!C.Skills.Exists(x => x.Name.ToLower() == Name.ToLower()))
            {
                await ReplyAsync("You don't know **" + Name + "**!");
                return;
            }
            else if (skills.Count() > 1 && skills.First().Name.ToLower() != Name.ToLower())
            {
                var msg = "Multiple Skills with this name were found! Please specify which one of these is the one you want to forget: ";
                foreach(Skill x in skills)
                {
                    msg += "`" + x.Name + "`, ";
                }
                var msg2 = msg.Substring(0, msg.Length - 1);
                msg2 += ".";
                await ReplyAsync(msg2);
            }
            else
            {
                var skill = skills.First();
                C.Skills.Remove(skill);
                C.Update();
                await ReplyAsync("You no longer know the skill **" + skill.Name + "**!");
            }
        }
        [Command("LevelUp"), Alias("Level-Up", "LU")]
        public async Task LU( string Name)
        {
            var C = new Playerlock(Database).GetPlayer(Context.User.Id).Character;
            C.PassInstance(Database);
            var skills = C.Skills.FindAll(x => x.Name.ToLower().StartsWith(Name.ToLower()));
            if (!C.Skills.Exists(x => x.Name.ToLower() == Name.ToLower()))
            {
                await ReplyAsync("You don't know **" + Name + "**!");
                return;
            }
            else if (skills.Count() > 1 && skills.First().Name.ToLower() != Name.ToLower())
            {
                var msg = "Multiple Skills with this name were found! Please specify which one of these is the one you want to forget: ";
                foreach (Skill x in skills)
                {
                    msg += "`" + x.Name + "`, ";
                }
                var msg2 = msg.Substring(0, msg.Length - 1);
                msg2 += ".";
                await ReplyAsync(msg2);
            }
            else
            {
                var skill = skills.First();
                if (skill.Level == 5) { await ReplyAsync("This skill is already at Max level!"); return; }
                else
                {
                    C.Skills.ElementAt(C.Skills.IndexOf(skill)).Level++;
                    C.Update();
                    await ReplyAsync("Skill **" + skill.Name + "** leved up to level `[" + Builders.ToRoman(skill.Level) + "]`!");
                }
            }
        }
    }

    [Group("Items"), Alias("Item","I")]
    public class InvMGR : ModuleBase<SocketCommandContext>
    {
        public LiteDatabase Database { get; set; }

        [Command]
        public async Task Get(string Name)
        {

            var col = Database.GetCollection<BaseItem>("Items");
            col.EnsureIndex("Name", "LOWER($.Name)");
            var c = col.Find(x => x.Name.StartsWith(Name.ToLower()));

            if (!col.Exists(x => x.Name.StartsWith(Name.ToLower())))
            {
                await ReplyAsync("This item does not exist in the databse!");
            }
            else if (c.Count() > 1 && c.First().Name.ToLower() != Name.ToLower())
            {
                var msg = "Your search brought up multiple results, please specify which one of these items you want to look at: ";
                foreach (BaseItem x in c)
                {
                    msg += "`" + x.Name + "`, ";
                }
                var msg2 = msg.Substring(0, msg.Length - 1);
                msg2 += ".";
                await ReplyAsync(msg2);
            }
            else
            {
                await ReplyAsync("", embed: new Builders().ItemBuilder(c.First()));
            }
        }
        [Command("Create"), Alias("New", "Add")]
        [RequireUserPermission(GuildPermission.ManageMessages)]
        public async Task New(string Name, string Description, string ImageURL = "")
        {
            var col = Database.GetCollection<BaseItem>("Items");
            col.EnsureIndex("Name", "LOWER($.Name)");

            if (col.Exists(x => x.Name.StartsWith(Name.ToLower())))
            {
                await ReplyAsync("This item does not exist in the databse!");
            }
            else
            {
                col.Insert(new BaseItem()
                {
                    Name = Name,
                    Description = Description,
                    ImageUrl = ImageURL
                });
                await ReplyAsync("Item **" + Name + "** added to the database successfully! You can now use `$Items Give <character> " + Name + "` to give this items to any character!");
            }
        }
        [Command("Delete"), Alias("Del", "Remove", "Rem")]
        [RequireUserPermission(GuildPermission.ManageMessages)]
        public async Task rem( string Name)
        {
            var col = Database.GetCollection<BaseItem>("Items");
            col.EnsureIndex("Name", "LOWER($.Name)");
            var c = col.Find(x => x.Name.StartsWith(Name.ToLower()));

            if (c.Count() == 0)
            {
                await ReplyAsync("This item does not exist in the databse!");
            }
            else if (c.Count() > 1 && c.First().Name.ToLower() != Name.ToLower())
            {
                var msg = "Your search brought up multiple results, please specify which one of these items you want to look at: ";
                foreach (BaseItem x in c)
                {
                    msg += "`" + x.Name + "`, ";
                }
                var msg2 = msg.Substring(0, msg.Length - 1);
                msg2 += ".";
                await ReplyAsync(msg2);
            }
            else if (c.Count() == 1 || c.ToList().Exists(x => x.Name.ToLower() == Name.ToLower()))
            {
                var I = c.First();
                col.Delete(x => x.ItemId == c.First().ItemId);
                await ReplyAsync("Item **" + I.Name + "** was siccessfully deleted from the database!");
            }
        }
        [Command("Edit")]
        [RequireUserPermission(GuildPermission.ManageMessages)]
        public async Task Edit(string Name, string Property,  string Value)
        {
            var col = Database.GetCollection<BaseItem>("Items");
            col.EnsureIndex("Name", "LOWER($.Name)");
            var c = col.Find(x => x.Name.StartsWith(Name.ToLower()));

            if (!col.Exists(x => x.Name.StartsWith(Name.ToLower())))
            {
                await ReplyAsync("This item does not exist in the databse!");
            }
            else if (c.Count() > 1 && c.First().Name.ToLower() != Name.ToLower())
            {
                var msg = "Your search brought up multiple results, please specify which one of these items you want to look at: ";
                foreach (BaseItem x in c)
                {
                    msg += "`" + x.Name + "`, ";
                }
                var msg2 = msg.Substring(0, msg.Length - 1);
                msg2 += ".";
                await ReplyAsync(msg2);
            }
            else
            {
                var I = c.First();
                switch (Property.ToLower())
                {
                    case "name":
                        I.Name = Value;
                        col.Update(I);
                        await ReplyAsync("Item name changed to **" + Value + "**!");
                        break;
                    case "description":
                        I.Description = Value;
                        col.Update(I);
                        await ReplyAsync("Item description changed!");
                        break;
                    case "image":
                        I.ImageUrl = Value;
                        col.Update(I);
                        await ReplyAsync("Item's Image URL changed!");
                        break;
                    default:
                        await ReplyAsync("Invalid property to change! The properties you can change are `Name`, `Description` or `Image`");
                        break;
                }
                
            }
        }
        [Command("Give")]
        [RequireUserPermission(GuildPermission.ManageMessages)]
        public async Task Give(string Name, string Item, int Ammount = 1)
        {
            var col = Database.GetCollection<Character>("Characters");
            if (!col.Exists(x => x.Name == Name.ToLower()))
            {
                await ReplyAsync("I couldn't find this player on the Database!");
                return;
            }

            var Char = col
                .Include(x => x.Inventory.Items)
                .Include(x => x.Equipment)
                .Find(x => x.Name.StartsWith(Name.ToLower()));

            if (Char.Count() > 1 && !Char.ToList().Exists(x => x.Name.ToLower() == Name.ToLower()))
            {
                string msg = "Multiple characters where found with this search! Please specify from one of the following: \n";
                foreach (Character X in Char)
                {
                    msg += "`" + X.Name + "`, ";
                }
                var msg2 = msg.Substring(0, msg.Length - 1);
                msg2 += ".";
                await ReplyAsync(msg2);
            }
            else if (Char.Count() == 1 || Char.ToList().Exists(x => x.Name.ToLower() == Name.ToLower()))
            {
                var c = Char.First();

                var col2 = Database.GetCollection<BaseItem>("Items");
                col2.EnsureIndex("Name", "LOWER($.Name)");
                var I = col2.Find(x => x.Name.StartsWith(Item.ToLower()));

                if (!col2.Exists(x => x.Name.StartsWith(Item.ToLower())))
                {
                    await ReplyAsync("This item does not exist in the databse!");
                }
                else if (I.Count() > 1 && I.First().Name.ToLower() != Item.ToLower())
                {
                    var msg = "Your search brought up multiple results, please specify which one of these items you want to look at: ";
                    foreach (BaseItem x in I)
                    {
                        msg += "`" + x.Name + "`, ";
                    }
                    var msg2 = msg.Substring(0, msg.Length - 1);
                    msg2 += ".";
                    await ReplyAsync(msg2);
                }
                else
                {
                    c.PassInstance(Database);
                    c.Inventory.Add(new Item()
                    {
                        BaseItem = I.First()
                    },Ammount);
                    col.Update(c);
                    await ReplyAsync("You gave **" + c.Name + "** " + Ammount + " **" + I.First().Name + "**(s)!");
                }
            }
        }
        [Command("Take")]
        [RequireUserPermission(GuildPermission.ManageMessages)]
        public async Task Take(string Name, string Item, int Ammount = 1)
        {
            var col = Database.GetCollection<Character>("Characters");
            if (!col.Exists(x => x.Name == Name.ToLower()))
            {
                await ReplyAsync("I couldn't find this player on the Database!");
                return;
            }

            var Char = col
                .Include(x => x.Inventory.Items)
                .Include(x => x.Equipment)
                .Find(x => x.Name.StartsWith(Name.ToLower()));

            if (Char.Count() > 1 && !Char.ToList().Exists(x => x.Name.ToLower() == Name.ToLower()))
            {
                string msg = "Multiple characters where found with this search! Please specify from one of the following: \n";
                foreach (Character X in Char)
                {
                    msg += "`" + X.Name + "`, ";
                }
                var msg2 = msg.Substring(0, msg.Length - 1);
                msg2 += ".";
                await ReplyAsync(msg2);
            }
            else if (Char.Count() == 1 || Char.ToList().Exists(x => x.Name.ToLower() == Name.ToLower()))
            {
                var c = Char.First();
                c.Inventory.buildInv(Database);
                var col2 = Database.GetCollection<BaseItem>("Items");
                col2.EnsureIndex("Name", "LOWER($.Name)");
                var I = col2.Find(x => x.Name.StartsWith(Item.ToLower()));

                if (!col2.Exists(x => x.Name.StartsWith(Item.ToLower())))
                {
                    await ReplyAsync("This item does not exist in the databse!");
                }
                else if (I.Count() > 1 && I.First().Name.ToLower() != Item.ToLower())
                {
                    var msg = "Your search brought up multiple results, please specify which one of these items you want to look at: ";
                    foreach (BaseItem x in I)
                    {
                        msg += "`" + x.Name + "`, ";
                    }
                    var msg2 = msg.Substring(0, msg.Length - 1);
                    msg2 += ".";
                    await ReplyAsync(msg2);
                }
                else
                { 
                    if (!c.Inventory.Items.Exists(x => x.BaseItem.ItemId == I.First().ItemId)) { await ReplyAsync(c.Name + " doesn't have the item " + I.First().Name + " in their inventory!"); return; }
                    c.PassInstance(Database);
                    c.Inventory.Consume(I.First(), Ammount);
                    c.Update();
                    await ReplyAsync("You Took away "+Ammount+" **" + I.First().Name + "** from **" + c.Name + "**!");
                }
            }
        }
        [Command("Empty")]
        [RequireUserPermission(GuildPermission.ManageMessages)]
        public async Task empty(string Name)
        {
            var col = Database.GetCollection<Character>("Characters");
            if (!col.Exists(x => x.Name == Name.ToLower()))
            {
                await ReplyAsync("I couldn't find this player on the Database!");
                return;
            }

            var Char = col
                .Include(x => x.Inventory.Items)
                .Include(x => x.Equipment)
                .Find(x => x.Name.StartsWith(Name.ToLower()));

            if (Char.Count() > 1 && !Char.ToList().Exists(x => x.Name.ToLower() == Name.ToLower()))
            {
                string msg = "Multiple characters where found with this search! Please specify from one of the following: \n";
                foreach (Character X in Char)
                {
                    msg += "`" + X.Name + "`, ";
                }
                var msg2 = msg.Substring(0, msg.Length - 1);
                msg2 += ".";
                await ReplyAsync(msg2);
            }
            else if (Char.Count() == 1 || Char.ToList().Exists(x => x.Name.ToLower() == Name.ToLower()))
            {
                var c = Char.First();
                c.PassInstance(Database);
                c.Inventory.Items.Clear();
                c.Update();
                await ReplyAsync("Purged " + c.Name + "'s Inventory!");
            }
        }
    }
    public class PlayerToPlayer : ModuleBase<SocketCommandContext>
    {
        public LiteDatabase Database { get; set; }

        [Command("Gift"), Alias("Give")]
        [Summary("Gives another player an item from your currently-locked character's inventory. Usage: `$Gift <Character> <Item> <Amount>`. (Amount defaults to 1 if nothing is specified)")]
        public async Task Give(string Player, string Item, int Quantity = 1)
        {
            Quantity = Math.Abs(Quantity);
            var col = Database.GetCollection<Character>("Characters");
            if (!col.Exists(x => x.Name == Player.ToLower()))
            {
                await ReplyAsync("I couldn't find this player on the Database!");
                return;
            }

            var Char = col
                .Include(x => x.Inventory.Items)
                .Include(x => x.Equipment)
                .Find(x => x.Name.StartsWith(Player.ToLower()));
            if (Char.Count() > 1 && !Char.ToList().Exists(x => x.Name.ToLower() == Player.ToLower()))
            {
                string msg = "Multiple characters where found with this search! Please specify from one of the following: \n";
                foreach (Character X in Char)
                {
                    msg += "`" + X.Name + "`, ";
                }
                var msg2 = msg.Substring(0, msg.Length - 1);
                msg2 += ".";
                await ReplyAsync(msg2);
            }
            else if (Char.Count() == 1 || Char.ToList().Exists(x => x.Name.ToLower() == Player.ToLower()))
            {
                var C = Char.First();
                C.Inventory.buildInv(Database);
                C.PassInstance(Database);
                var items = Database.GetCollection<BaseItem>("Items");
                var me = new Playerlock(Database).GetPlayer(Context.User.Id).Character;
                me.Inventory.buildInv(Database);
                me.PassInstance(Database);

                if (!me.Inventory.Items.Exists(x => x.BaseItem.Name.ToLower().StartsWith(Item.ToLower())))
                {
                    await ReplyAsync("You don't have this item in your inventory!");
                    return;
                }
                var I = me.Inventory.Items.Find(x => x.BaseItem.Name.ToLower().StartsWith(Item.ToLower()));
                if (I.Quantity < Quantity)
                {
                    await ReplyAsync("You dont have this many " + I.BaseItem.Name + "s!");
                    return;
                }
                me.Inventory.Consume(I.BaseItem, Quantity);
                C.Inventory.Add(I, Quantity);
                me.Update();
                C.Update();
                await ReplyAsync(me.Name+" gave **" + C.Name + "** " + Quantity + " **" + I.BaseItem.Name + "**(s)!");
            }
        }
        [Command("Use"), Alias("Consume", "Toss","Discard")]
        [Summary("Use, Consume, Toss or otherwise spend an item by a defined ammount. Usage: `$Use <Item> <Amount>`. (Amount defaults to 1 if not specified)")]
        public async Task Discard(string Item, int Ammount = 1)
        {
            Ammount = Math.Abs(Ammount);
            var me = new Playerlock(Database).GetPlayer(Context.User.Id).Character;
            me.Inventory.buildInv(Database);
            me.PassInstance(Database);
            if (!me.Inventory.Items.Exists(x => x.BaseItem.Name.ToLower().StartsWith(Item.ToLower())))
            {
                await ReplyAsync(me.Name+" doesn't have this item in their inventory!");
                return;
            }
            var I = me.Inventory.Items.Find(x => x.BaseItem.Name.ToLower().StartsWith(Item.ToLower()));
            if (I.Quantity < Ammount)
            {
                await ReplyAsync(me.Name+" doesn't have this many " + I.BaseItem.Name + "s!");
                return;
            }
            me.Inventory.Consume(I.BaseItem, Ammount);
            me.Update();
            await ReplyAsync(me.Name+" used up " + Ammount + " of their **" + I.BaseItem.Name + "**(s)!", embed: new Builders().ItemBuilder(I.BaseItem));
        }
        [Command("Pay"), Alias("Spend")]
        [Summary("Spend money on something or transfer money to someone! Usage: `$Pay <Amount> [Player]` The player parameter is optional, if non is specified, you'll simply spend the amount indicated.")]
        public async Task Spend(double Amount, string Name = null)
        {
            Amount = Math.Abs(Amount);
            Amount = Math.Round(Amount, 2);
            var me = new Playerlock(Database).GetPlayer(Context.User.Id).Character;
            me.Inventory.buildInv(Database);
            me.PassInstance(Database);
            if (me.Money < Amount)
            {
                await ReplyAsync(me.Name+" doesn't have this much money!");
            }
            if (Name != null)
            {
                var col = Database.GetCollection<Character>("Characters");
                if (!col.Exists(x => x.Name == Name.ToLower()))
                {
                    await ReplyAsync("I couldn't find this player on the Database!");
                    return;
                }

                var Char = col
                    .Include(x => x.Inventory.Items)
                    .Include(x => x.Equipment)
                    .Find(x => x.Name.StartsWith(Name.ToLower()));
                if (Char.Count() > 1 && !Char.ToList().Exists(x => x.Name.ToLower() == Name.ToLower()))
                {
                    string msg = "Multiple characters where found with this search! Please specify from one of the following: \n";
                    foreach (Character X in Char)
                    {
                        msg += "`" + X.Name + "`, ";
                    }
                    var msg2 = msg.Substring(0, msg.Length - 1);
                    msg2 += ".";
                    await ReplyAsync(msg2);
                }
                else if (Char.Count() == 1|| Char.ToList().Exists(x => x.Name.ToLower() == Name.ToLower()))
                {
                    var C = Char.First();
                    C.PassInstance(Database);
                    C.Money += Math.Round(Amount, 2);
                    me.Money -= Amount;
                    me.Update();
                    C.Update();
                    await ReplyAsync(me.Name+" gave **" + C.Name + "** $" + Amount+".");
                }
            }
            else
            {
                me.Money -= Amount;
                me.Update();
                await ReplyAsync(me.Name+" spent $" + Amount + ".");
            }
        }
        [Command("Equip"), Alias("Wear","Put-On")]
        [Summary("Put one of your items on as an equip. Usage: `$Equip <Item>`")]
        public async Task Equip(string Item)
        {
            var me = new Playerlock(Database).GetPlayer(Context.User.Id).Character;
            me.Inventory.buildInv(Database);
            me.PassInstance(Database);
            if (!me.Inventory.Items.Exists(x => x.BaseItem.Name.ToLower().StartsWith(Item.ToLower())))
            {
                await ReplyAsync(me.Name+" doesn't have this item in their inventory!");
                return;
            }
            var I = me.Inventory.Items.Find(x => x.BaseItem.Name.ToLower().StartsWith(Item.ToLower()));
            me.Equip(I.BaseItem);
            me.Update();
            await ReplyAsync(me.Name+" equiped their " + I.BaseItem.Name + ".");
        }
        [Command("Unequip"), Alias("DeEquip", "De-Equip", "re-un-de-equip")]
        [Summary("Takes off a piece of equipment. Usage: `$Unequip <Item>`")]
        public async Task Unequip(string Item)
        {
            var me = new Playerlock(Database).GetPlayer(Context.User.Id).Character;
            me.Inventory.buildInv(Database);
            me.PassInstance(Database);
            if (!me.Equipment.Exists(x => x.Name.ToLower().StartsWith(Item.ToLower())))
            {
                await ReplyAsync(me.Name+" doesn't have this item in their Equipment list!");
                return;
            }
            else
            {
                var I = me.Equipment.Find(x => x.Name.ToLower().StartsWith(Item.ToLower()));
                me.DeEquip(I);
                me.Update();
                await ReplyAsync(me.Name+" unequiped their " + I.Name + ".");
            }
        }
        [Command("Stress")]
        [Summary("Add or remove stress to a character! Use negative numbers to remove stress. Usage: `$Stress <character> <Amount>`.")]
        [RequireUserPermission(GuildPermission.ManageMessages)]
        public async Task Stress(string Name, int amount)
        {
            var col = Database.GetCollection<Character>("Characters");
            var Char = col
                .Include(x => x.Inventory.Items)
                .Include(x => x.Equipment)
                .Find(x => x.Name.StartsWith(Name.ToLower()));
            if (Char.Count() > 1 && !Char.ToList().Exists(x => x.Name.ToLower() == Name.ToLower()))
            {
                string msg = "Multiple characters where found with this search! Please specify from one of the following: \n";
                foreach (Character X in Char)
                {
                    msg += "`" + X.Name + "`, ";
                }
                var msg2 = msg.Substring(0, msg.Length - 1);
                msg2 += ".";
                await ReplyAsync(msg2);
            }
            else if (Char.Count() == 1 || Char.ToList().Exists(x => x.Name.ToLower() == Name.ToLower()))
            {
                var C = Char.First();
                C.Inventory.buildInv(Database);
                C.PassInstance(Database);
                if (C.MaxStress-C.Stress < amount || C.Stress + amount < 0)
                {
                    await ReplyAsync("This character can't get any more stressed!");
                    return;
                }
                C.Stress += amount;
                C.Update();
                await ReplyAsync("**" + C.Name + "** has gained " + amount + " stress!");
            }
        }
        [Command("Reward"), Alias("DMPay")]
        [RequireUserPermission(GuildPermission.ManageMessages)]
        [Summary("Give a player a sum of money. Usage: `$Reward <player> <Amount>`")]
        public async Task Pay(string Name, int Amount)
        {
            var col = Database.GetCollection<Character>("Characters");
            var Char = col
                .Include(x => x.Inventory.Items)
                .Include(x => x.Equipment)
                .Find(x => x.Name.StartsWith(Name.ToLower()));
            if (Char.Count() > 1 && !Char.ToList().Exists(x => x.Name.ToLower() == Name.ToLower()))
            {
                string msg = "Multiple characters where found with this search! Please specify from one of the following: \n";
                foreach (Character X in Char)
                {
                    msg += "`" + X.Name + "`, ";
                }
                var msg2 = msg.Substring(0, msg.Length - 1);
                msg2 += ".";
                await ReplyAsync(msg2);
            }
            else if (Char.Count() == 1 || Char.ToList().Exists(x => x.Name.ToLower() == Name.ToLower()))
            {
                var C = Char.First();
                C.Inventory.buildInv(Database);
                C.PassInstance(Database);
                C.Money += Amount;
                C.Update();
                await ReplyAsync("**" + C.Name + "** was rewarded with $" + Amount + "!");
            }
        }
    }
}
