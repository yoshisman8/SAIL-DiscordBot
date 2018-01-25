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
using Octokit;

namespace ERA20.Services
{
    public class CommandHandlingService
    {
        private readonly DiscordSocketClient _discord;
        private readonly CommandService _commands;
        private IConfiguration _config;
        private IServiceProvider _provider;
        private LiteDatabase _database;
        private GitHubClient _gitClient;

        private Toggles _toggles;

        public CommandHandlingService(IServiceProvider provider, DiscordSocketClient discord,IConfiguration config, CommandService commands, LiteDatabase database, GitHubClient gitHubClient, Toggles toggles)
        {
            _discord = discord;
            _commands = commands;
            _provider = provider;
            _database = database;
            _config = config;
            _gitClient = gitHubClient;
            _toggles = toggles;

            _discord.MessageReceived += MessageReceived;
            _discord.UserJoined += _discord_UserJoined;
            _discord.UserLeft += OnUserLeft;
            _discord.ReactionAdded += OnReact;
            _discord.GuildMemberUpdated += OnUserUpdate;
            _discord.Disconnected += OnDisconnect;
        }


        private async Task OnUserUpdate(SocketGuildUser OldUser, SocketGuildUser NewUser)
        {
            SocketGuild Guild = _discord.GetGuild(311970313158262784);
            var streamerzone = Guild.GetTextChannel(390769178946174976);
            var role = Guild.GetRole(314934868830191617);
            var general = Guild.GetTextChannel(311987726872215552);
            if (NewUser.Game.HasValue && NewUser.Game.Value.StreamType == StreamType.Twitch)
            {
                await streamerzone.SendMessageAsync(NewUser.Mention+" Is streaming **"+NewUser.Game.Value.Name+"**!" +
                    "\nYou can go watch them stream over at "+NewUser.Game.Value.StreamUrl);
            }
            if (!OldUser.Roles.Contains(role) && NewUser.Roles.Contains(role))
            {
                await general.SendMessageAsync("Welcome our new den member " + NewUser.Mention + "!");
            }
        }

        private async Task OnReact(Cacheable<IUserMessage, ulong> m, ISocketMessageChannel c, SocketReaction r)
        {
            var msg = await m.DownloadAsync();
            if (r.Emote.Name == "🗣")
            {
                var col = _database.GetCollection<Quote>("Quotes");
                Quote quote = new Quote
                {
                    Date = msg.Timestamp.DateTime,
                    Content = msg.Content,
                    Channel = c.Id,
                    User = msg.Author.Id,
                    Message = msg.Id
                };
                if (!col.Exists(x => x.Message == msg.Id))
                {
                    col.Insert(quote);
                    await msg.AddReactionAsync(new Discord.Emoji("💽"));
                }
            }
            else if (r.Emote.Name == "🔥")
            {
                await msg.AddReactionAsync(new Discord.Emoji("🚒"));
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

        private async Task OnDisconnect(Exception e){
            await _discord.LoginAsync(TokenType.Bot, _config["tokens:discord"]);
            await _discord.SetGameAsync(_config["status"]);
            await _discord.StartAsync();
        }
        private async Task MessageReceived(SocketMessage rawMessage)
        {
            // Ignore system messages and messages from bots
            if (!(rawMessage is SocketUserMessage message)) return;
            if (message.Source != MessageSource.User) return;
            var msg = rawMessage as SocketUserMessage;
            int argPos = 0;
            var context = new SocketCommandContext(_discord, message);

            var cmd = msg.Content.Substring(1).Split(' ').FirstOrDefault();

            if (msg.HasStringPrefix(_config["prefix"], ref argPos) || msg.HasMentionPrefix(_discord.CurrentUser, ref argPos))
            {

                if (_toggles.Slowmode && (cmd.ToLower() != "slowmode" || msg.Channel is IDMChannel)){
                    var response = await new CommandTimer().GobalValidate(context,_database,TimeSpan.FromMinutes(_toggles.Cooldown));
                    if (!response) return;
                }
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
