using System;
using System.Collections.Generic;
using System.Linq;
using Discord.Commands;
using Discord.WebSocket;
using Discord.Addons.Interactive;
using Discord.Addons.CommandCache;
using SAIL.Classes;
using System.Threading.Tasks;
using Discord;
using System.Text;

namespace SAIL.Modules
{
	[Name("Help Module")] [Untoggleable]
	[Summary("Has all commands related to obtaining help.")]
    public class Help_Module : InteractiveBase<SocketCommandContext>
	{
		public CommandService command { get; set; }
		public IServiceProvider provider { get; set; }

		[Command("Help"),Alias("Commands","AllCommands")]
		public async Task AllCommands()
		{
			var guild = Context.Guild != null ? Program.Database.GetCollection<SysGuild>("Guilds").FindOne(x => x.Id == Context.Guild.Id) : null ;
			string prefix = guild?.Prefix ?? "";
			var modules = command.Modules.Where(x => !x.Attributes.Any(y => y.GetType() == typeof(Exclude)));
			var valid = new List<CommandInfo>();
			foreach(var Mod in modules)
			{
				foreach(var Cmd in Mod.Commands)
				{
					if((await Cmd.CheckPreconditionsAsync(Context, provider)).IsSuccess)
					{
						valid.Add(Cmd);
					}
				}
			}

			var commands = new StringBuilder();
			foreach (var x in valid.OrderBy(x => x.Name))
			{
				var sb = new StringBuilder();
				foreach(var a in x.Parameters)
				{
					if (a.IsOptional) sb.Append(" [" + a.Name + "] ");
					else sb.Append(" <" + a.Name + "> ");
				}
				commands.AppendLine(prefix+ x.Name + sb.ToString());
			}
			var embed = new EmbedBuilder()
				.WithTitle("Commands Available for " + Context.User.Username)
				.WithDescription("Paramenters surrounded by `<Example>` are mandatory.\nParameters surrounded by `[Example]` are optional")
				.AddField("Commands",commands.ToString());
			await ReplyAsync(" ", embed: embed.Build());
		}
		[Command("Help"),Alias("Command","Manual")]
		public async Task GetHelp([Remainder]string Command)
		{

		}
	}
}
