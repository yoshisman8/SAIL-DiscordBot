using System;
using Discord;
using Discord.WebSocket;
using System.Threading.Tasks;
using System.IO;
using LiteDB;
using Newtonsoft.Json;
using Microsoft.Extensions.DependencyInjection;
using Discord.Addons.Interactive;

namespace ERABOT
{

    public class Program
    {
        static void Main(string[] args)
        => new Program().StartAsync().GetAwaiter().GetResult();

        private DiscordSocketClient _client;

        private CommandHandler _handler;

        public async Task StartAsync()
        {
            _client = new DiscordSocketClient();

            _handler = new CommandHandler();
            
            string token = File.ReadAllText("token.txt");

            string game = File.ReadAllText("game.txt");

            Directory.CreateDirectory(@"Data/");

            await _client.LoginAsync(TokenType.Bot, token);

            await _client.StartAsync();

            var services = ConfigureServices();

            await _handler.InitializeAsync(services);

            await _client.SetGameAsync(game);
            
            await Task.Delay(-1);
        }
        private IServiceProvider ConfigureServices()
        {
            return new ServiceCollection()
                .AddSingleton(_client)
                .AddSingleton(new InteractiveService(_client))
                .BuildServiceProvider();
        }

    }

}