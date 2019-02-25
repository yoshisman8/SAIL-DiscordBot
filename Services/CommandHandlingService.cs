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
using Familiar.Modules;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

namespace Familiar.Services
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
            var msg = await _msg.GetOrDownloadAsync();

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
                foreach (var x in msg.Reactions.Where(x=> x.Key != new Emoji("📌")))
                {
                    Q.Reactions.Add(new QuoteReaction()
                    {
                        Emote = x.Key,
                        Metadata = x.Value
                    });
                }
                if (msg.Content != "")
                {
                    if (Q.IsImageUrl(msg.Content)) Q.Type = QuoteType.ImageURL;
                    else Q.Type = QuoteType.Text;
                    await msg.AddReactionAsync(new Emoji("🔖"));
                }
                if ((msg.Content == "" && msg.Attachments.Count() > 0) && Q.IsImageUrl(msg.Attachments.First().Url))
                {
                    Q.Type = QuoteType.Image;
                    await msg.AddReactionAsync(new Emoji("🖼"));
                }
                if ((msg.Content == "" && msg.Attachments.Count() > 0) && !Q.IsImageUrl(msg.Attachments.First().Url))
                {
                    Q.Type = QuoteType.Attachment;
                    await msg.AddReactionAsync(new Emoji("📦"));
                }
                col.Insert(Q);
                col.EnsureIndex(x => x.Message);
                col.EnsureIndex(x => x.Channel);
                col.EnsureIndex(x => x.Guild);
                col.EnsureIndex(x => x.SearchText.ToLower());
                return;
            }
            if (col.Exists(x=> x.Message == msg.Id))
            {
                var Quote = col.FindOne(x => x.Message == msg.Id);
                if(!Quote.Reactions.Exists(x => x.Emote == reaction.Emote))
                {
                    Quote.Reactions.Add(new QuoteReaction()
                    {
                        Emote = reaction.Emote,
                        Metadata = msg.Reactions.GetValueOrDefault(reaction.Emote)
                    });
                    col.Update(Quote);
                }
                else
                {
                    var r = Quote.Reactions.Find(x=>x.Emote == reaction.Emote);
                    var I = Quote.Reactions.IndexOf(r);
                    Quote.Reactions.ElementAt(I).Metadata = msg.Reactions.GetValueOrDefault(reaction.Emote);
                    col.Update(Quote);
                }
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

            int argPos = 0;
            if (!message.HasMentionPrefix(_discord.CurrentUser, ref argPos) && !message.HasStringPrefix(_config["prefix"], ref argPos)) return;

            var context = new SocketCommandContext(_discord, message);
            var result = await _commands.ExecuteAsync(context, argPos, _provider);

            if (result.Error.HasValue && 
                result.Error.Value != CommandError.UnknownCommand)
                await context.Channel.SendMessageAsync(result.ToString());
        }
    }
}
