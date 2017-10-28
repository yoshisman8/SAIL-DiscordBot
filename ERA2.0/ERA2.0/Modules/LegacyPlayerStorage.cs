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

namespace ERA.Modules
{
    public class LegacyCharacter
    {
        public string Owner { get; set; }
        public string Name { get; set; }
        public string Sheet { get; set; }

        public IOrderedEnumerable<LegacyCharacter> Query(string _Query)
        {
            Directory.CreateDirectory(@"Data/Legacy/");
            var files = Directory.EnumerateFiles(@"Data/Legacy/");
            List<LegacyCharacter> db = new List<LegacyCharacter> { };
            foreach (string x in files)
            {
                db.Add(JsonConvert.DeserializeObject<LegacyCharacter>(File.ReadAllText(x)));
            }
            var query = db.Where(x => x.Name.ToLower().StartsWith(_Query.ToLower())).OrderBy(x => x.Name);
            return query;
        }
    }
    public class LegacyPlayerStorage : ModuleBase<SocketCommandContext>
    {
        [Command("Addchar")]
        [Alias("Add-Char", "LegacyAdd")]
        public async Task Addchar(string name, [Remainder] string sheet)
        {
            Directory.CreateDirectory(@"Data/Legacy/");
            var Char = new LegacyCharacter
            {
                Owner = Context.User.ToString(),
                Name = name,
                Sheet = sheet
            };
            string json = JsonConvert.SerializeObject(Char);
            if (File.Exists("Data/Legacy/" + Char.Name + ".json") == true)
            {
                File.WriteAllText("Data/Legacy/" + Char.Name + ".json", json);
                await Context.Channel.SendMessageAsync(Context.User.Mention+", Character **" + Char.Name + "** Updated Successfully!");
            }
            else
            {
                File.WriteAllText("Data/Legacy/" + Char.Name + ".json", json);
                await Context.Channel.SendMessageAsync(Context.User.Mention+", Character **" + Char.Name + "** Created Successfully!");
            }
        }
        [Command("Char")]
        [Alias("LegacyChar")]
        public async Task GetChar(string name)
        {
            var query = new LegacyCharacter().Query(name);
            if (query.Count() > 1 && query.First().Name.ToLower() != name)
            {
                string msg = "Multiple charactes were found! Please specify which one of the following characters is the one you're looking for: ";
                foreach (LegacyCharacter q in query)
                {
                    msg += "`" + q.Name + "` ";
                }
                await Context.Channel.SendMessageAsync(msg);
            }
            else if (query.Count() == 1)
            {
                var character = query.First();
                await Context.Channel.SendMessageAsync(Context.User.Mention+", Character **"+character.Name+"** (Created by "+character.Owner+"):\n"+character.Sheet);
            }
            else
            {
                await Context.Channel.SendMessageAsync(Context.User.Mention+"This Character does not exist!");
            }
        }
        [Command("DeleteChar")]
        [Alias("LegacyDelete", "Del-Char", "Delchar")]
        public async Task DelChar(string name)
        {
            IRole Dmasters = Context.Guild.GetRole(324320068748181504);
            var User = Context.User as SocketGuildUser;
            Directory.CreateDirectory(@"Data/Legacy/");
            var files = Directory.EnumerateFiles(@"Data/Legacy/");
            List<LegacyCharacter> db = new List<LegacyCharacter> { };
            foreach (string x in files)
            {
                db.Add(JsonConvert.DeserializeObject<LegacyCharacter>(File.ReadAllText(x)));
            }
            var query = new LegacyCharacter().Query(name);
            if (query.Count() > 1 && query.First().Name.ToLower() != name)
            {
                string msg = "Multiple charactes were found! Please specify which one of the following characters is the one you're looking for: ";
                foreach (LegacyCharacter q in query)
                {
                    msg += "`" + q.Name + "` ";
                }
                await Context.Channel.SendMessageAsync(msg);
            }
            else if (query.Count() == 1)
            {
                var character = query.First();

                if (character.Owner == Context.User.ToString() || User.Roles.Contains(Dmasters) == true)
                { 
                    File.Delete("Data/Legacy/" + name + ".json");
                    await Context.Channel.SendMessageAsync(Context.User.Mention + " Character **" + character.Name + "** deleted!");
                }
                else
                {
                    await Context.Channel.SendMessageAsync("This isnt your Character! You cant delete it!");
                }
            }
            else
            {
                await Context.Channel.SendMessageAsync(Context.User.Mention + "This Character does not exist!");
            }
        }
    }
}
