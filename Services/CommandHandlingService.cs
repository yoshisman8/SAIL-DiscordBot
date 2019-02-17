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
using Familiar;
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
