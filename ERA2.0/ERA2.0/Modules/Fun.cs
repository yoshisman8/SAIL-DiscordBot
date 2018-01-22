using System;
using System.Collections.Generic;
using System.Text;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using LiteDB;
using System.Threading.Tasks;

namespace ERA20.Modules
{
    public class Fun : ModuleBase<SocketCommandContext>
    {
        public LiteDatabase Database { get; set; }
        public CommandService Service {get;set;}

        [Command("Ban")]
        [RequireContext(ContextType.Guild)]
        [Summary("'Bans' someone. Ussage: `$Ban <name>`")]
        public async Task Ban([Remainder] IUser _Target)
        {
            var result = await new CommandTimer().ValidateTimer(Context,Database,TimeSpan.FromMinutes(3),Service));

            if (result)
            {
                IRole Admins = Context.Guild.GetRole(311989788540665857);
                IRole trialadmin = Context.Guild.GetRole(364633182357815298);
                {
                    await Context.Channel.SendMessageAsync(_Target.Mention + " ur banne https://cdn.discordapp.com/attachments/314912846037254144/366611543263019009/ban1.png");
                }
            }
        }

        [Command("Nuke")]
        [RequireContext(ContextType.Guild)]
        [Summary("'Nuke' someone or something. Ussage: `$Nuke <Thing>`")]
        public async Task nuke([Remainder] string _Target)
        {
            var result = await new CommandTimer().ValidateTimer(Context,Database,TimeSpan.FromMinutes(3),Service));

            if (result)
            {
                {
                    await Context.Channel.SendMessageAsync("Deploying recreational nukes at " + _Target + "'s location! https://media.giphy.com/media/DAataETSDUXqo/giphy.gif");
                }
            }
        }

        [Command("Beep")]
        public async Task Beepboop()
        {
            var result = await new CommandTimer().ValidateTimer(Context,Database,TimeSpan.FromMinutes(3),Service));

            if (result)
            {
                await Context.Channel.SendMessageAsync("boop!");
            }
        }

        [Command("Boop")]
        public async Task Boobbeep()
        {
            var result = await new CommandTimer().ValidateTimer(Context,Database,TimeSpan.FromMinutes(3),Service));

            if (result.Result)
            {
                await Context.Channel.SendMessageAsync("I'm the one who boops! >:c");
            }
        }

        [Command("Hug")]
        [RequireContext(ContextType.Guild)]
        [Summary("Sends a hug to someone! Usage: `$Hug <name>`")]
        public async Task Hug([Remainder] IUser _User)
        {
            if (_User == null)
            {
                await Context.Channel.SendMessageAsync("I can't find this user!");
            }
            else
            {
                var result = await new CommandTimer().ValidateTimer(Context,Database,TimeSpan.FromMinutes(3),Service));

                if (result)
                {
                    IDMChannel dMChannel = await _User.GetOrCreateDMChannelAsync();
                    await Context.Channel.SendMessageAsync(Context.User.Mention + ", Hug sent successfully!");
                    await dMChannel.SendMessageAsync(Context.User.ToString() + " Sent you a hug!\n https://cdn.discordapp.com/attachments/314937091874095116/359130427136671744/de84426f25e6bf383afa8b5118b85770.gif");
                }
            }
        }

        [Command("kill")]
        public async Task Kill(string victim)
        {
            var result = await new CommandTimer().ValidateTimer(Context,Database,TimeSpan.FromMinutes(3),Service));

            if (result)
            {
                await Context.Channel.SendMessageAsync(Context.User.Mention+ " went 🔪🔪 on "+ victim);
            }
        }
    }
}
