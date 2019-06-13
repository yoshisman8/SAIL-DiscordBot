using System;
using System.Net;
using System.Text;
using System.Linq;
using System.Globalization;
using System.Threading.Tasks;
using System.Collections.Generic;
using LiteDB;
using SAIL.Classes;
using Discord.Addons.CommandCache;
using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.WebSocket;
using Discord.Rest;
using Discord;
using System.Text.RegularExpressions;

namespace SAIL.Modules
{
	[Name("Administrative Module")] [Untoggleable]
	[Summary("This module contains a series of Administrative commands for the bot. It cannot be dissabled.")]
	public class AdministrativeModule : InteractiveBase<SocketCommandContext>
	{
		public CommandCacheService Cache { get; set; }
		public IServiceProvider Provider { get; set; }
		public CommandService command { get; set; }

		[Command("Overview"), Alias("Summary")]
		[Summary("Shows the current settings and what modules are on or off")]
		[RequireUserPermission(GuildPermission.ManageGuild)]
		public async Task SummaryPanel()
		{
			var col = Program.Database.GetCollection<SysGuild>("Guilds");
			var guild = col.FindOne(x => x.Id == Context.Guild.Id);
			guild.Load(Context);

			var msg = await ReplyAsync("", embed: guild.Summary(command));
			Cache.Add(Context.Message.Id, msg.Id);
		}
		[Command("Configure"), Alias("Settings","SetUp","Config")]
		[Summary("Shows the current settings and what modules are on or off")]
		[RequireUserPermission(GuildPermission.ManageGuild)]
		public async Task ConfigPanel()
		{
			var col = Program.Database.GetCollection<SysGuild>("Guilds");
			var guild = col.FindOne(x => x.Id == Context.Guild.Id);
			guild.Load(Context);
			string CurrListMode = "";
			switch (guild.ListMode)
			{
				case ListMode.None:
					CurrListMode = "Currently looking for commands on all channels";
					break;
				case ListMode.Blacklist:
					CurrListMode = "Currently ignoring commands on some channels.";
					break;
				case ListMode.Whitelist:
					CurrListMode = "Currently only listening for commands on some channels.";
					break;
			}

			var MenuOptions = new PagedMenu.MenuOption[3][];
			MenuOptions[0] = new PagedMenu.MenuOption[]
			{
				new PagedMenu.MenuOption("Set Server Prefix",
				async(menu,page,opt) =>
				{
					var prompt = await menu.Context.Channel.SendMessageAsync("Please respond with the new prefix (Only the first letter will be used).");
					var response = await menu.Interactive.NextMessageAsync(menu.Context,true,true,TimeSpan.FromMinutes(1));

					((SysGuild)menu.Storage).Prefix = (response!=null && !response.Content.NullorEmpty())? response.Content.Substring(0,1):"!";
					menu.Options[page][opt].Description = "Current Prefix: `"+((SysGuild)menu.Storage).Prefix+"`.";

					await response.DeleteAsync();
					await prompt.DeleteAsync();
					return null;
				},"Current Prefix: `"+guild.Prefix+"`.",false),
				// -------------------------------------------------------------------
				new PagedMenu.MenuOption("Toggle White/Black list",
				async(menu,page,opt) =>
				{
					switch (((SysGuild)menu.Storage).ListMode)
					{
						case ListMode.None:
							((SysGuild)menu.Storage).ListMode = ListMode.Blacklist;
							menu.Options[page][opt].Description = "Currently ignoring commands on some channels.";
						break;
						case ListMode.Blacklist:
							((SysGuild)menu.Storage).ListMode = ListMode.Whitelist;
							menu.Options[page][opt].Description = "Currently only listening for commands on some channels.";
						break;
						case ListMode.Whitelist:
							((SysGuild)menu.Storage).ListMode = ListMode.None;
							menu.Options[page][opt].Description = "Currently looking for commands on all channels";
						break;
					}
					return null;
				},CurrListMode,false),
				// -------------------------------------------------------------------
				new PagedMenu.MenuOption("Set Channels to be White/Black listed",
				async(menu,page,opt) =>
				{
					var prompt = await menu.Context.Channel.SendMessageAsync("Please respond with the list of channels you want to Black or White list separated by a comma.\n(ie: #Channel-1, #Channel-2, #Channel-3)");
					var response = await menu.Interactive.NextMessageAsync(menu.Context,true,true,TimeSpan.FromMinutes(1));
					var names = new Regex(@"\d+").Matches(response.Content).Select(x=>x.Value);
					var chns = new List<SocketTextChannel>();
					foreach(var x in names)
					{
						var temp = Context.Guild.GetTextChannel(ulong.Parse(x));
						if (temp!=null) chns.Add(temp);
					}
					
					((SysGuild)menu.Storage).Channels = chns.Select(x=>x.Id).ToList();

					menu.Options[page][opt].Description = "Current Channels: \n"+string.Join("\n",chns.Select(x=>"• #"+x.Name));

					await response.DeleteAsync();
					await prompt.DeleteAsync();
					return null;
				},"Current Channels: \n"+string.Join("\n",guild.LoadedChannels.Select(x=>x.Name)),false),
				// -------------------------------------------------------------------
				new PagedMenu.MenuOption("Save Changes",
				async(menu,page,opt) =>
				{
					return menu.Storage;
				},"Exit Save all changes."),
				new PagedMenu.MenuOption("Discard Changes",
				async(menu,page,opt) =>
				{
					return null;
				},"Exit and Discard all changes.")
			};
			var n = Context.Guild.GetTextChannel(guild.Notifications.NotificationChannel);
			var not = n == null ? "Not Set" : n.Name;

			MenuOptions[1] = new PagedMenu.MenuOption[]
			{
				new PagedMenu.MenuOption("Toggle Notifications",
				async(menu,page,opt) =>
				{
					((SysGuild)menu.Storage).Notifications.Module ^= true;
					menu.Options[page][opt].Description = "Currently: "+(((SysGuild)menu.Storage).Notifications.Module?"Enablbed ✅":"Disabled ⛔");
					return null;
				},"Currently: "+(guild.Notifications.Module?"Enablbed ✅":"Disabled ⛔"),false),
				// -------------------------------------------------------------------
				new PagedMenu.MenuOption("Set Notification Channel",
				async(menu,page,opt) =>
				{
					var prompt = await menu.Context.Channel.SendMessageAsync("Please respond with the channel name. (ie: `#Channel-Name`)");
					var response = await menu.Interactive.NextMessageAsync(menu.Context,true,true,TimeSpan.FromMinutes(1));
					var ch = new Regex(@"\d+").Match(response.Content).Value;

					var temp = Context.Guild.GetTextChannel(ulong.Parse(ch==null?"0":ch));

					if(temp!= null)
					{
						((SysGuild)menu.Storage).Notifications.NotificationChannel = temp.Id;

						menu.Options[page][opt].Description = "Current Notification Channel: `#"+temp.Name+"`.";
					}
					await response.DeleteAsync();
					await prompt.DeleteAsync();
					return null;
				},"Notification channel: "+not,false),
				// -------------------------------------------------------------------
				new PagedMenu.MenuOption("Set User Joined/Welcome Message",
				async(menu,page,opt) =>
				{
					var prompt = await menu.Context.Channel.SendMessageAsync("Please respond with the welcome message.\n" +
						"the word {user} (case sensitive!) will be replaced with the user's name.\n" +
						"Respond with \"disable\" (case sensitive!) to instead disable the 'User Joined' Message.");
					var response = await menu.Interactive.NextMessageAsync(menu.Context,true,true,TimeSpan.FromMinutes(1));

					if(response.Content=="disable") ((SysGuild)menu.Storage).Notifications.JoinedMsg = "";
					else ((SysGuild)menu.Storage).Notifications.JoinedMsg = response.Content;

					menu.Options[page][opt].Description = "Currently: "+(((SysGuild)menu.Storage).Notifications.JoinedMsg==""?"Disabled ⛔":guild.Notifications.JoinedMsg);

					await response.DeleteAsync();
					await prompt.DeleteAsync();
					return null;
				},"Currently: "+(guild.Notifications.JoinedMsg==""?"Disabled ⛔":guild.Notifications.JoinedMsg),false),
				// -------------------------------------------------------------------
				new PagedMenu.MenuOption("Set User Left/Farewell Message",
				async(menu,page,opt) =>
				{
					var prompt = await menu.Context.Channel.SendMessageAsync("Please respond with the farewell message.\n" +
						"the word {user} (case sensitive!) will be replaced with the user's name.\n" +
						"Respond with \"disable\" (case sensitive!) to instead disable the 'User Left' Message.");
					var response = await menu.Interactive.NextMessageAsync(menu.Context,true,true,TimeSpan.FromMinutes(1));

					if(response.Content=="disable") ((SysGuild)menu.Storage).Notifications.LeftMsg = "";
					else ((SysGuild)menu.Storage).Notifications.LeftMsg = response.Content;

					menu.Options[page][opt].Description = "Currently: "+(((SysGuild)menu.Storage).Notifications.LeftMsg==""?"Disabled ⛔":guild.Notifications.LeftMsg);

					await response.DeleteAsync();
					await prompt.DeleteAsync();
					return null;
				},"Currently: "+(guild.Notifications.LeftMsg==""?"Disabled ⛔":guild.Notifications.LeftMsg),false),
				// -------------------------------------------------------------------
				new PagedMenu.MenuOption("Save Changes",
				async(menu,page,opt) =>
				{
					return menu.Storage;
				},"Exit Save all changes."),
				new PagedMenu.MenuOption("Discard Changes",
				async(menu,page,opt) =>
				{
					return null;
				},"Exit and Discard all changes.")
			};
			var modules = command.Modules.Where(x => guild.CommandModules.Keys.Any(y => y == x.Name) 
												&& !x.Attributes.Any(y=>y.GetType()==typeof(Untoggleable)));
			var options = new List<PagedMenu.MenuOption>();
			foreach(var x in modules)
			{
				options.Add(new PagedMenu.MenuOption(x.Name,
					async (menu, page, opt) =>
					{
						((SysGuild)menu.Storage).CommandModules[x.Name] ^= true;
						menu.Options[page][opt].Description = "Currently " + (((SysGuild)menu.Storage).CommandModules[x.Name] ? "Enablbed ✅" : "Disabled ⛔");
						return null;
					}, "Currently " + (guild.CommandModules[x.Name] ? "Enablbed ✅" : "Disabled ⛔"), false));
			}
			options.Add(new PagedMenu.MenuOption("Save Changes",
				async (menu, page, opt) =>
				{
					return menu.Storage;
				}, "Exit Save all changes."));
			options.Add(new PagedMenu.MenuOption("Discard Changes",
				async (menu, page, opt) =>
				{
					return null;
				}, "Exit and Discard all changes."));
			MenuOptions[2] = options.ToArray();

			var g = (SysGuild)await new PagedMenu(Context.Guild.Name + "'s Settings Panel", " ", MenuOptions, guild).StartMenu(Context, Interactive);

			if (g == null)
			{
				var msg = await ReplyAsync("Discard all changes to " + Context.Guild.Name + "'s Settings.");
				Cache.Add(Context.Message.Id, msg.Id);
				return;
			}
			else
			{
				col.Update(g);
				var msg = await ReplyAsync("Saved all changes to " + Context.Guild.Name + "'s Settings.");
				Cache.Add(Context.Message.Id, msg.Id);
			}
		}
        //private async Task<Embed> GenerateEmbedPage(SocketCommandContext ctx, CommandService cmd,IServiceProvider _provider, Module _module,SysGuild guild)
        //{
            
        //    var module = command.Modules.Single(y => y.Name == _module.Name);
        //        var embed = new EmbedBuilder()
        //        .WithTitle("Commands Available on "+Context.Guild)
        //        .WithDescription("Parameters surounded by [] are optional, Parameters surrounded by <> are mandatory."
        //            +"\nOnly commands you can use will be shown.")
        //        .WithThumbnailUrl(Context.Guild.IconUrl)
        //        .AddField(module.Name,module.Summary,false);
        //    foreach (var c in module.Commands)
        //    {
        //        var result = await c.CheckPreconditionsAsync(ctx,_provider);
        //        if (!result.IsSuccess) continue;
                
        //        string arguments = "";
        //        if(c.Parameters.Count > 0) {
        //            foreach(var p in c.Parameters){
        //            arguments += p.IsOptional? "["+p.Name+"] ":"<"+p.Name+"> ";
        //            }
        //        }
        //        embed.AddField(guild.Prefix+c.Name+" "+arguments,(c.Aliases.Count > 0 ? "Aliases: "+string.Join(",",c.Aliases)+"\n":"")+c.Summary,true);
        //    }
        //    return embed.Build();
        //}
    }
}