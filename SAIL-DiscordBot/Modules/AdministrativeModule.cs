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
using Discord.Addon.InteractiveMenus;
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
		public MenuService MenuService { get; set; }

		[Command("Guild"), Alias("Server", "Summary", "ServerInfo")]
		[Summary("Shows the current settings and what modules are on or off, and other miscellaneous details.")]
		[RequireContext(ContextType.Guild)]
		public async Task SummaryPanel()
		{
			var col = Program.Database.GetCollection<SysGuild>("Guilds");
			var guild = col.FindOne(x => x.Id == Context.Guild.Id);
			guild.Load(Context);

			var msg = await ReplyAsync("", embed: guild.Summary(command));
			Cache.Add(Context.Message.Id, msg.Id);
		}
		[Command("SetPrefix")]
		[Summary("Sets the guild prefix.")]
		[RequireContext(ContextType.Guild)]
		public async Task SetPrefix([Remainder]string Prefix)
		{
			var col = Program.Database.GetCollection<SysGuild>("Guilds");
			var guild = col.FindOne(x => x.Id == Context.Guild.Id);
			guild.Load(Context);

			guild.Prefix = Prefix.Substring(0, 1);
			col.Update(guild);

			var msg = await ReplyAsync("Server prefix for commands set to `"+guild.Prefix+"`.");
			Cache.Add(Context.Message.Id, msg.Id);
		}

		[Command("WhoIs"), Alias("User")]
		[Summary("Prints some info about the user.")]
		[RequireContext(ContextType.Guild)]
		public async Task WhoIs([Remainder] SocketGuildUser Username)
		{
			var col = Program.Database.GetCollection<SysGuild>("Guilds");
			var guild = col.FindOne(x => x.Id == Context.Guild.Id);
			var characters = Program.Database.GetCollection<Character>("Characters").Find(x => x.Owner == Username.Id && x.Guild==Context.Guild.Id).Select(x=>x.Name);
			var Templates = guild.CharacterTemplates.Where(x => x.Owner == Username.Id).Select(x => x.Name);

			string c = characters.Count()==0? Username.Username + " has made no characters.": string.Join(", ", characters);
			string t = Templates.Count()==0 ? Username.Username + " has made no templates.": string.Join(", ", Templates);
			string roles = Username.Roles.Count > 0 ? string.Join(", ", Username.Roles.Select(x => x.Mention)) : Username.Username+" has no roles.";
			var embed = new EmbedBuilder()
				.WithTitle(Username.Username)
				.WithThumbnailUrl(Username.GetAvatarUrl())
				.WithUrl(Username.GetAvatarUrl())
				.AddField("Joined " + Context.Guild.Name + " at.", Username.JoinedAt.Value.ToString("MM/dd/yyyy h:mm tt"), true)
				.AddField("Roles", roles, true)
				.AddField("Characters", c, true)
				.AddField("Templates", t, true);
			var msg = await ReplyAsync("", embed: embed.Build());
			Cache.Add(Context.Message.Id, msg.Id);
		}
		[Command("ConfigWhitelist"),Alias("EditBlacklist","EditWhitelsit","ConfigBlacklist")]
		[Summary("Configure the server's White/Blacklist settings")]
		[RequireUserPermission(GuildPermission.ManageGuild)] [RequireContext(ContextType.Guild)]
		public async Task ConfigWhiteBlackList()
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
			var options = new EditorMenu.EditorOption[]
			{
				new EditorMenu.EditorOption("Toggle White/Black list",CurrListMode,
				async(m) =>
				{
					switch (((SysGuild)m.EditableObject).ListMode)
					{
						case ListMode.None:
							((SysGuild)m.EditableObject).ListMode = ListMode.Blacklist;
							m.CurrentOption.Description = "Currently ignoring commands on some channels.";
						break;
						case ListMode.Blacklist:
							((SysGuild)m.EditableObject).ListMode = ListMode.Whitelist;
							m.CurrentOption.Description = "Currently only listening for commands on some channels.";
						break;
						case ListMode.Whitelist:
							((SysGuild)m.EditableObject).ListMode = ListMode.None;
							m.CurrentOption.Description = "Currently looking for commands on all channels";
						break;
					}
					return m.EditableObject;
				}),
				// -------------------------------------------------------------------
				new EditorMenu.EditorOption("Set Channels to be White/Black listed",
					"Current Channels: \n"+string.Join("\n",guild.LoadedChannels.Select(x=>x.Name)),
				async(C) =>
				{
					var prompt = await C.CommandContext.Channel.SendMessageAsync("Please respond with the list of channels you want to Black or White list separated by a comma.\n(ie: #Channel-1, #Channel-2, #Channel-3)");
					var response = await C.MenuService.NextMessageAsync(C.CommandContext,TimeSpan.FromMinutes(1));
					var names = new Regex(@"\d+").Matches(response.Content).Select(x=>x.Value);
					var chns = new List<SocketTextChannel>();
					foreach(var x in names)
					{
						var temp = Context.Guild.GetTextChannel(ulong.Parse(x));
						if (temp!=null) chns.Add(temp);
					}

					((SysGuild)C.EditableObject).Channels = chns.Select(x=>x.Id).ToList();

					C.CurrentOption.Description = "Current Channels: \n"+string.Join("\n",chns.Select(x=>"• #"+x.Name));

					await response.DeleteAsync();
					await prompt.DeleteAsync();
					return C.EditableObject;
				}),
			};
			var menu = new EditorMenu("Black/Whitelist settings for " + Context.Guild.Name, guild, options);
			await MenuService.CreateMenu(Context, menu, true);
			var g = (SysGuild)await menu.GetObject();
			if (g == null)
			{
				var msg = await ReplyAsync("🗑 Discarted all changes to " + Context.Guild.Name + "'s Settings.");
				Cache.Add(Context.Message.Id, msg.Id);
				return;
			}
			else
			{
				col.Update(g);
				var msg = await ReplyAsync("💾 Saved all changes to " + Context.Guild.Name + "'s Settings.");
				Cache.Add(Context.Message.Id, msg.Id);
			}
		}
		[Command("ConfigNotifcations"),Alias("NotificationSettings", "EditNotifications","Notifications")]
		[Summary("Configure the server's notification settings.")]
		[RequireUserPermission(GuildPermission.ManageGuild)] [RequireContext(ContextType.Guild)]
		public async Task NotifSettings()
		{
			var col = Program.Database.GetCollection<SysGuild>("Guilds");
			var guild = col.FindOne(x => x.Id == Context.Guild.Id);
			guild.Load(Context);
			var n = Context.Guild.GetTextChannel(guild.Notifications.NotificationChannel);
			var not = n == null ? "Not Set" : n.Name;
			var options = new EditorMenu.EditorOption[]
			{
				new EditorMenu.EditorOption("Toggle Notifications","Currently: "+(guild.Notifications.Module?"Enablbed ✅":"Disabled ⛔"),
				async(menu) =>
				{
					((SysGuild)menu.EditableObject).Notifications.Module ^= true;
					menu.CurrentOption.Description = "Currently: "+(((SysGuild)menu.EditableObject).Notifications.Module?"Enablbed ✅":"Disabled ⛔");
					return menu.EditableObject;
				}),
				// -------------------------------------------------------------------
				new EditorMenu.EditorOption("Set Notification Channel","Notification channel: "+not,
				async(menu) =>
				{
					var prompt = await menu.CommandContext.Channel.SendMessageAsync("Please respond with the channel name. (ie: `#Channel-Name`)");
					var response = await menu.MenuService.NextMessageAsync(menu.CommandContext,TimeSpan.FromMinutes(1));
					var ch = new Regex(@"\d+").Match(response.Content).Value;

					var temp = Context.Guild.GetTextChannel(ulong.Parse(ch==null?"0":ch));

					if(temp!= null)
					{
						((SysGuild)menu.EditableObject).Notifications.NotificationChannel = temp.Id;

						menu.CurrentOption.Description = "Current Notification Channel: `#"+temp.Name+"`.";
					}
					await response.DeleteAsync();
					await prompt.DeleteAsync();
					return menu.EditableObject;
				}),
				// -------------------------------------------------------------------
				new EditorMenu.EditorOption("Set User Joined/Welcome Message","Currently: "+(guild.Notifications.JoinedMsg==""?"Disabled ⛔":guild.Notifications.JoinedMsg),
				async(menu) =>
				{
					var prompt = await menu.CommandContext.Channel.SendMessageAsync("Please respond with the welcome message.\n" +
						"the word {user} (case sensitive!) will be replaced with the user's name.\n" +
						"Respond with \"disable\" (case sensitive!) to instead disable the 'User Joined' Message.");
					var response = await menu.MenuService.NextMessageAsync(menu.CommandContext,TimeSpan.FromMinutes(1));

					if(response.Content=="disable") ((SysGuild)menu.EditableObject).Notifications.JoinedMsg = "";
					else ((SysGuild)menu.EditableObject).Notifications.JoinedMsg = response.Content;

					menu.CurrentOption.Description = "Currently: "+(((SysGuild)menu.EditableObject).Notifications.JoinedMsg==""?"Disabled ⛔":((SysGuild)menu.EditableObject).Notifications.JoinedMsg);

					await response.DeleteAsync();
					await prompt.DeleteAsync();
					return menu.EditableObject;
				}),
				// -------------------------------------------------------------------
				new EditorMenu.EditorOption("Set User Left/Farewell Message","Currently: "+(guild.Notifications.JoinedMsg==""?"Disabled ⛔":guild.Notifications.JoinedMsg),
				async(menu) =>
				{
					var prompt = await menu.CommandContext.Channel.SendMessageAsync("Please respond with the farewell message.\n" +
						"the word {user} (case sensitive!) will be replaced with the user's name.\n" +
						"Respond with \"disable\" (case sensitive!) to instead disable the 'User Left' Message.");
					var response = await menu.MenuService.NextMessageAsync(menu.CommandContext,TimeSpan.FromMinutes(1));

					if(response.Content=="disable") ((SysGuild)menu.EditableObject).Notifications.JoinedMsg = "";
					else ((SysGuild)menu.EditableObject).Notifications.JoinedMsg = response.Content;

					menu.CurrentOption.Description = "Currently: "+(((SysGuild)menu.EditableObject).Notifications.JoinedMsg==""?"Disabled ⛔":((SysGuild)menu.EditableObject).Notifications.JoinedMsg);

					await response.DeleteAsync();
					await prompt.DeleteAsync();
					return menu.EditableObject;
				})
			};

			var editor = new EditorMenu(Context.Guild.Name + "'s Notifications Settings", guild, options.ToArray());
			await MenuService.CreateMenu(Context, editor, true);
			var g = (SysGuild)await editor.GetObject();

			if (g == null)
			{
				var msg = await ReplyAsync("🗑 Discard all changes to " + Context.Guild.Name + "'s Settings.");
				Cache.Add(Context.Message.Id, msg.Id);
				return;
			}
			else
			{
				col.Update(g);
				var msg = await ReplyAsync("💾 Saved all changes to " + Context.Guild.Name + "'s Settings.");
				Cache.Add(Context.Message.Id, msg.Id);
			}
		}
		[Command("ConfigModules"), Alias("ToggleModules","ConfigureModules")]
		[Summary("Enable or Disable a module")]
		[RequireUserPermission(GuildPermission.ManageGuild)]
		public async Task ConfigPanel()
		{
			var col = Program.Database.GetCollection<SysGuild>("Guilds");
			var guild = col.FindOne(x => x.Id == Context.Guild.Id);
			guild.Load(Context);

			var modules = command.Modules.Where(x => guild.CommandModules.Keys.Any(y => y == x.Name) 
												&& !x.Attributes.Any(y=>y.GetType()==typeof(Untoggleable)));
			var options = new List<EditorMenu.EditorOption>();
			foreach(var x in modules)
			{
				options.Add(
					new EditorMenu.EditorOption(x.Name, "Currently " + (guild.CommandModules[x.Name] ? "Enablbed ✅" : "Disabled ⛔"),
						async (Ctx) =>
						{
							((SysGuild)Ctx.EditableObject).CommandModules[x.Name] ^= true;
							Ctx.CurrentOption.Description = "Currently " + (((SysGuild)Ctx.EditableObject).CommandModules[x.Name] ? "Enablbed ✅" : "Disabled ⛔");
							return Ctx.EditableObject;
						}));
			}

			var menu = new EditorMenu(Context.Guild.Name + "'s Module settings", guild, options.ToArray());
			await MenuService.CreateMenu(Context, menu, true);
			var g = (SysGuild)await menu.GetObject();

			if (g == null)
			{
				var msg = await ReplyAsync("🗑 Discard all changes to " + Context.Guild.Name + "'s Settings.");
				Cache.Add(Context.Message.Id, msg.Id);
				return;
			}
			else
			{
				col.Update(g);
				var msg = await ReplyAsync("💾 Saved all changes to " + Context.Guild.Name + "'s Settings.");
				Cache.Add(Context.Message.Id, msg.Id);
			}
		}
		[Command("Role"),Alias("SelfAssign")]
		[RequireBotPermission(GuildPermission.ManageRoles)]
		[Summary("Assigns you one of the self-assignable roles")]
		public async Task SelfAssign([Remainder]SocketRole Role)
		{
			var col = Program.Database.GetCollection<SysGuild>("Guilds");
			var guild = col.FindOne(x => x.Id == Context.Guild.Id);
			guild.Load(Context);

			if (guild.AssignableRoles.Contains(Role.Id))
			{
				var user = (SocketGuildUser)Context.User;
				await user.AddRoleAsync(Role);
				var msg = await ReplyAsync(Context.User.Mention+", you've been asigned the role "+Role.Name);
			}
		}
		[Command("Role"), Alias("SelfAssign")]
		[RequireUserPermission(GuildPermission.ManageGuild)]
		[RequireBotPermission(GuildPermission.ManageRoles)]
		[Summary("Sets up a role for self-assigment.")]
		public async Task setSelfAssign([Remainder]SocketRole Role)
		{
			var col = Program.Database.GetCollection<SysGuild>("Guilds");
			var guild = col.FindOne(x => x.Id == Context.Guild.Id);
			guild.Load(Context);

			if (guild.AssignableRoles.Contains(Role.Id))
			{
				guild.AssignableRoles.Remove(Role.Id);
				col.Update(guild);
				var msg = await ReplyAsync("Role "+Role.Name + " is no longer self-assignable." );
			}
			else
			{
				guild.AssignableRoles.Add(Role.Id);
				col.Update(guild);
				var msg = await ReplyAsync("Role " + Role.Name + " is now self-assignable.");	
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