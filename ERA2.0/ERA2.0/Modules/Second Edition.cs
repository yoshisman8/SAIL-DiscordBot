using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Discord;
using Discord.Commands;
using LiteDB;
using System.Threading.Tasks;
using ERA20.Modules.Classes;

namespace ERA20.Modules
{
    [Group("Character")]
    [Alias("Char","C")]
    [Summary("Perform all sorts of Second-Edition-Related Character commands, you can do `$Character <name>` to search for a character" +
        " or use `$Character <Subcommand>` to use any of the sub-commands.\nAvailable sub-commands are 'Create' to create a new character and 'Delete' to delete a character.")]
    public class Second_Edition : ModuleBase<SocketCommandContext>
    {
        public LiteDatabase Database { get; set; }

        [Command]
        public async Task Char(string Name)
        {
            var Char = new Character().GetCharacter(Name);

            if (Char == null)
            {
                await ReplyAsync("I couldn't find this player on the Database!");
            }
            else if (Char.Count() > 1)
            {
                string msg = "Multiple characters where found with this search! Please specify from one of the following: \n";
                foreach (Character X in Char)
                {
                    msg += "`" + X.Name + "`, ";
                }
                await ReplyAsync(msg);
            }
            else if (Char.Count() == 1)
            {
                await ReplyAsync("", embed: new Builders().BuildSheet(Char.First(), Context));
            }
        }
        [Command("Create")]
        [Alias("C")]
        [Summary("Creates a new character entry in the database and locks you as that character for further edits. Usage: `$Character Create <Name> <Race> <Class>`.")]
        public async Task Create(string Name, string Race, string Class)
        {
            var chars = new Character().GetCharacter(Name);

            if (chars.Count() != 0 || chars != null)
            {
                await ReplyAsync("Someone with this name already exists in the Database!");
            }
            else
            {
                var Char = new Character
                {
                    Name = Name,
                    Race = Race,
                    Class = Class
                };
                try
                {
                    Char.Add();
                    new Playerlock().Lock(Context.User.Id, Char);
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
        [Alias("D","Del")]
        [Summary("Deletes a character from the database. Usage: `$Character Delete <Name>`.")]
        public async Task Del(string Name)
        {
            var user = Context.User as IGuildUser;
            var C = new Character().GetCharacter(Name);
            var roles = user.RoleIds.Where(x => x == 324320068748181504);
            if (C == null || C.Count() == 0) { await ReplyAsync("This character doesn't exist!"); }
            else if (C.Count() > 1)
            {
                string msg = "Multiple characters where found with this search! Please specify from one of the following: \n";
                foreach (Character X in C)
                {
                    msg += "`" + X.Name + "`, ";
                }
                await ReplyAsync(msg);
            }
            else if (C.First().Owner == user.Id || roles != null)
            {
                C.First().Delete();
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
                .WithColor(new Color(0xE90FCD))
            .WithFooter(footer => {
                footer
                    .WithText(user.Nickname)
                    .WithIconUrl(user.GetAvatarUrl());
            })
            .WithDescription(player.Description)
            .WithImageUrl(player.ImageUrl)
            .WithAuthor(author => {
                author
                    .WithName(player.Name)
                    .WithUrl(player.ImageUrl);
            })
            .AddInlineField(":chart_with_upwards_trend: Basic info", ":wrench: Class: " + player.Class + "\n :bust_in_silhouette: Race: " + player.Race + "\n:heart: Stress: " + BuildStress(player) + "\n")
            .AddInlineField(":tools: Gear", Buildlist(player, 1))
            .AddField("Afflictions", Buildlist(player,5))
            .AddInlineField(":star2: Traits", Buildlist(player, 3))
            .AddInlineField(":star: Skills", Buildlist(player, 2))
            .AddField(":school_satchel: Inventory", Buildlist(player, 4));
            var embed = builder.Build();
            return embed;
        }
        private string BuildStress(Character character)
        {
            string msg = "";
            int A = character.MaxStress = character.Stress;
            
            for (int x = 0; x <= character.Stress; x++)
            {
                msg += ":red_circle: ";
            }
            for (int x = 0; x <= A; x++)
            {
                msg += ":white_circle: ";
            }
            return msg;
        }
        private string Buildlist(Character player, int arg)
        {
            string msg = "";
            switch (arg)
            {
                case 1:
                    foreach (Item x in player.Equipment)
                    {
                        msg += x.Emote + x.Name + "\n";
                    }
                    break;
                case 2:
                    foreach (Skill x in player.Skills)
                    {
                        msg += x.Emote + x.Name + " [" + ToRoman(x.Level) + "]\n";
                    }
                    break;
                case 3:
                    foreach (Trait x in player.Traits)
                    {
                        msg += x.Name + "\n";
                    }
                    break;
                case 4:
                    msg += ":moneybag: " + Math.Round(player.Money, 2) + "\n";
                    foreach (Item x in player.Inventory.Items)
                    {
                        msg += x.Emote + x.Name + "\n";
                    }
                    break;
                case 5:
                    foreach (Affliction x in player.Afflictions)
                    {
                        msg += x.Emoji + " " + x.Name + ": " + x.Description + "\n";
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
}
