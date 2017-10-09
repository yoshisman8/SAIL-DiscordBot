using System;
using System.Collections.Generic;
using System.Text;
using Discord.Commands;
using Discord;
using Discord.WebSocket;
using System.Threading.Tasks;
using System.Linq;

namespace ERA.Modules
{
    public class TestModule : ModuleBase<SocketCommandContext>
    {
        [Command("ping")]
        public async Task Pinger()
        {
            await Context.Channel.SendMessageAsync("I can hear you loud and clear, " + Context.User.Mention+"!");
        }
        [Command("Xsend")]
        public async Task Sendtoroom(ITextChannel channel, [Remainder] string message)
        {
            var User = Context.User as SocketGuildUser;
            IRole Admins = Context.Guild.GetRole(311989788540665857);
            IRole trialadmin = Context.Guild.GetRole(364633182357815298);
            IRole DMs = Context.Guild.GetRole(324320068748181504);

            if (User.Roles.Contains(Admins) == true || User.Roles.Contains(trialadmin) == true || User.Roles.Contains(DMs) == true) {
                var builder = new EmbedBuilder()
                    .WithDescription(message)
                    .WithColor(new Color(0x000000))
                    .WithTimestamp(DateTime.Now)
                    .WithAuthor(author =>
                    {
                        author
                        .WithName("E.R.A. System Message")
                        .WithIconUrl(Context.Client.CurrentUser.GetAvatarUrl());
                    });
                var embed = builder.Build();
                await Context.Channel.SendMessageAsync("Message Sent Successfully!");
                await channel.SendMessageAsync("", embed: embed)
                .ConfigureAwait(false);
            }
            else
            {
                await Context.Channel.SendMessageAsync("`You cannot use this Command!`");
            }
        }
        [Command ("modkill")]
        public async Task Modkill(string _Victim)
        {
            await Context.Channel.SendMessageAsync("```fix\nBoom!```\n" + _Victim + " Is now dead.");
        }
        [Command("Ban")]
        public async Task Ban(IUser _Target)
        {
            IRole Admins = Context.Guild.GetRole(311989788540665857);
            IRole trialadmin = Context.Guild.GetRole(364633182357815298);
            var User = Context.User as SocketGuildUser;
            if (User.Roles.Contains(Admins) == true || User.Roles.Contains(trialadmin) == true)
            {
                await Context.Channel.SendMessageAsync(_Target.Mention + " ur banne https://cdn.discordapp.com/attachments/314912846037254144/366611543263019009/ban1.png");
            }
            else
            {
                await Context.Channel.SendMessageAsync("You don't have the power for that!");
            }
        }
        [Command("beep")]
        public async Task Beepboop()
        {
            await Context.Channel.SendMessageAsync("boop!");
        }
        [Command("boop")]
        public async Task Boobbeep()
        {
            await Context.Channel.SendMessageAsync("Im the one who boops! >:c");
        }
        [Command("hug")]
        public async Task Hug(IUser user)
        {
            IDMChannel dMChannel = await user.GetOrCreateDMChannelAsync();
            await Context.Channel.SendMessageAsync(Context.User.Mention+", Hug sent successfully!");
            await dMChannel.SendMessageAsync(Context.User.ToString()+ " Sent you a hug!\n https://cdn.discordapp.com/attachments/314937091874095116/359130427136671744/de84426f25e6bf383afa8b5118b85770.gif");
        }
    }
}
