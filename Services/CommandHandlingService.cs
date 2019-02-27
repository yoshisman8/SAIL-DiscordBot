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
        private LiteDatabase _database;
        private IServiceProvider _provider;
        private InteractiveService _interactive;
        private readonly IConfiguration _config;
        private CommandCacheService _cache;

        public CommandHandlingService(IConfiguration config, IServiceProvider provider, DiscordSocketClient discord, CommandService commands,LiteDatabase database, CommandCacheService cache,InteractiveService interactive)
        {
            _discord = discord;
            _commands = commands;
            _provider = provider;
            _database = database;
            _config = config;
            _interactive = interactive;
            _cache = cache;
            
            _discord.MessageReceived += MessageReceived;
            _discord.ReactionAdded += OnReactAdded;
            _discord.ReactionsCleared += OnReactionCleared;
            _discord.MessageDeleted += OnMessageDeleted;
            _discord.MessageUpdated += OnMessageUpdated;
            _discord.Ready += OnReady;
        }

        public async Task OnReady()
        {
            await InitializeGuildsDB(_discord, _database);
            await UpdateModules(_discord,_database,_commands);
        }

        private async Task UpdateModules(DiscordSocketClient discord, LiteDatabase database, CommandService commands)
        {
            var col = database.GetCollection<SysGuild>("Guilds");
            var servers = col.FindAll();
            var modules = commands.Modules.Select(x=> x.Name).ToList();
            foreach (var x in modules.Where(x=>x != "Control Module"))
            {
                foreach(var y in servers.Where(z=>!z.Modules.Exists(w=>w.Name == x)))
                {
                    y.Modules.Add(new Classes.Module(){Name=x});
                    col.Update(y);
                }
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
                    col.EnsureIndex("Modules","$.Modules[*].Name",false);
                }
            }
        }

        public async Task OnMessageUpdated(Cacheable<IMessage, ulong> _OldMsg, SocketMessage NewMsg, ISocketMessageChannel Channel)
        {
            var OldMsg = await _OldMsg.DownloadAsync();
            if (OldMsg.Source != MessageSource.User) return;

            var col = _database.GetCollection<Quote>("Quotes");
            
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
            var col = _database.GetCollection<Quote>("Quotes");
            var msg = await _msg.GetOrDownloadAsync();
            if (msg == null || msg.Source != MessageSource.User) return;

            if (col.Exists(x=> x.Message == msg.Id))
            {
                var Quote = col.FindOne(x => x.Message == msg.Id);
                col.Delete(x => x.Message == Quote.Message);
            }
        }

        public async Task OnReactionCleared(Cacheable<IUserMessage, ulong> _msg, ISocketMessageChannel channel)
        {
            var col = _database.GetCollection<Quote>("Quotes");
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
            var col = _database.GetCollection<Quote>("Quotes");
            var msg = await _msg.GetOrDownloadAsync();
            var context = new CommandContext(_discord,msg);
            var guild = context.Guild;

            if (reaction.Emote.Name == "📌" && (col.Exists(x => x.Message == msg.Id)==false))
            {
                Quote Q = new Quote()
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
            await _commands.AddModulesAsync(Assembly.GetEntryAssembly(),_provider);
            // Add additional initialization code here...
        }

        private async Task MessageReceived(SocketMessage rawMessage)
        {
            // Ignore system messages and messages from bots
            if (!(rawMessage is SocketUserMessage message)) return;
            if (message.Source != MessageSource.User) return;
            
            var context = new SocketCommandContext(_discord, message);
            var Guild = _database.GetCollection<SysGuild>("Guilds").FindOne(x=>x.Id==context.Guild.Id);
            if (Guild.ListMode == ListMode.Blacklist && Guild.Channels.Exists(x=> x == message.Channel.Id)) return;
            if (Guild.ListMode == ListMode.Whitelist && Guild.Channels.Exists(x=> x == message.Channel.Id) == false) return;

            
            var command = _commands.Search(context,message.Content.Replace(Guild.Prefix,"").Split(' ').First());
            if (command.IsSuccess && command.Commands != null)
            {
                var module = command.Commands.First().Command.Module;
                int argPos = 0;
                if ((!message.HasMentionPrefix(_discord.CurrentUser, ref argPos) 
                    && !message.HasStringPrefix(Guild.Prefix, ref argPos))
                    || Guild.Modules.Exists(x=> x.Name == module.Name && x.Active == false)) return;

                var result = await _commands.ExecuteAsync(context, argPos, _provider);

                if (result.Error.HasValue && (result.Error.Value != CommandError.UnknownCommand))
                {
                    Console.WriteLine(result.Error); 
                }
            }
        }
    }
}
