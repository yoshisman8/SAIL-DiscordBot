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
            InitializeGuildsDB(_discord, _commands,_database);
        }

        private Task InitializeGuildsDB(DiscordSocketClient discord, CommandService commands, LiteDatabase database)
        {
            var col = database.GetCollection<SysGuild>("Guilds");
            var joined = discord.Guilds.
            foreach(var x )
        }

        public async Task OnMessageUpdated(Cacheable<IMessage, ulong> OldMsg, SocketMessage NewMsg, ISocketMessageChannel Channel)
        {
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

            if (col.Exists(x=> x.Message == msg.Id))
            {
                var Quote = col.FindOne(x => x.Message == msg.Id);
                col.Delete(x => x.Message == Quote.Message);
            }
        }

        public async Task OnReactAdded(Cacheable<IUserMessage, ulong> _msg, ISocketMessageChannel channel, SocketReaction reaction)
        {
            var col = _database.GetCollection<Quote>("Quotes");
            var msg = await _msg.GetOrDownloadAsync() as SocketUserMessage;

            if (reaction.Emote == new Emoji("📌") && !col.Exists(x => x.Message == msg.Id))
            {  
                Quote Q = new Quote()
                {
                    Message = msg.Id,
                    SearchText = msg.Content,
                    Channel = msg.Channel.Id
                };
                if (msg.Author != null) Q.Author = msg.Author.Id;
                else Q.Author = 0;
                if (msg.Content == "")
                {
                    var prompt = await channel.SendMessageAsync("This message has no text in it, which would make it impossible to lookup and will make it appear only on random quote pools. Please respond with a string of text to use for the purposes of searching this mesage using the `Quote` command.");
                    var reply = await _interactive.NextMessageAsync(new SocketCommandContext(_discord,msg));
                    Q.SearchText = reply.Content.ToLower();
                }
                await msg.AddReactionAsync(new Emoji("🔖"));
                col.Insert(Q);
                col.EnsureIndex(x => x.Message);
                col.EnsureIndex(x => x.Channel);
                col.EnsureIndex(x => x.Guild);
                col.EnsureIndex(x => x.SearchText.ToLower());
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
            var module = _commands.Search(context,message.Content.Replace(Guild.Prefix,"")
                                .Split(' ').First()).Commands.First().Command.Module.Name;
            int argPos = 0;
            if ((!message.HasMentionPrefix(_discord.CurrentUser, ref argPos) 
                && !message.HasStringPrefix(Guild.Prefix, ref argPos))
                || Guild.Modules.Exists(x=> x.Name == module && x.Active == false)) return;

            var result = await _commands.ExecuteAsync(context, argPos, _provider);

            if (result.Error.HasValue && 
                (result.Error.Value != CommandError.UnknownCommand))
                Console.WriteLine(result.Error); 
        }
    }
}
