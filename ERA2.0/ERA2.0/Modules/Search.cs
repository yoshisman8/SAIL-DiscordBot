using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Discord.Commands;
using System.Threading.Tasks;
using LiteDB;
using Newtonsoft.Json;
using ERA20.Modules.Classes;
using Discord;
using System.Net;

namespace ERA20.Modules
{
    [Group("Search")]
    public class Search : ModuleBase<SocketCommandContext>
    {
        public LiteDatabase Database { get; set; }
         [Command]
         public async Task Default()
        {
            await ReplyAsync("This is the search command! You can use it to search the bot's database for plenty of things!\n" +
                "You can use this command by doing `$Search <Category> <Query>` (No need to wrap your query around \"s) where the catergory can be any of the following:\n\n" +
                "- 'Characters', 'char' or 'chars' to search for characters (both 2e and Legacy ones).\n" +
                "- 'Players', 'users' 'made-by' or 'madeby' to search characters made by a specific user/person (both 2e and Legacy ones).\n" +
                "- 'Wiki', 'Entries' or 'articles' to search for Articles on the bot's internal Wiki.\n" +
                "- 'Items' or 'Item' To search on the Item's database.\n" +
                "- 'Storages' or 'Storage' To search for storages.");
        }
        [Command("Characters"), Alias("Character", "Chars","Char")]
        public async Task Chars([Remainder] string Query)
        {
            Directory.CreateDirectory(@"Data/Legacy/");
            var files = Directory.EnumerateFiles(@"Data/Legacy/");
            List<LegacyCharacter> db = new List<LegacyCharacter> { };
            foreach (string x in files)
            {
                db.Add(JsonConvert.DeserializeObject<LegacyCharacter>(File.ReadAllText(x)));
            }
            var LChars = db.Where(x => x.Name.ToLower().Contains(Query.ToLower())).OrderBy(x => x.Name);
            var col = Database.GetCollection<Character>("Characters");
            var NChars = col.Find(x => x.Name.Contains(Query.ToLower()));

            if ( LChars.Count() == 0 && NChars.Count() == 0)
            {
                await ReplyAsync("There are no characters whose names contain '" + Query + "'.");
                return;
            }
            if ((LChars.Count() + NChars.Count()) > 24)
            {
                await ReplyAsync("This search brought up too many results (Over 25)! Please be more specific in your search!");
                return;
            }
            else
            {
                var builder = new EmbedBuilder()
                    .WithAuthor("E.R.A. Database Search", Context.Client.CurrentUser.GetAvatarUrl())
                    .WithDescription("Here are some results for your search:")
                    .WithCurrentTimestamp();
                foreach (LegacyCharacter X in LChars)
                {
                    builder.AddField("[LEGACY] "+X.Name, "Made by: " + X.Owner + "\nLecagy characters Don't Have descriptions.");
                }
                foreach(Character X in NChars)
                {
                    builder.AddField(X.Name, X.Description + "\nMade by: " + Context.Guild.GetUser(X.Owner).Username);
                }
                await ReplyAsync("", embed: builder.Build());
            }
        }
        [Command("Items"), Alias("Item")]
        public async Task Items([Remainder]string Query)
        {
            var col = Database.GetCollection<BaseItem>("Items");
            var results = col.Find(x => x.Name.Contains(Query.ToLower()));
            if (results.Count() == 0)
            {
                await ReplyAsync("There are no items whose names contain '" + Query + "'.");
                return;
            }
            if (results.Count() > 24)
            {
                await ReplyAsync("This search brought up too many results (Over 25)! Please be more specific in your search!");
                return;
            }
            else
            {
                var builder = new EmbedBuilder()
                    .WithAuthor("E.R.A. Database Search", Context.Client.CurrentUser.GetAvatarUrl())
                    .WithDescription("Here are some results for your search:")
                    .WithCurrentTimestamp();
                foreach(BaseItem X in results)
                {
                    builder.AddField(X.Name, X.Description);
                }
                await ReplyAsync("", embed: builder.Build());
            }
        }
        [Command("Players"), Alias("Made-By","Madeby","users")]
        public async Task users([Remainder]string Name)
        {
            var col = Database.GetCollection<Character>("Characters");
            Directory.CreateDirectory(@"Data/Legacy/");
            var files = Directory.EnumerateFiles(@"Data/Legacy/");
            List<LegacyCharacter> db = new List<LegacyCharacter> { };
            foreach (string x in files)
            {
                db.Add(JsonConvert.DeserializeObject<LegacyCharacter>(File.ReadAllText(x)));
            }
            if (GetUserAlt(Name) == null)
            {
                await ReplyAsync("There's no user on this server whose username (not nickname) starts with '" + Name + "'.");
                return;
            }
            var LChars = db.Where(x => x.Owner.Split("#").Last() == GetUserAlt(Name).Discriminator);
            var NChar = col.Find(x => Context.Guild.GetUser(x.Owner) == GetUserAlt(Name));
            if (LChars.Count() == 0 && NChar.Count() == 0)
            {
                await ReplyAsync("There are no characters whose names contain '" + Name + "'.");
                return;
            }
            if ((LChars.Count() + NChar.Count()) > 24)
            {
                await ReplyAsync("This search brought up too many results (Over 25)! Please be more specific in your search!");
                return;
            }
            else
            {
                var builder = new EmbedBuilder()
                    .WithAuthor("E.R.A. Database Search", Context.Client.CurrentUser.GetAvatarUrl())
                    .WithDescription("Here are all characters made by "+Context.Guild.GetUser(GetUserAlt(Name).Id)+":")
                    .WithCurrentTimestamp();
                foreach (LegacyCharacter X in LChars)
                {
                    builder.AddField("[LEGACY] " + X.Name, "Made by: " + X.Owner + "\nLecagy characters Don't Have descriptions.");
                }
                foreach (Character X in NChar)
                {
                    builder.AddField(X.Name, X.Description + "\nMade by: " + Context.Guild.GetUser(X.Owner).Username);
                }
                await ReplyAsync("", embed: builder.Build());
            }

        }
        [Command("Wiki"),Alias("Articles", "Entries","Entry","Article")]
        public async Task Wiki([Remainder]string Query)
        {
            var db = Database.GetCollection<Entry>("Wiki");
            var Result = db.Find(x => x.Name.Contains(Query.ToLower()));
            var builder = new EmbedBuilder()
                    .WithAuthor("E.R.A. Database Search", Context.Client.CurrentUser.GetAvatarUrl())
                    .WithDescription("Here are some results for your search:")
                    .WithCurrentTimestamp();
            foreach(Entry x in Result)
            {
                builder.AddField(x.Name, StringCutter(buildart(x),200)+"(...)");
            }
            await ReplyAsync("", embed: builder.Build());
        }
        [Command("Storages"), Alias("Storage")]
        public async Task Storage ([Remainder]string Query)
        {
            var col = Database.GetCollection<Storage>("Storages");
            var results = col.Find(x => x.Name.Contains(Query.ToLower()));

            if (results.Count() == 0)
            {
                await ReplyAsync("There are no storages whose names contain '" + Query + "'.");
                return;
            }
            if (results.Count() > 24)
            {
                await ReplyAsync("This search brought up too many results (Over 25)! Please be more specific in your search!");
                return;
            }
            else
            {
                var builder = new EmbedBuilder()
                    .WithAuthor("E.R.A. Database Search", Context.Client.CurrentUser.GetAvatarUrl())
                    .WithDescription("Here are some results for your search:")
                    .WithCurrentTimestamp();
                foreach (Storage x in results)
                {
                    builder.AddField(x.Name, StringCutter(x.Description, 100)+"(...)");
                }
                await ReplyAsync("", embed: builder.Build());
            }
        }
        public IUser GetUser([Remainder]string name)
        {
            string ID = name.Split("#").Last();
            var user = Context.Guild.Users.Where(x => x.Username.Contains(ID));
            if (user.Count() == 0) { return null; }
            else { return user.First() as IUser; }
        }
        public IUser GetUserAlt([Remainder]string name)
        {
            var user = Context.Guild.Users.Where(x => x.Username.ToLower().Contains(name.ToLower()));
            if (user.Count() == 0) { return null; }
            else { return user.First() as IUser; }
        }

        public string StringCutter (string str, int maxLength)
        {
            return str.Substring(0, Math.Min(str.Length, maxLength));
        }
        private string buildart(Entry _Entry)
        {
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
                            header = line.Remove(0, 1);
                            toggle = true;
                        }
                        else
                        {
                            content += line + "\n";
                        }
                        if (line.EndsWith('|'))
                        {
                            header = "";
                            content = "";
                        }
                    }
                    else
                    {
                        body += line + "\n";
                    }

                }
                return body.Remove(body.Length - 1);
            }
        }
    }  
}

