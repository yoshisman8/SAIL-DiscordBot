using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Discord.Commands;
using Discord.WebSocket;
using Discord;
using Discord.Addons.Interactive;
using Newtonsoft.Json;
using System.Linq;

namespace ERA.Modules
{
    public class LegacyCharacter
    {
        public string Owner { get; set; }
        public string Name { get; set; }
        public string Sheet { get; set; }
    }
    public class LegacyPlayerStorage : InteractiveBase<SocketCommandContext>
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
            Directory.CreateDirectory(@"Data/Legacy/");
            var query = Directory.EnumerateFiles(@"Data/Legacy/","*"+name+"*");
            if (query.Count() > 1)
            {
                string msg = "Multiple charactes were found! Please specify which one of the following characters is the one you're looking for:\n";
                foreach (string q in query)
                {
                    LegacyCharacter result = JsonConvert.DeserializeObject<LegacyCharacter>(q);
                    msg += "`" + result.Name + " ";
                }
                await Context.Channel.SendMessageAsync(msg);
            }
            else if (query.Count() == 1)
            {
                string sheet = File.ReadAllText(query.First());
                var character = JsonConvert.DeserializeObject<LegacyCharacter>(sheet);
                await Context.Channel.SendMessageAsync(Context.User.Mention+", Character **"+character.Name+"** (Created by "+character.Owner+"):\n"+character.Sheet);
            }
            else
            {
                await Context.Channel.SendMessageAsync(Context.User.Mention+"This Character does not exist!");
            }
        }
        [Command("DeleteChar")]
        [Alias("LegacyDelete", "Del-Char", "delchar")]
        public async Task DelChar(string name)
        {
            IRole Dmasters = Context.Guild.GetRole(357023942520733696);
            var User = Context.User as SocketGuildUser;
            var role = User.Roles.Where(x => x.Id == Dmasters.Id);
            Directory.CreateDirectory(@"Data/Legacy/");
            var query = Directory.EnumerateFiles(@"Data/Legacy/", "*" + name + "*");
            if (query.Count() > 1)
            {
                string msg = "Multiple charactes were found! Please specify which one of the following characters is the one you're looking for:\n";
                foreach (string q in query)
                {
                    LegacyCharacter result = JsonConvert.DeserializeObject<LegacyCharacter>(q);
                    msg += "`" + result.Name + " ";
                }
                await Context.Channel.SendMessageAsync(msg);
            }
            else if (query.Count() == 1)
            {
                string sheet = File.ReadAllText(query.First());
                var character = JsonConvert.DeserializeObject<LegacyCharacter>(sheet);

                if (character.Owner == Context.User.ToString() || role != null)
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
