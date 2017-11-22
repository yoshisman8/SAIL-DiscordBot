using Discord;
using Discord.Commands;
using Discord.Addons.Interactive;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using LiteDB;
using System.Threading.Tasks;

namespace ERA20
{
    public class Program
    {
        public static void Main(string[] args)
            => new Program().StartAsync().GetAwaiter().GetResult();

        private IConfigurationRoot _config;

        public async Task StartAsync()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile(@"_configuration.json");    // Begin building the configuration file  
            _config = builder.Build();                  // Build the configuration file
            var services = new ServiceCollection()      // Begin building the service provider
                .AddSingleton(new DiscordSocketClient(new DiscordSocketConfig     // Add the discord client to the service provider
                {
                    LogLevel = LogSeverity.Verbose,
                    MessageCacheSize = 1000     // Tell Discord.Net to cache 1000 messages per channel
                }))
                .AddSingleton(new CommandService(new CommandServiceConfig     // Add the command service to the service provider
                {
                    DefaultRunMode = RunMode.Async,     // Force all commands to run async
                    LogLevel = LogSeverity.Verbose
                }))
                .AddSingleton<CommandHandler>()     // Add remaining services to the provider
                .AddSingleton<LoggingService>()
                .AddSingleton<StartupService>()
                .AddSingleton<InteractiveService>()
                .AddSingleton<Random>()             // You get better random with a single instance than by creating a new one every time you need it
                .AddSingleton(_config)
                .AddSingleton(new LiteDatabase(@"Data\Database.db"));

            var provider = services.BuildServiceProvider();     // Create the service provider

            provider.GetRequiredService<LoggingService>();
            provider.GetRequiredService<CommandHandler>(); // Initialize the logging service, startup service, and command handler
            await provider.GetRequiredService<StartupService>().StartAsync();

            await Task.Delay(-1);     // Prevent the application from closing
        }
    }
}
