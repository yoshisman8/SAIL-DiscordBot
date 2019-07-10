using System;
using System.Collections.Generic;
using System.Linq;
using Discord.Commands;
using Discord.WebSocket;
using Discord.Addons.Interactive;

using SAIL.Classes;
using System.Threading.Tasks;
using Discord;
using System.Text;

namespace SAIL.Modules
{
	[Name("Help Module")] [Untoggleable]
	[Summary("Has all commands related to obtaining help.")]
    public class Help_Module : SailBase<SocketCommandContext>
	{
		public CommandService CommandService { get; set; }
		public IServiceProvider provider { get; set; }

		[Command("Help"),Alias("Commands","AllCommands")]
		public async Task AllCommands()
		{
			var guild = Context.Guild != null ? Program.Database.GetCollection<SysGuild>("Guilds").FindOne(x => x.Id == Context.Guild.Id) : null ;
			string prefix = guild?.Prefix ?? "";
			var modules = CommandService.Modules.Where(x => !x.Attributes.Any(y => y.GetType() == typeof(Exclude)));
			var embed = new EmbedBuilder()
				.WithTitle("Commands Available for " + Context.User.Username)
				.WithDescription("Paramenters surrounded by `<Example>` are mandatory.\nParameters surrounded by `[Example]` are optional");

			var cmds = new StringBuilder();
			foreach (var Mod in modules.Where(x=>guild.CommandModules[x.Name] || x.Attributes.Any(a=> a.GetType()==typeof(Untoggleable))))
			{
				cmds.Clear();
				foreach(var C in Mod.Commands)
				{
					if((await C.CheckPreconditionsAsync(Context, provider)).IsSuccess)
					{
						string command = prefix+C.Name;
						foreach(var a in C.Parameters)
						{
							if (a.IsOptional) command += " [" + a.Name + "] ";
							else command += " <" + a.Name + "> ";
						}
						cmds.AppendLine(command);
					}
				}
				embed.AddField(Mod.Name, cmds.ToString());
			}
			await ReplyAsync(" ", embed: embed.Build());
		}
		[Command("Help"),Alias("Command","Manual")]
		public async Task GetHelp([Remainder]string Command)
		{
			var guild = Context.Guild != null ? Program.Database.GetCollection<SysGuild>("Guilds").FindOne(x => x.Id == Context.Guild.Id) : null;
			string prefix = guild?.Prefix ?? "";

			var embed = new EmbedBuilder()
				.WithTitle("Results for \"" + Command + "\"")
				.WithDescription("Paramenters surrounded by `<Example>` are mandatory.\nParameters surrounded by `[Example]` are optional"); ;

			var modules = CommandService.Modules.Where(x => !x.Attributes.Any(a => a.GetType() == typeof(Exclude)));
			var results = modules.SelectMany(x => x.Commands).Where(x=>x.Name.StartsWith(Command,StringComparison.CurrentCultureIgnoreCase)).ToList();

			if (results.Count==0)
			{
				await ReplyAsync("Could not find any commands that start with \""+Command+"\".");
				return;
			}
			foreach(var x in results)
			{
				string args = "";
				foreach(var a in x.Parameters)
				{
					args += a.IsOptional ? " [" + a.Name + "]" : " <" + a.Name + ">";
				}
				embed.AddField(x.Name, 
					"**Aliases**: "+string.Join(", ",x.Aliases)+
					"\n**From Module**: "+x.Module.Name+
					".\n**Usage**: `"+prefix+x.Name+args+"`\n\n"+x.Summary);
			}
			await ReplyAsync(" ", embed.Build());
		}
	}
}
