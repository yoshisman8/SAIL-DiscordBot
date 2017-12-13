using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;
using Discord.Commands;
using Discord.WebSocket;
using Discord;
using Newtonsoft.Json;
using System.Linq;
using LiteDB;
using System.Net;

namespace ERA20.Modules
{
    [Name("Wiki")]
    [Summary("Commands related to the In-Server Wiki for storing lore™ and other important information.")]
    public class Wiki : ModuleBase<SocketCommandContext>
    {
        public LiteDatabase Database { get; set; }
        [Command("wikimigrate")]
        [RequireOwner]
        public async Task migrate()
        {
            try
            {
                var col = Database.GetCollection<Entry>("Wiki");
                var files = Directory.EnumerateFiles(@"Data/Wiki/");
                var olddb = new List<LEntry>();
                foreach (string x in files)
                {
                    var entry = JsonConvert.DeserializeObject<LEntry>(File.ReadAllText(x));
                    File.WriteAllText(x, entry.Body);
                    entry.Body = x;
                    olddb.Add(entry);
                }
                foreach (LEntry X in olddb)
                {
                    
                    col.Insert(new Entry()
                    {
                        Name = X.Name,
                        Body = X.Body,
                        Creator = X.Creator,
                        LastModified = X.LastModified,
                        Thumbnail = X.Thumbnail,
                        Visits = X.Visits
                    });
                    col.EnsureIndex("Name", "LOWER($.Name)");
                }
                await ReplyAsync("Migration Successful!");
            }
            catch (Exception e)
            {
                await ReplyAsync(e.Message);
            }
            
        }

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
        public async Task ShowEntry([Remainder] string _Entry)
        {
            {
                var col = Database.GetCollection<Entry>("Wiki");
                var db = col.Find(x => x.Name.StartsWith(_Entry.ToLower()));

                if (db.Count() == 0) await Context.Channel.SendMessageAsync("No entry on the wiki with that name!");
                else if (db.Count() > 1 && !db.ToList().Exists(x => x.Name == _Entry.ToLower()))
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
                    var entry = db.First();
                    await Context.Channel.SendMessageAsync("", embed: EntryBuilder(entry));
                    entry.Visits += 1;
                    col.Update(entry);
                }
            }
        }
        [Command("Wikiadd")]
        [Alias("Wadd", "Wiki-Add")]
        [RequireUserPermission(GuildPermission.ManageMessages)]
        [Summary("Adds a wiki entry. Usage: `$Wikiadd <Name> <Thumbnail URL (leave as empty if non applicable or just updating an article)>`. Make sure to use this command while sending a .txt file with the contents of the article.\n" +
            "For help about wiki article formatting, use `$Wiki Formatting` to see how it works.")]
        public async Task AddEntry(string _Name, [Remainder] string _ImageURL = "")
        {
            {
                var col = Database.GetCollection<Entry>("Wiki");

                if(Context.Message.Attachments.Count() == 0)
                {
                    await ReplyAsync("You have to attach the article as a .txt file (this is to go around Discord's character limit and to allow you to add fields into the article.).");
                    return;
                }
                if (col.Exists(x => x.Name == (_Name.ToLower())))
                {
                    var entry = col.FindOne(x => x.Name.StartsWith(_Name.ToLower()));
                    entry.Body = Context.Message.Attachments.FirstOrDefault().Url;
                    if (_ImageURL != "")
                    {
                        entry.Thumbnail = _ImageURL;
                    }
                    col.EnsureIndex("Name", "LOWER($.Name)");
                    col.Update(entry);
                    await Context.Channel.SendMessageAsync("Entry for **" + entry.Name + "** edited successfully!");
                }
                else
                {
                    var entry = new Entry()
                    {
                        Name = _Name,
                        Creator = Context.User.Id,
                        LastModified = DateTime.Now,
                        Body = Context.Message.Attachments.FirstOrDefault().Url
                    };
                    if (_ImageURL != "")
                    {
                        entry.Thumbnail = _ImageURL;
                    }
                    col.EnsureIndex("Name", "LOWER($.Name)");
                    col.Insert(entry);
                    await Context.Channel.SendMessageAsync("Entry for **" + _Name + "** added successfully!");
                }
                
            }
        }
        [Command("Wikidelete")]
        [Alias("Wdel", "Wiki-Del")]
        [Summary("Deletes a wiki entry. Usage: `$Wikidelete <name>`")]
        [RequireUserPermission(GuildPermission.ManageMessages)]
        public async Task delete([Remainder] string _Entry)
        {
            {
                Directory.CreateDirectory(@"Data/Wiki/");
                var db = Database.GetCollection<Entry>("Wiki");
                if (!db.Exists(x => x.Name.StartsWith(_Entry.ToLower())))
                {
                    await Context.Channel.SendMessageAsync("No entry on the wiki with that name!");
                }
                else if (db.Count() > 1 && !db.Exists(x => x.Name == _Entry.ToLower()))
                {
                    string msg = "Multiple entries found! Please specify which one of the following is the correct one: ";
                    foreach (Entry X in db.Find(x => x.Name.StartsWith(_Entry.ToLower())))
                    {
                        msg += "`" + X.Name + "` ";
                    }
                    await Context.Channel.SendMessageAsync(msg);
                }
                else
                {
                    var entry = db.FindOne(x => x.Name.StartsWith(_Entry.ToLower()));
                    db.Delete(x => x.Name.StartsWith(_Entry.ToLower()));
                    await ReplyAsync("Wiki article **"+entry.Name+"** deleted from the database!");
                }
            }
        }
        
        public Embed EntryBuilder(Entry _Entry)
        {
            IUser User = Context.Guild.GetUser(_Entry.Creator);
            var builder = new EmbedBuilder()
    .WithTitle(_Entry.Name)
    .WithColor(new Color(0xffffff))
    .WithTimestamp(_Entry.LastModified)
    .WithFooter(footer =>
    {
        footer
            .WithText("Last edit on ");
    })
    .WithThumbnailUrl(_Entry.Thumbnail)
    .WithUrl(_Entry.Thumbnail)
    .WithAuthor(author =>
    {
        author
            .WithName("Made by: " + User.Username)
            .WithIconUrl(User.GetAvatarUrl());
    });
            using (var client = new WebClient())
            {
                Directory.CreateDirectory(@"Data/Temp/");
                client.DownloadFile(_Entry.Body, @"Data/Temp/" + _Entry.Name + ".txt");
            }
            using (var reader = new StreamReader(@"Data/Temp/" + _Entry.Name + ".txt"))
            {
                string line;
                string body = "";
                string header = "";
                string content = "";
                bool toggle = false;
                while ((line = reader.ReadLine()) != null)
                {
                    if (line.StartsWith('|') || toggle)
                    {
                        if (line.StartsWith('|'))
                        {
                            header = line.Remove(0,1);
                            toggle = true;
                        }
                        else
                        {
                            content += line + "\n";
                        }
                        if (line.EndsWith('|'))
                        {
                            builder.AddField(header, content.Remove(content.Length - 2));
                            header = "";
                            content = "";
                        }
                    }
                    else
                    {
                        body += line + "\n";
                    }
                    
                }
                builder.Description = body.Remove(body.Length -1);
            }
            var embed = builder.Build();
            return embed;
        }
        public Embed FrontpageBuilder()
        {
            var db = Database.GetCollection<Entry>("Wiki");
            var builder = new EmbedBuilder()
                .WithTitle("Dragon's Den Personal Wikipedia")
                .WithDescription("This is the Wiki Frontpage! So far we have a total of " + db.Count() + " entries on the wiki!\n" +
                "If you're looking for something specific, use `$Wiki <entry>` to look for it!")
                .WithAuthor(Context.User)
                .WithCurrentTimestamp()
                .WithColor(new Color(255, 255, 255));
            var dbview = db.FindAll().OrderByDescending(e => e.Visits);
            string t5 = "";
            for (int x = 0; x < 5; x++)
            {
                t5 += "• " + dbview.ToList()[x].Name + "\n";
            }
            builder.AddInlineField(":chart_with_upwards_trend: Most Visited Entries", t5);
            var wikirecent = db.FindAll().OrderByDescending(e => e.LastModified);
            t5 = "";
            for (int x = 0; x < 5; x++)
            {
                t5 += "• " + wikirecent.ToList()[x].Name + "\n";
            }
            builder.AddInlineField(":clock4: Last modified articles", t5);
            var embed = builder.Build();
            return embed;
        }
    }

    public class Entry
    {
        public int EntryId { get; set; }
        public ulong Creator { get; set; }
        public int Visits { get; set; } = 0;
        public string Name { get; set; }
        public string Body { get; set; }
        public string Thumbnail { get; set; } = "https://imageog.flaticon.com/icons/png/512/48/48930.png?size=1200x630f&pad=10,10,10,10&ext=png&bg=FFFFFFFF";
        public DateTime LastModified { get; set; }
    }
    public class LEntry
    {
        public ulong Creator { get; set; }
        public int Visits { get; set; } = 0;
        public string Name { get; set; }
        public string Body { get; set; }
        public string Thumbnail { get; set; } = "https://imageog.flaticon.com/icons/png/512/48/48930.png?size=1200x630f&pad=10,10,10,10&ext=png&bg=FFFFFFFF";
        public DateTime LastModified { get; set; }
    }
}
