using Discord.Commands;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;

namespace Example
{
    public class CommandHandler
    {
        private readonly DiscordSocketClient _discord;
        private readonly CommandService _commands;
        private readonly IConfigurationRoot _config;
        private readonly IServiceProvider _provider;

        // DiscordSocketClient, CommandService, IConfigurationRoot, and IServiceProvider are injected automatically from the IServiceProvider
        public CommandHandler(
            DiscordSocketClient discord,
            CommandService commands,
            IConfigurationRoot config,
            IServiceProvider provider)
        {
            _discord = discord;
            _commands = commands;
            _config = config;
            _provider = provider;

            _discord.MessageReceived += OnMessageReceivedAsync;
            _discord.UserJoined += _discord_UserJoined;
            //_discord.MessageUpdated += OnMessageUpdate;
            _discord.UserLeft += OnUserLeft;
        }

        private async Task OnUserLeft(SocketGuildUser u)
        {
            SocketGuild Guild = _discord.GetGuild(311970313158262784);
            IMessageChannel Fax = Guild.GetTextChannel(358635970632876043);
            var builder = new EmbedBuilder()
               .WithAuthor(_discord.CurrentUser)
               .WithColor(new Color(255, 0, 0))
               .WithTitle(u.Username + " Left!")
               .WithThumbnailUrl(u.GetAvatarUrl())
               .WithCurrentTimestamp();
            await Fax.SendMessageAsync("", embed: builder.Build());
        }

        //private async Task OnMessageUpdate(Cacheable<IMessage, ulong> original, SocketMessage edit, ISocketMessageChannel channel)
        //{
        //    var command = channel.GetMessagesAsync(edit, Direction.After, 2,CacheMode.AllowDownload) as List<IMessage>;
        //    var query = command.Find(x => x.Author == _discord.CurrentUser);
        //    await query.DeleteAsync();
        //    await OnMessageReceivedAsync(edit);
        //}

        private async Task _discord_UserJoined(SocketGuildUser u)
        {
            SocketGuild Guild = _discord.GetGuild(311970313158262784);
            IRole Admin = Guild.GetRole(311989788540665857);
            IRole TrialAdmin = Guild.GetRole(364633182357815298);
            IMessageChannel ReceptionDesk = Guild.GetTextChannel(311974698839703562);
            IMessageChannel Fax = Guild.GetTextChannel(358635970632876043);

            var msg = await ReceptionDesk.SendMessageAsync("Welcome to the server " + u.Mention + "! \nPLease wait here while either a "+ Admin.Mention +" or a "+ TrialAdmin.Mention +" gives" +
                "you the Spectator role! \n In the meantime, make sure to read the rules on #rules-of-the-den!");
            await Task.Delay(TimeSpan.FromMinutes(5));
            var builder = new EmbedBuilder()
                .WithAuthor(_discord.CurrentUser)
                .WithColor(new Color(0, 255, 0))
                .WithTitle(u.Username + " Joined!")
                .WithThumbnailUrl(u.GetAvatarUrl())
                .WithCurrentTimestamp();
            await Fax.SendMessageAsync("", embed: builder.Build());
            await msg.DeleteAsync();
        }

        private async Task OnMessageReceivedAsync(SocketMessage s)
        {
            var msg = s as SocketUserMessage;     // Ensure the message is from a user/bot
            if (msg == null) return;
            if (msg.Author == _discord.CurrentUser) return;     // Ignore self when checking commands
            
            var context = new SocketCommandContext(_discord, msg);     // Create the command context

            int argPos = 0;     // Check if the message has a valid command prefix
            if (msg.HasStringPrefix(_config["prefix"], ref argPos) || msg.HasMentionPrefix(_discord.CurrentUser, ref argPos))
            {
                var result = await _commands.ExecuteAsync(context, argPos, _provider);     // Execute the command

                if (!result.IsSuccess && result.Error != CommandError.UnknownCommand)
                {     // If not successful, reply with the error.
                    await context.Channel.SendMessageAsync("Something went wrong! Use `$Help <command>` to see how that command works and get more help!");
                }
            }
        }
    }
}
