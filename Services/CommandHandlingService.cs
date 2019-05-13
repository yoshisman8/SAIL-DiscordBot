using System;
using System.Reflection;
using System.Threading.Tasks;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Discord.Addons.Interactive;
using Discord.Addons.CommandCache;
using LiteDB;
using SAIL.Modules;
using SAIL.Classes;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

namespace SAIL.Services
{
    public class CommandHandlingService
    {
        private readonly DiscordSocketClient _discord;
        private readonly CommandService _commands;
        private IServiceProvider _provider;
        private InteractiveService _interactive;
        private readonly IConfiguration _config;
        private CommandCacheService _cache;
        private GlobalTimer _timer;
        private bool Ready = false;

        public CommandHandlingService(IConfiguration config, IServiceProvider provider, DiscordSocketClient discord, CommandService commands, CommandCacheService cache,InteractiveService interactive,GlobalTimer timer)
        {
            _discord = discord;
            _commands = commands;
            _provider = provider;
            _config = config;
            _interactive = interactive;
            _cache = cache;
            _timer = timer;
            
            _discord.MessageReceived += MessageReceived;
            _discord.ReactionAdded += OnReactAdded;
            _discord.ReactionsCleared += OnReactionCleared;
            _discord.ReactionRemoved += OnReactRemoved;
            _discord.MessageDeleted += OnMessageDeleted;
            _discord.MessageUpdated += OnMessageUpdated;
            _discord.JoinedGuild += OnJoinedGuild;
            _discord.Ready += OnReady;

            
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

        public async Task OnTimerTick()
        {
            if(!Ready) return;
            var col = Program.Database.GetCollection<SysGuild>("Guilds");
            var schedulemod = _commands.Modules.Single(x=> x.Name.Contains("Schedule"));
            var Guilds = col.FindAll().ToList();
            foreach (var x in Guilds)
            {
                var index = x.CommandModules.FindIndex(y=>y.Name == schedulemod.Name);
                if(!x.CommandModules[index].Value) continue;
                if(!x.Notifications) continue;
                if(x.NotificationChannel<=0) continue;
                GuildEvent[] events = null; //TODO

                var guild = _discord.GetGuild(x.Id);
                var channel = guild.GetTextChannel(x.NotificationChannel);
                if (events != null && events.Length > 0)
                {
                    foreach(var E in events)
                    {
                        await x.PrintEvent(_discord,E);
                        if (E.Repeating == RepeatingState.Once)
                        {
                            x.Events.Remove(E);
                            col.Update(x);
                        }
                    }
                }
            }
        }

        public async Task OnReady()
        {
            await InitializeGuildsDB(_discord, Program.Database);
            await UpdateModules(_discord,Program.Database,_commands);
            Ready = true;
        }

        private async Task UpdateModules(DiscordSocketClient discord, LiteDatabase database, CommandService commands)
        {
            var col = database.GetCollection<SysGuild>("Guilds");
            var servers = col.IncludeAll().FindAll().ToArray();
            var modules = commands.Modules.Where(x=>!x.Attributes.Any(y=>y.GetType()==typeof(Exclude))).ToList();
            foreach(var y in servers)
            {
                var mds = y.CommandModules;
                foreach (var x in modules)
                {
                    foreach(var z in y.CommandModules)
                    {
                        if (!modules.Any(m=>m.Name == z.Name)) mds.Remove(z);
                    }
                    if (!y.CommandModules.Any(m=>m.Name == x.Name)) y.CommandModules.Add(new SAIL.Classes.Module(){Name=x.Name,Summary = x.Summary});
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
                    col.EnsureIndex("CommandModules","$.CommandModules[*].Name",false);
                }
            }
        }

        public async Task OnMessageUpdated(Cacheable<IMessage, ulong> _OldMsg, SocketMessage NewMsg, ISocketMessageChannel Channel)
        {
            var OldMsg = await _OldMsg.DownloadAsync();
            if (OldMsg== null||NewMsg==null) return;
            if (OldMsg.Source != MessageSource.User||NewMsg.Source != MessageSource.User) return;

            var col = Program.Database.GetCollection<SAIL.Classes.Quote>("Quotes");
            
            if(_cache.TryGetValue(NewMsg.Id, out var CacheMsg))
            {
                var reply = await Channel.GetMessageAsync(CacheMsg.First());
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
                    _cache.Add(msg.Id,prompt.Id);
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
            _commands.AddTypeReader(typeof(Character[]),new CharacterTypeReader());
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
            var Guild = (context.Guild==null)?null:Program.Database.GetCollection<SysGuild>("Guilds").FindOne(x=>x.Id==context.Guild.Id);

            int argPos = 0;
            if (Guild == null && !message.HasMentionPrefix(_discord.CurrentUser, ref argPos))return;
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
        }
    }
}
