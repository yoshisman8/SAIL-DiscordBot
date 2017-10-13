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
    public class Wiki: ModuleBase<SocketCommandContext>
    {
        [Command("wiki")]
        public async Task ShowEntry(string _Entry = "$$%%^&")
        {
            Directory.CreateDirectory(@"Data/Wiki/");
            var db = new WikiDb().LoadWiki();
            var Query = db.Wiki.Where(x => x.Name.Contains(_Entry));
            if (Query != null)
            {
                await Context.Channel.SendMessageAsync("", embed: EntryBuilder(Query.First()));
            }
            else
            {
                await Context.Channel.SendMessageAsync("No entry on the wiki with that name was found or no entry was searched in the first place.");
            }
        }
        [Command("wikiadd")]
        public async Task AddEntry(string _Name = "", string _Thumbnail = "", [Remainder] string _Body = "")
        {
            if (_Name == "" || _Body == "")
            {
                await Context.Channel.SendMessageAsync("Incorrect command ussage! Correct usage is `$Wikiadd <name> <Thumbnail URL (leave as N/A if non applicable)> <Entry Body>`.");
            }
            else
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
                    await Context.Channel.SendMessageAsync("Entry for " + _Name + " Added successfully!");
                }
                else
                {
                    await Context.Channel.SendMessageAsync("`You cannot use this Command!`");
                }
            }
        }
        [Command("wikiedit")]
        public async Task EditEntry(string _Name = "", [Remainder] string _Body = "")
        {
            if (_Name == "" || _Body == "")
            {
                await Context.Channel.SendMessageAsync("Incorrect command ussage! Correct usage is `$Wikiedit <name> <Entry Body>`.");
            }
            else
            {
                var User = Context.User as SocketGuildUser;
                IRole Admins = Context.Guild.GetRole(311989788540665857);
                IRole trialadmin = Context.Guild.GetRole(364633182357815298);
                IRole DMs = Context.Guild.GetRole(324320068748181504);
                if (User.Roles.Contains(Admins) == true || User.Roles.Contains(trialadmin) == true || User.Roles.Contains(DMs) == true)
                {
                    var db = new WikiDb().LoadWiki();
                    var Query = db.Wiki.Where(x => x.Name.Contains(_Name));
                    if (Query != null)
                    {
                        Entry Entry = Query.First();
                        Entry.Body = _Body;
                        Entry.LastModified = DateTime.Now;
                        string json = JsonConvert.SerializeObject(Entry);
                        Directory.CreateDirectory(@"Data/Wiki/");
                        File.WriteAllText(@"Data/Wiki/" + _Name + ".json", json);
                        await Context.Channel.SendMessageAsync("Entry for " + _Name + " Updated successfully!");
                    }
                    else
                    {
                        await Context.Channel.SendMessageAsync("No entry on the wiki with that name was found or no entry was searched in the first place.");
                    }
                }
                else
                {
                    await Context.Channel.SendMessageAsync("`You cannot use this Command!`");
                }
            }
        }
        [Command("wikipic")]
        public async Task EditPic(string _Name = "", [Remainder] string thumbnail = "")
        {
            if (_Name == "" || thumbnail == "")
            {
                await Context.Channel.SendMessageAsync("Incorrect command ussage! Correct usage is `$Wikipic <name> <Thumbnail URL (leave as N/A if non applicable)>`.");
            }
            else
            {
                var User = Context.User as SocketGuildUser;
                IRole Admins = Context.Guild.GetRole(311989788540665857);
                IRole trialadmin = Context.Guild.GetRole(364633182357815298);
                IRole DMs = Context.Guild.GetRole(324320068748181504);
                if (User.Roles.Contains(Admins) == true || User.Roles.Contains(trialadmin) == true || User.Roles.Contains(DMs) == true)
                {
                    var db = new WikiDb().LoadWiki();
                    var Query = db.Wiki.Where(x => x.Name.Contains(_Name));
                    if (Query != null)
                    {
                        Entry Entry = Query.First();
                        Entry.Thumbnail = thumbnail;
                        Entry.LastModified = DateTime.Now;
                        string json = JsonConvert.SerializeObject(Entry);
                        Directory.CreateDirectory(@"Data/Wiki/");
                        File.WriteAllText(@"Data/Wiki/" + _Name + ".json", json);
                        await Context.Channel.SendMessageAsync("Entry for " + _Name + " Updated successfully!");
                    }
                    else
                    {
                        await Context.Channel.SendMessageAsync("No entry on the wiki with that name was found or no entry was searched in the first place.");
                    }
                }
                else
                {
                    await Context.Channel.SendMessageAsync("`You cannot use this Command!`");
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
            .WithText("Last edit on: ");
    })
	.WithThumbnailUrl(_Entry.Thumbnail)
    .WithAuthor(author => {
        author
            .WithName("Made by: "+User.Username)
            .WithIconUrl(User.GetAvatarUrl());
    });
            var embed = builder.Build();
            return embed;
        }
    }
    public class WikiDb
    {
        public List<Entry> Wiki { get; set; }

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
    }
    public class Entry
    {
        public ulong Creator { get; set; }
        public string Name { get; set; }
        public string Body { get; set; }
        public string Thumbnail { get; set; }
        public DateTime LastModified { get; set; }
    }
}
