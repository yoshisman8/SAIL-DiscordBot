using System;
using System.Reflection;
using System.Threading.Tasks;
using Discord;
using System.IO;
using Discord.Commands;
using Discord.WebSocket;
using LiteDB;
using Newtonsoft.Json;
using Microsoft.Extensions.Configuration;
using System.Linq;

namespace DiscordBot.Services
{
    public class CommandHandlingService
    {
        private readonly DiscordSocketClient _discord;
        private readonly CommandService _commands;
        private IConfiguration _config;
        private IServiceProvider _provider;
        private LiteDatabase _database;

        public CommandHandlingService(IServiceProvider provider, DiscordSocketClient discord,IConfiguration config, CommandService commands, LiteDatabase database)
        {
            _discord = discord;
            _commands = commands;
            _provider = provider;
            _database = database;
            _config = config;

            _discord.MessageReceived += MessageReceived;
            _discord.UserJoined += _discord_UserJoined;
            _discord.UserLeft += OnUserLeft;
            _discord.ReactionAdded += OnReact;
        }

        private async Task OnReact(Cacheable<IUserMessage, ulong> m, ISocketMessageChannel c, SocketReaction r)
        {
            var msg = await m.DownloadAsync();
            if (r.Emote.Name == "🗣")
            {
                Directory.CreateDirectory(@"Data/Quotes/");
                Quote quote = new Quote
                {
                    Content = msg.Content,
                    Date = msg.Timestamp.DateTime,
                    Channel = c.Id,
                    User = msg.Author.Id
                };
                string json = JsonConvert.SerializeObject(quote);
                File.WriteAllText(@"Data/Quotes/" + msg.Id + ".json", json);
                await msg.AddReactionAsync(new Emoji("💽"));
            }
            else if (r.Emote.Name == "🔥")
            {
                await msg.AddReactionAsync(new Emoji("🚒"));
            }
            else if (r.Emote.Equals(Emote.Parse("<:fifihype:315182065169596417>")))
            {
                await msg.AddReactionAsync(Emote.Parse("<:fifihype:315182065169596417>"));
            }
        }

        private async Task OnUserLeft(SocketGuildUser u)
        {
            SocketGuild Guild = _discord.GetGuild(311970313158262784);
            IMessageChannel Fax = Guild.GetTextChannel(358635970632876043);
            var builder = new EmbedBuilder()
               .WithAuthor(_discord.CurrentUser)
               .WithColor(new Color(255, 0, 0))
               .WithTitle("User left the server!")
               .WithDescription(u.Mention + " Left!")
               .WithThumbnailUrl(u.GetAvatarUrl())
               .WithCurrentTimestamp();
            await Fax.SendMessageAsync("", embed: builder.Build());
        }


        private async Task _discord_UserJoined(SocketGuildUser u)
        {
            SocketGuild Guild = _discord.GetGuild(311970313158262784);
            IRole Admin = Guild.GetRole(311989788540665857);
            IRole TrialAdmin = Guild.GetRole(364633182357815298);
            IMessageChannel ReceptionDesk = Guild.GetTextChannel(311974698839703562);
            IMessageChannel Fax = Guild.GetTextChannel(358635970632876043);

            var msg = await ReceptionDesk.SendMessageAsync("Welcome to the server " + u.Mention + "! \nPlease wait here while either a " + Admin.Mention + " or a " + TrialAdmin.Mention + " gives" +
                "you the Audience role! \nIn the meantime, make sure to read the rules on <#349026777852542986>!");
            var builder = new EmbedBuilder()
                .WithAuthor(_discord.CurrentUser)
                .WithColor(new Color(0, 255, 0))
                .WithTitle("User Joined the server!")
                .WithDescription(u.Mention + " Joined!")
                .WithThumbnailUrl(u.GetAvatarUrl())
                .WithCurrentTimestamp();
            await Fax.SendMessageAsync("", embed: builder.Build());
            await Task.Delay(TimeSpan.FromMinutes(3));
            await msg.DeleteAsync();
        }

        public async Task InitializeAsync(IServiceProvider provider)
        {
            _provider = provider;
            await _commands.AddModulesAsync(Assembly.GetEntryAssembly());
            // Add additional initialization code here...
        }

        private async Task MessageReceived(SocketMessage rawMessage)
        {
            // Ignore system messages and messages from bots
            if (!(rawMessage is SocketUserMessage message)) return;
            if (message.Source != MessageSource.User) return;
            var msg = rawMessage as SocketUserMessage;
            int argPos = 0;
            var context = new SocketCommandContext(_discord, message);

            if (msg.HasStringPrefix(_config["prefix"], ref argPos) || msg.HasMentionPrefix(_discord.CurrentUser, ref argPos))
            {
                var result = await _commands.ExecuteAsync(context, argPos, _provider);     // Execute the command

                if (!result.IsSuccess && result.Error != CommandError.UnknownCommand)
                {     // If not successful, reply with the error.
                    await msg.AddReactionAsync(Emote.Parse("<:RynnQuestion:365983788724912128>"));
                }
                if (!result.IsSuccess && result.Error == CommandError.UnknownCommand)
                {     // If not successful, reply with the error.
                    await msg.AddReactionAsync(Emote.Parse("<:RynnQuestion:365983788724912128>"));
                }
            }
            if (msg.Content.ToLower().StartsWith("hmmm"))
            {
                await msg.AddReactionAsync(Emote.Parse("<:Wyrthis:354398518586114049>"));
            }
            if (msg.Content.ToLower().Contains("robot") || msg.Content.ToLower().Contains("beep boop") || msg.Content.ToLower().Contains("beepboop") || msg.Content.ToLower().Contains("beep"))
            {
                await msg.AddReactionAsync(Emote.Parse("<:RynnLurk:365983787932319745>"));
            }
        }

    }
}
