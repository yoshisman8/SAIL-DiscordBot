﻿using System;
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
        [Command("wiki")]
        [Alias("w")]
        [Summary("Search an entry on the wiki. Usage: `$wiki <entry name>`")]
        public async Task ShowEntry(string _Entry)
        {
            {
                Directory.CreateDirectory(@"Data/Wiki/");
                var db = new WikiDb().Query(_Entry);
                if (db == null) await Context.Channel.SendMessageAsync("No entry on the wiki with that name!");
                else
                {
                    await Context.Channel.SendMessageAsync("", embed: EntryBuilder(db.First()));
                }
            }
        }
        [Command("wikiadd")]
        [Alias("wadd")]
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
        [Command("wikidelete")]
        [Alias("wdel")]
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
        public string Name { get; set; }
        public string Body { get; set; }
        public string Thumbnail { get; set; }
        public DateTime LastModified { get; set; }
    }
}
