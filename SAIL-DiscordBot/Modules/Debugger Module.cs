using System;
using System.Net;
using System.Text;
using System.Linq;
using System.Globalization;
using System.Threading.Tasks;
using System.Collections.Generic;
using SAIL.Classes;
using LiteDB;

using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.WebSocket;
using Discord.Rest;
using Discord;

namespace SAIL.Modules
{
    [Name("Debugger Module")][Exclude]
    public class Debugger : SailBase<SocketCommandContext>
    {
        public IServiceProvider Provider {get;set;}
        public CommandService command {get;set;}
        
        [Command("Statistics")] [RequireOwner]
        public async Task Stats()
        {
            var guilds = Program.Database.GetCollection<SysGuild>("Guilds").FindAll();
            var col = Program.Database.GetCollection<Quote>("Quotes").FindAll();
            var All = Program.Database.GetCollection<Character>("Characters").FindAll();

            var embed = new EmbedBuilder();
            foreach(var x in guilds)
            {
                embed.AddField(Context.Client.GetGuild(x.Id).Name,"Characters in this server: "+All.Where(c=>c.Guild==x.Id).Count()+"\n"+"Quotes in this server: "+col.Where(c=>c.Guild==x.Id).Count());
            }
            await ReplyAsync("",false,embed.Build());
        }
        [Command("Import")] [RequireOwner]
        [RequireContext(ContextType.Guild)]
        public async Task Import()
        {
            var Ccol = Program.Database.GetCollection<Character>("Characters"); 
            var chars = Ccol.Find(x=>x.Guild!=Context.Guild.Id);
            
            var Qcol = Program.Database.GetCollection<Quote>("Quotes");
            var quotes = Qcol.Find(x=>x.Guild!=Context.Guild.Id);
            
            var guild = Program.Database.GetCollection<SysGuild>("Guilds").FindOne(x=>x.Id==Context.Guild.Id);

            var Qcount = 0;
            var Ccount = 0;
            foreach(var x in chars)
            {
                var y = x;
                y.Guild = Context.Guild.Id;
                Ccol.Update(y);
                Ccount++;
            }
            foreach(var x in quotes)
            {
                var y = x;
                y.Guild = Context.Guild.Id;
                Qcol.Update(y);
                Qcount++;
            }
            await ReplyAsync("Imported "+Ccount+" characters and "+Qcount+" quotes to "+Context.Guild.Name+".");
        }
        [Command("ResetSettings")] [RequireOwner]
        public async Task Resetto()
        {
			Program.Database.DropCollection("Guilds");
            var guilds = Program.Database.GetCollection<SysGuild>("Guilds");
            foreach (var x in guilds.FindAll())
            {
                var mds = new Dictionary<string,bool>();
                foreach(var m in command.Modules.Where(y=>!y.Attributes.Any(a=>a.GetType()==typeof(Exclude))))
                {
                    mds.Add(m.Name,true);
                }
                x.CommandModules = mds;
                guilds.Update(x);
            }
            await ReplyAsync("Reset all guild module settings.");
        }
    }
}