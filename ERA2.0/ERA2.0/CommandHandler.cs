using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Text;
using Discord.Commands;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;
using Discord.Addons.Interactive;


namespace ERABOT
{
    public class CommandHandler
    {
        private DiscordSocketClient _client;

        private CommandService _service;

        private IServiceProvider _Iservice;

        public async Task InitializeAsync(IServiceProvider client)
        {
            _client = client.GetRequiredService<DiscordSocketClient>(); ;

            _service = new CommandService();

            _Iservice = client;

            await _service.AddModulesAsync(Assembly.GetEntryAssembly());

            _client.MessageReceived += HandleCommandAsync;
        }

        private async Task HandleCommandAsync(SocketMessage s)
        {
            var msg = s as SocketUserMessage;
            if (msg == null) return;

            var context = new SocketCommandContext(_client, msg);
            int argPos = 0;
            if (msg.HasCharPrefix('$', ref argPos))
            {
                var result = await _service.ExecuteAsync(context, argPos, _Iservice);

                if (!result.IsSuccess && result.Error != CommandError.UnknownCommand)
                {
                    await context.Channel.SendMessageAsync("Error! "+result.Error.ToString());
                }
                else if (!result.IsSuccess && result.Error == CommandError.BadArgCount)
                {
                    await context.Channel.SendMessageAsync("`Too few argumnets!`");
                }
                else if (!result.IsSuccess && result.Error == CommandError.Exception)
                {
                    await context.Channel.SendMessageAsync("`An exception has occured!`");
                }
            }
        }
    }
}