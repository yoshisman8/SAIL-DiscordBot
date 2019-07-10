using System;
using System.Reflection;
using System.Threading.Tasks;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using LiteDB;
using SAIL.Classes;
using SAIL.Classes.Legacy;
using Dice;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SAIL.Services
{
    public class CommandHandlingService
    {
        private readonly DiscordSocketClient _discord;
        private readonly CommandService _commands;
        private IServiceProvider _provider;
        private readonly IConfiguration _config;
        private GlobalTimer _timer;
        private bool Ready = false;
		private readonly LogService _logService;

		public Dictionary<ulong, ulong> Cache { get; set; } = new Dictionary<ulong, ulong>();
        public CommandHandlingService(LogService Logger,IConfiguration config, IServiceProvider provider, DiscordSocketClient discord, CommandService commands,GlobalTimer timer)
        {
            _discord = discord;
            _commands = commands;
            _provider = provider;
            _config = config;
            _timer = timer;
			_logService = Logger;
            
            _discord.MessageReceived += MessageReceived;
            _discord.ReactionAdded += OnReactAdded;
            _discord.ReactionsCleared += OnReactionCleared;
            _discord.ReactionRemoved += OnReactRemoved;
            _discord.MessageDeleted += OnMessageDeleted;
            _discord.MessageUpdated += OnMessageUpdated;
            _discord.JoinedGuild += OnJoinedGuild;
            _discord.Ready += OnReady;
			_discord.UserJoined += _discord_UserJoined;
			_discord.UserLeft += _discord_UserLeft;
        }


		private async Task _discord_UserLeft(SocketGuildUser arg)
		{
			
			var guild = Program.Database.GetCollection<SysGuild>("Guilds").FindOne(x=>x.Id==arg.Guild.Id);
			if(guild!= null && guild.Notifications.Module && !guild.Notifications.JoinedMsg.NullorEmpty())
			{
				var ch = arg.Guild.GetTextChannel(guild.Notifications.NotificationChannel);
				await ch.SendMessageAsync(guild.Notifications.JoinedMsg.Replace("{user}", arg.Mention));
			}
		}

		private async Task _discord_UserJoined(SocketGuildUser arg)
		{
			var guild = Program.Database.GetCollection<SysGuild>("Guilds").FindOne(x => x.Id == arg.Guild.Id);
			if (guild != null && guild.Notifications.Module && !guild.Notifications.JoinedMsg.NullorEmpty())
			{
				var ch = arg.Guild.GetTextChannel(guild.Notifications.NotificationChannel);
				await ch.SendMessageAsync(guild.Notifications.LeftMsg.Replace("{user}", arg.Mention));
			}
		}

		public async Task OnJoinedGuild(SocketGuild arg)
        {
            var col = Program.Database.GetCollection<SysGuild>("Guilds");
            if(col.Exists(x=>x.Id==arg.Id))
            {
                await UpdateModules(_discord,Program.Database,_commands);
                return;
            }
            col.Insert(new SysGuild() {Id=arg.Id});
        }


        public async Task OnReady()
        {
            await InitializeGuildsDB(_discord, Program.Database);
            await UpdateModules(_discord,Program.Database,_commands);
            if (File.Exists(Path.Combine(Directory.GetCurrentDirectory(),"Data","Old.db")))
            {
                await Migrate(Program.Database,_discord);
            }
            Ready = true;
        }

        private async Task UpdateModules(DiscordSocketClient discord, LiteDatabase database, CommandService commands)
        {
			SysGuild[] servers;
			LiteCollection<SysGuild> col;
			try
			{
				col = database.GetCollection<SysGuild>("Guilds");
				servers = col.IncludeAll().FindAll().ToArray();
			}
			catch
			{
				database.DropCollection("Guilds");
				col = database.GetCollection<SysGuild>("Guilds");
				servers = col.FindAll().ToArray();
			}			
			var modules = commands.Modules.Where(y => !y.Attributes.Any(a => a.GetType() == typeof(Exclude))).ToList();

            foreach(var y in servers)
            {
                var mds = new Dictionary<string,bool>(y.CommandModules);
                foreach (var x in modules)
                {
					foreach (var z in y.CommandModules)
					{
						if (!modules.Any(m => m.Name == z.Key)) mds.Remove(z.Key);
					}
                    if (!y.CommandModules.Any(m=>m.Key == x.Name)) y.CommandModules.Add(x.Name,true);
                }
                
                if(mds != y.CommandModules) y.CommandModules = mds;
                col.Update(y);
            }
        }

        private async Task InitializeGuildsDB(DiscordSocketClient discord, LiteDatabase database)
        {
            var col = database.GetCollection<SysGuild>("Guilds");
            var joined = discord.Guilds.Select(x=> x.Id).ToList();
            foreach (var x in joined)
            {
                if (!col.Exists(y =>y.Id == x))
                {
                    col.Insert(new SysGuild()
                    {
                        Id = x
                    });
                    col.EnsureIndex("CommandModules","$.CommandModules[*].Key",false);
                }
            }
        }
        private async Task Migrate(LiteDatabase database, DiscordSocketClient discord)
        {
            database.DropCollection("Characters");
            var legacydb = new LiteDatabase(Path.Combine(Directory.GetCurrentDirectory(),"Data","Old.db"));
            var AllChars = legacydb.GetCollection<SAIL.Classes.Legacy.Character>("Characters").IncludeAll().FindAll();
            var guild = discord.GetGuild(311970313158262784);
            var sguild = database.GetCollection<SysGuild>("Guilds").FindOne(x=>x.Id==311970313158262784);
            var AllLegacyChar = new LegacyCharacter().GetAll();
            var CharCol = database.GetCollection<SAIL.Classes.Character>("Characters");
            foreach(var x in AllLegacyChar)
            {
                var c = new SAIL.Classes.Character()
                {
                    Name = x.Name,
                    Guild = 311970313158262784
                };
                if(guild.Users.ToList().Exists(a=>a.ToString() == x.Owner))
                {
                    c.Owner = guild.Users.Single(a=>a.ToString() == x.Owner).Id;
                }
                else
                {
                    c.Owner = guild.OwnerId;
                }
                c.Pages.Add(new CharPage());
                c.Pages[0].Fields.Add(new Field()
                {
                    Title= "Sheet",
                    Content=x.Sheet
                });
            }
            foreach(var x in AllChars)
            {
                x.Inventory.buildInv(legacydb);
                var c = new SAIL.Classes.Character()
                {
                    Name = x.Name,
                    Guild = 311970313158262784,
                    Owner = x.Owner,
                };
                var p1 = new CharPage()
                {
                    Thumbnail = x.ImageUrl,
                    Summary = "Basic Character Info",
                    Fields = new List<Field>()
                    {
                        new Field()
                        {
                            Title = "Basic info",
                            Content = "Class: " + x.Class + "\nRace: " + x.Race + "\nStress: " + x.BuildStress(x)
                        },
                        new Field()
                        {
                            Title = "Gear",
                            Content = x.Buildequip(x)
                        },
                        new Field()
                        {
                            Title = "Afflictions",
                            Content = x.BuildAfflictions(x)
                        },
                        new Field()
                        {
                            Title = x.ITrait.Name,
                            Content = x.ITrait.Description
                        },
                        new Field()
                        {
                            Title = "Traits",
                            Content = x.BuildTraits(x)
                        }
                    }
                };
                c.Pages.Add(p1);
                var p2 = new CharPage()
                {
                    Summary = "Character Skills",
                };
                foreach(var s in x.Skills)
                {
                    p2.Fields.Add(new Field()
                    {
                        Title = s.Name+" ["+s.Level.ToRoman()+"]",
                        Content = s.Description
                    });
                }
                c.Pages.Add(p2);
                var p3 = new CharPage()
                {
                    Summary = "Character Inventory",
                    Thumbnail = "https://image.flaticon.com/icons/png/128/179/179507.png"
                };
                foreach(var i in x.Inventory.Items)
                {
                    p3.Fields.Add(new Field()
                    {
                        Title = i.BaseItem.Name,
                        Content = "Amount: "+i.Quantity+"\n"+i.BaseItem.Description,
                        Inline = true
                    });
                }
                c.Pages.Add(p3);
                CharCol.Insert(c);
				CharCol.EnsureIndex("Name", "LOWER($.Name)");
            }
            legacydb.Dispose();

            File.Move(Path.Combine(Directory.GetCurrentDirectory(),"Data","Old.db"),Path.Combine(Directory.GetCurrentDirectory(),"Data","Old.db.migrated"));
            
        }
        public async Task OnMessageUpdated(Cacheable<IMessage, ulong> _OldMsg, SocketMessage NewMsg, ISocketMessageChannel Channel)
        {
            var OldMsg = await _OldMsg.DownloadAsync();
            if (OldMsg== null||NewMsg==null) return;
            if (OldMsg.Source != MessageSource.User||NewMsg.Source != MessageSource.User) return;

            var col = Program.Database.GetCollection<SAIL.Classes.Quote>("Quotes");
            
            if(Cache.TryGetValue(NewMsg.Id, out var CacheMsg))
            {
                var reply = await Channel.GetMessageAsync(CacheMsg);
                await reply.DeleteAsync();
            }
            await MessageReceived(NewMsg);
            if (col.Exists(x=> x.Message == NewMsg.Id))
            {
                var Quote = col.FindOne(x => x.Message == NewMsg.Id);
                Quote.SearchText = NewMsg.Content;
                col.Update(Quote);
            }
        }

        public async Task OnMessageDeleted(Cacheable<IMessage, ulong> _msg, ISocketMessageChannel channel)
        {
            var col = Program.Database.GetCollection<SAIL.Classes.Quote>("Quotes");
            var msg = await _msg.GetOrDownloadAsync();
            if (msg == null || msg.Source != MessageSource.User) return;

            if (col.Exists(x=> x.Message == msg.Id))
            {
                var Quote = col.FindOne(x => x.Message == msg.Id);
                col.Delete(x => x.Message == Quote.Message);
            }
        }

        private async Task OnReactRemoved(Cacheable<IUserMessage, ulong> _msg, ISocketMessageChannel channel, SocketReaction reaction)
        {
            var col = Program.Database.GetCollection<SAIL.Classes.Quote>("Quotes");
            var msg = await _msg.GetOrDownloadAsync();
        }
        public async Task OnReactionCleared(Cacheable<IUserMessage, ulong> _msg, ISocketMessageChannel channel)
        {
            var col = Program.Database.GetCollection<SAIL.Classes.Quote>("Quotes");
            var msg = await _msg.GetOrDownloadAsync();
            if (msg.Source != MessageSource.User) return;
            if (col.Exists(x=> x.Message == msg.Id))
            {
                var Quote = col.FindOne(x => x.Message == msg.Id);
                col.Delete(x => x.Message == Quote.Message);
            }
        }

        public async Task OnReactAdded(Cacheable<IUserMessage, ulong> _msg, ISocketMessageChannel channel, SocketReaction reaction)
        {
            if (reaction.UserId == _discord.CurrentUser.Id) return;
            var col = Program.Database.GetCollection<SAIL.Classes.Quote>("Quotes");
            var msg = await _msg.GetOrDownloadAsync();
			if (msg== null) return;
            var context = new CommandContext(_discord,msg);
            var guild = context.Guild;

            if (reaction.Emote.Name == "📌" && (col.Exists(x => x.Message == msg.Id)==false))
            {
                SAIL.Classes.Quote Q = new SAIL.Classes.Quote()
                {
                    Message = msg.Id,
                    SearchText = msg.Content,
                    Channel = msg.Channel.Id,
                    Guild = guild.Id
                };
                if (msg.Author != null) Q.Author = msg.Author.Id;
                else Q.Author = 0;
                if (msg.Content == "")
                {
                    var prompt = await channel.SendMessageAsync("This message has no text in it, which will make it impossible to lookup outside of the Random Quote command.\n"+
                    "Please consider editing this message's contents in order to make it searchable in the future.");
                }
                col.Insert(Q);
                col.EnsureIndex(x => x.Message);
                col.EnsureIndex(x => x.Channel);
                col.EnsureIndex(x => x.Guild);
                col.EnsureIndex("SearchText","LOWER($.SearchText)");
                await msg.AddReactionAsync(new Emoji("🔖"));
                return;
            }
        }

        public async Task InitializeAsync(IServiceProvider provider)
        {
            _provider = provider;
            _commands.AddTypeReader(typeof(SAIL.Classes.Character[]),new CharacterTypeReader());
            _commands.AddTypeReader(typeof(ModuleInfo[]),new ModuleTypeReader());
            await _commands.AddModulesAsync(Assembly.GetEntryAssembly(),_provider);
            // Add additional initialization code here...
        }

		private async Task MessageReceived(SocketMessage rawMessage)
		{
			// Ignore system messages and messages from bots
			if (!(rawMessage is SocketUserMessage message)) return;
			if (message.Source != MessageSource.User) return;

			var context = new SocketCommandContext(_discord, message);
			var Guild = (context.Guild == null) ? null : Program.Database.GetCollection<SysGuild>("Guilds").FindOne(x => x.Id == context.Guild.Id);

			if (Guild!=null && Guild.CommandModules["Dice Roller"] && Regex.IsMatch(message.Content, @"\[\[(.*?)\]\]"))
			{
				try
				{
					var rolls = Regex.Matches(message.Content, @"\[\[(.*?)\]\]");
					var sb = new StringBuilder();
					foreach (Match x in rolls)
					{
						var die = Roller.Roll(x.Groups[1].Value);
						sb.AppendLine("[" + die.Expression + "] " + die.ToString().Split("=>")[1] + " ⇒ **" + die.Value + "**.");
					}
					await message.Channel.SendMessageAsync(message.Author.Mention + "\n" + sb.ToString());
				}
				catch
				{
					await message.AddReactionsAsync(new Emoji[]{ new Emoji("🎲"),new Emoji("❔")});
				}
			}

			int argPos = 0;
            if (Guild!= null && !message.HasStringPrefix(Guild.Prefix, ref argPos) && !message.HasMentionPrefix(_discord.CurrentUser, ref argPos)) return;

            if(DateTime.Now.Month == 4 && DateTime.Now.Day == 1)
            {
                var chance = new Random().Next(0,100);
                if(chance <= 25)
                {
                    await context.Channel.SendMessageAsync("OOPSIE WOOPSIE!! Uwu We made a fucky wucky!! A wittle fucko boingo! The code monkeys at our headquarters are working VEWY HAWD to fix this!");
                    return;
                }
            }
            var result = await _commands.ExecuteAsync(context, argPos, _provider);
            
            if (result.Error.HasValue && (result.Error.Value != CommandError.UnknownCommand))
            {
                Console.WriteLine(result.Error+"\n"+result.ErrorReason); 
            }
            if (result.Error.HasValue && result.Error.Value == CommandError.ObjectNotFound)
            {
                var msg = await context.Channel.SendMessageAsync("Sorry. "+result.ErrorReason);
                Cache.Add(context.Message.Id,msg.Id);
            }
			
        }
    }
}
