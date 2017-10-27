using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Discord.Commands;
using Discord.WebSocket;
using Discord;
using Newtonsoft.Json;
using System.Linq;

namespace ERA20.Modules
{
    [Name("Wiki")]
    [Summary("Commands related to the In-Server Wiki for storing lore™ and other important information.")]
    public class Wiki: ModuleBase<SocketCommandContext>
    {
        [Command("Wiki")]
        [Alias("W")]
        [Summary("Search an entry on the wiki. Usage: `$wiki <entry name>`")]
        public async Task Frontpage()
        {
            await Context.Channel.SendMessageAsync("", embed: FrontpageBuilder());
        }
        [Command("Wiki")]
        [Alias("w")]
        [Summary("Search an entry on the wiki. Usage: `$wiki <entry name>`")]
        public async Task ShowEntry(string _Entry)
        {
            {
                Directory.CreateDirectory(@"Data/Wiki/");
                var db = new WikiDb().Query(_Entry);
                if (db == null) await Context.Channel.SendMessageAsync("No entry on the wiki with that name!");
                else if (db.Count() > 1 && db.First().Name.ToLower() != _Entry.ToLower())
                {
                    string msg = "Multiple entries found! Please specify which one of the following is the correct one: ";
                    foreach (Entry X in db)
                    {
                        msg += "`" + X.Name + "` ";
                    }
                    await Context.Channel.SendMessageAsync(msg);
                }
                else
                {
                    await Context.Channel.SendMessageAsync("", embed: EntryBuilder(db.First()));

                    db.First().Visits += 1;

                    string json = JsonConvert.SerializeObject(db.First());
                    Directory.CreateDirectory(@"Data/Wiki/");
                    File.WriteAllText(@"Data/Wiki/" + db.First().Name + ".json", json);
                }
            }
        }
        [Command("Wikiadd")]
        [Alias("Wadd","Wiki-Add")]
        [RequireUserPermission(GuildPermission.ManageMessages)]
        [Summary("Adds a wiki entry. Usage: `$Wikiadd <entry> <Thumbnail URL (leave as `N/A` if non applicable)> <Entry Body>")]
        public async Task AddEntry(string _Name, string _Thumbnail, [Remainder] string _Body)
        {
            {
                var User = Context.User as SocketGuildUser;
                IRole Admins = Context.Guild.GetRole(311989788540665857);
                IRole trialadmin = Context.Guild.GetRole(364633182357815298);
                IRole DMs = Context.Guild.GetRole(324320068748181504);
                if (User.Roles.Contains(Admins) == true || User.Roles.Contains(trialadmin) == true || User.Roles.Contains(DMs) == true)
                {
                    var Entry = new Entry()
                    {
                        Name = _Name,
                        Body = _Body,
                        Creator = Context.User.Id,
                        Thumbnail = _Thumbnail,
                        LastModified = DateTime.Now
                    };
                    string json = JsonConvert.SerializeObject(Entry);
                    Directory.CreateDirectory(@"Data/Wiki/");
                    File.WriteAllText(@"Data/Wiki/" + _Name + ".json", json);
                    await Context.Channel.SendMessageAsync("Entry for **" + _Name + "** added or edited successfully!");
                }
                else
                {
                    await Context.Channel.SendMessageAsync("`You cannot use this Command!`");
                }
            }
        }
        [Command("Wikidelete")]
        [Alias("Wdel","Wiki-Del")]
        [Summary("Deletes a wiki entry. Usage: `$Wikidelete <name>`")]
        [RequireUserPermission(GuildPermission.ManageMessages)]
        public async Task delete(string _Entry)
        {
            {
                Directory.CreateDirectory(@"Data/Wiki/");
                var db = new WikiDb().Query(_Entry);
                if (db == null)
                {
                    await Context.Channel.SendMessageAsync("No entry on the wiki with that name!");
                }
                else if (db.Count() > 1 && db.First().Name.ToLower() != _Entry.ToLower())
                {
                    string msg = "Multiple entries found! Please specify which one of the following is the correct one: ";
                    foreach (Entry X in db)
                    {
                        msg += "`" + X.Name + "` ";
                    }
                    await Context.Channel.SendMessageAsync(msg);
                }
                else
                {
                    var User = Context.User as SocketGuildUser;
                    IRole Admins = Context.Guild.GetRole(311989788540665857);
                    IRole trialadmin = Context.Guild.GetRole(364633182357815298);
                    IRole DMs = Context.Guild.GetRole(324320068748181504);
                    if (User.Roles.Contains(Admins) == true || User.Roles.Contains(trialadmin) == true || User.Roles.Contains(DMs) == true)
                    {
                        File.Delete(@"Data/Wiki/" + _Entry + ".json");
                        await Context.Channel.SendMessageAsync("Entry **" + _Entry + "** Deleted successfully!");
                    }
                }
            }
        }
        public Embed EntryBuilder(Entry _Entry)
        {
            IUser User = Context.Guild.GetUser(_Entry.Creator);
            var builder = new EmbedBuilder()
    .WithTitle(_Entry.Name)
    .WithDescription(_Entry.Body)
    .WithColor(new Color(0xffffff))
    .WithTimestamp(_Entry.LastModified)
    .WithFooter(footer => {
        footer
            .WithText("Last edit on ");
    })
	.WithImageUrl(_Entry.Thumbnail)
    .WithAuthor(author => {
        author
            .WithName("Made by: "+User.Username)
            .WithIconUrl(User.GetAvatarUrl());
    });
            var embed = builder.Build();
            return embed;
        }
        public Embed FrontpageBuilder()
        {
            var db = new WikiDb().LoadWiki();
            var builder = new EmbedBuilder()
                .WithTitle("Dragon's Den Personal Wikipedia")
                .WithDescription("This is the Wiki Frontpage! So far we have a total of " + db.Wiki.Count() + " entries on the wiki!\n" +
                "If you're looking for something specific, use `$Wiki <entry>` to look for it!")
                .WithAuthor(Context.User)
                .WithCurrentTimestamp()
                .WithColor(new Color(255, 255, 255));
            db.Wiki.OrderBy(e => e.Visits);
            string t5 = "";
            for (int x = 0; x < 5; x++)
            {
                t5 += "• " + db.Wiki[x]+"/n";
            }
            builder.AddInlineField(":chart_with_upwards_trend: Most Visited Entries", t5);
            db.Wiki.OrderBy(e => e.LastModified);
            t5 = "";
            for (int x = 0; x < 5; x++)
            {
                t5 += "• " + db.Wiki[x] + "/n";
            }
            builder.AddInlineField(":clock4: Last modified articles", t5);
            var embed = builder.Build();
            return embed;
        }
    }
    public class WikiDb
    {
        public List<Entry> Wiki { get; set; } = new List<Entry> { };

        public WikiDb LoadWiki()
        {
            var db = new WikiDb();
            Directory.CreateDirectory(@"Data/Wiki/");
            var folder = Directory.EnumerateFiles(@"Data/Wiki/");
            foreach (string x in folder)
            {
                db.Wiki.Add(JsonConvert.DeserializeObject<Entry>(File.ReadAllText(x)));
            }

            return db;
        }
        public List<Entry> Query(string _Name)
        {
            var db = this.LoadWiki();
            var Query = db.Wiki.Where(x => x.Name.ToLower().Contains(_Name.ToLower()));
            return Query.OrderBy(x => x.Name) as List<Entry>;
        }
    }
    public class Entry
    {
        public ulong Creator { get; set; }
        public int Visits { get; set; } = 0;
        public string Name { get; set; }
        public string Body { get; set; }
        public string Thumbnail { get; set; }
        public DateTime LastModified { get; set; }
    }
}
