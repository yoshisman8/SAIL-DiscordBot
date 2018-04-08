using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using ERA20.Modules.Classes;
using Discord.Commands;
using Discord;
using LiteDB;
using System.Threading.Tasks;

namespace ERA20.Modules
{
    [Group("Party")]
    public class PartyMGR : ModuleBase<SocketCommandContext>
    {
        public LiteDatabase Database { get; set; }
        [Command]
        [RequireContext(ContextType.Guild)]
        public async Task get(string name)
        {
            var col = Database.GetCollection<Party>("Party");
            if (!col.Exists(x => x.Name.StartsWith(name.ToLower())))
            {
                await ReplyAsync("There isn't any party whose name starts with '" + name + "'.");
                return;
            }
            var result = col
                .Include(x => x.Characters)
                .Find(x => x.Name.StartsWith(name.ToLower())).OrderBy(x => x.Name);
            if (result.Count() > 1 && !col.Exists(x => x.Name == name.ToLower()))
            {
                string msg = "Multiple Parties where found with this search! Please specify from one of the following: \n";
                foreach (Party X in result)
                {
                    msg += "`" + X.Name + "`, ";
                }
                var msg2 = msg.Substring(0, msg.Length - 2);
                msg2 += ".";
                await ReplyAsync(msg2);
            }
            else
            {
                var P = result.First();

                var builder = new EmbedBuilder()
                    .WithAuthor(Context.Client.CurrentUser)
                    .WithTitle(P.Name)
                    .WithUrl(P.MapUrl)
                    .WithThumbnailUrl(P.MapUrl)
                    .WithDescription(P.Summary);
                foreach (Character x in P.Characters)
                {
                    builder.AddField(x.Name + " (Played by: " + GetUser(x.Owner).Username+")", new Search().StringCutter(x.Description, 100) + "(...)\nStress: "+new Builders().BuildStress(x) +"\n" + new Builders().BuildAfflictions(x));
                }
                await ReplyAsync("", embed: builder.Build());
            }
        }
        [Command("Create")]
        [RequireUserPermission(ChannelPermission.ManageMessages)]
        public async Task Make(string Name)
        {
            var col = Database.GetCollection<Party>("Party");

            if (col.Exists(x => x.Name == Name.ToLower()))
            {
                await ReplyAsync("There's already a party with this name!");
                return;
            }
            else
            {
                var p = new Party() { Name = Name };
                col.EnsureIndex("Name", "LOWER($.Name)");
                col.Insert(p);
                await ReplyAsync("Party **" + Name + "** Created! Add some characters to it by doing `/Party Add <Party> <character>`!");
            }
        }
        [Command("Delete")]
        [RequireUserPermission(ChannelPermission.ManageMessages)]
        public async Task delete (string name)
        {
            var col = Database.GetCollection<Party>("Party");
            if (!col.Exists(x => x.Name.StartsWith(name.ToLower())))
            {
                await ReplyAsync("There isn't any party whose name starts with '" + name + "'.");
                return;
            }
            var result = col
                .Include(x => x.Characters)
                .Find(x => x.Name.StartsWith(name.ToLower())).OrderBy(x => x.Name);
            if (result.Count() > 1 && !col.Exists(x => x.Name == name.ToLower()))
            {
                string msg = "Multiple Parties where found with this search! Please specify from one of the following: \n";
                foreach (Party X in result)
                {
                    msg += "`" + X.Name + "`, ";
                }
                var msg2 = msg.Substring(0, msg.Length - 2);
                msg2 += ".";
                await ReplyAsync(msg2);
            }
            else
            {
                var P = result.First();

                col.Delete(x => x.PartyId == P.PartyId);

                await ReplyAsync(P.Name + " deleted from the Database!");
            }
        }
        [Command("Add")]
        [RequireUserPermission(ChannelPermission.ManageMessages)]
        public async Task Add(string party, string Name)
        {
            var col1 = Database.GetCollection<Party>("Party");
            if (!col1.Exists(x => x.Name.StartsWith(party.ToLower())))
            {
                await ReplyAsync("There isn't any party whose name starts with '" + party + "'.");
                return;
            }
            var result = col1
                .Include(x => x.Characters)
                .Find(x => x.Name.StartsWith(party.ToLower())).OrderBy(x => x.Name);
            if (result.Count() > 1 && !col1.Exists(x => x.Name == party.ToLower()))
            {
                string msg = "Multiple Parties where found with this search! Please specify from one of the following: \n";
                foreach (Party X in result)
                {
                    msg += "`" + X.Name + "`, ";
                }
                var msg2 = msg.Substring(0, msg.Length - 2);
                msg2 += ".";
                await ReplyAsync(msg2);
            }
            else
            {
                var p = result.First();
                var col = Database.GetCollection<Character>("Characters");
                if (!col.Exists(x => x.Name.StartsWith(Name.ToLower())))
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
                    var msg2 = msg.Substring(0, msg.Length - 2);
                    msg2 += ".";
                    await ReplyAsync(msg2);
                }
                else if (Char.Count() == 1 || Char.ToList().Exists(x => x.Name.ToLower() == Name.ToLower()))
                {
                    var C = Char.First();
                    if (p.Characters.Contains(C))
                    {
                        await ReplyAsync(C.Name+" is already part of "+p.Name+"!");
                        return;
                    }
                    else
                    {
                        p.Characters.Add(C);
                        col1.Update(p);
                        await ReplyAsync(C.Name+" is now part of "+p.Name+"!");
                    }
                }
            }
        }
        [Command("Remove"), Alias("Rem")]
        [RequireUserPermission(ChannelPermission.ManageMessages)]
        public async Task rem(string party, string Name)
        {
            var col1 = Database.GetCollection<Party>("Party");
            if (!col1.Exists(x => x.Name.StartsWith(party.ToLower())))
            {
                await ReplyAsync("There isn't any party whose name starts with '" + party + "'.");
                return;
            }
            var result = col1
                .Include(x => x.Characters)
                .Find(x => x.Name.StartsWith(party.ToLower())).OrderBy(x => x.Name);
            if (result.Count() > 1 && !col1.Exists(x => x.Name == party.ToLower()))
            {
                string msg = "Multiple Parties where found with this search! Please specify from one of the following: \n";
                foreach (Party X in result)
                {
                    msg += "`" + X.Name + "`, ";
                }
                var msg2 = msg.Substring(0, msg.Length - 2);
                msg2 += ".";
                await ReplyAsync(msg2);
            }
            else
            {
                var p = result.First();
                if (!p.Characters.Exists(x => x.Name.ToLower().StartsWith(Name.ToLower())))
                {
                    await ReplyAsync("There isn't a character on " + p.Name + " whose name starts with '" + Name + "'.");
                }
                else if (p.Characters.FindAll(x => x.Name.ToLower().StartsWith(Name.ToLower())).Count() > 1 && p.Characters.Exists(x => x.Name.ToLower() == Name.ToLower()))
                {
                    string msg = "Multiple characters where found with this search! Please specify from one of the following: \n";
                    foreach (Character X in p.Characters.FindAll(x => x.Name.ToLower().StartsWith(Name.ToLower())))
                    {
                        msg += "`" + X.Name + "`, ";
                    }
                    var msg2 = msg.Substring(0, msg.Length - 2);
                    msg2 += ".";
                    await ReplyAsync(msg2);
                }
                else
                {
                    var c = p.Characters.Find(x => x.Name.ToLower().StartsWith(Name.ToLower()));
                    p.Characters.Remove(c);
                    col1.Update(p);
                    await ReplyAsync(c.Name + " is no longer part of " + p.Name);
                }
            }
        }
        [Command("Summary")]
        [RequireUserPermission(ChannelPermission.ManageMessages)]
        public async Task summary(string name, string Value)
        {
            var col = Database.GetCollection<Party>("Party");
            if (!col.Exists(x => x.Name.StartsWith(name.ToLower())))
            {
                await ReplyAsync("There isn't any party whose name starts with '" + name + "'.");
                return;
            }
            var result = col
                .Include(x => x.Characters)
                .Find(x => x.Name.StartsWith(name.ToLower())).OrderBy(x => x.Name);
            if (result.Count() > 1 && !col.Exists(x => x.Name == name.ToLower()))
            {
                string msg = "Multiple Parties where found with this search! Please specify from one of the following: \n";
                foreach (Party X in result)
                {
                    msg += "`" + X.Name + "`, ";
                }
                var msg2 = msg.Substring(0, msg.Length - 2);
                msg2 += ".";
                await ReplyAsync(msg2);
            }
            else
            {
                var P = result.First();

                P.Summary = Value;
                col.Update(P);
                await ReplyAsync(P.Name + "'s Summary updated!");
            }
        }
        [Command("Map"), Alias("Image")]
        [RequireUserPermission(ChannelPermission.ManageMessages)]
        public async Task map(string name, string Value)
        {
            var col = Database.GetCollection<Party>("Party");
            if (!col.Exists(x => x.Name.StartsWith(name.ToLower())))
            {
                await ReplyAsync("There isn't any party whose name starts with '" + name + "'.");
                return;
            }
            var result = col
                .Include(x => x.Characters)
                .Find(x => x.Name.StartsWith(name.ToLower())).OrderBy(x => x.Name);
            if (result.Count() > 1 && !col.Exists(x => x.Name == name.ToLower()))
            {
                string msg = "Multiple Parties where found with this search! Please specify from one of the following: \n";
                foreach (Party X in result)
                {
                    msg += "`" + X.Name + "`, ";
                }
                var msg2 = msg.Substring(0, msg.Length - 2);
                msg2 += ".";
                await ReplyAsync(msg2);
            }
            else
            {
                var P = result.First();

                P.MapUrl = Value;
                col.Update(P);
                await ReplyAsync(P.Name + "'s Map Image updated!");
            }
        }
        public IGuildUser GetUser(ulong Id)
        {
            IGuildUser User = Context.Guild.GetUser(Id);
            return User;
        }
    }
}
