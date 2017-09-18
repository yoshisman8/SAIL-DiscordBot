using System;
using System.Collections.Generic;
using System.Text;
using Discord.Commands;
using Discord;
using Discord.WebSocket;
using System.Threading.Tasks;
using LiteDB;
using System.Linq;

namespace ERA.Modules
{
    public class TestModule : ModuleBase<SocketCommandContext>
    {
        [Command("ping")]
        public async Task Pinger()
        {
            await Context.Channel.SendMessageAsync("Your COMM link is operational, " + Context.User.Mention+"!");
        }
        [Command("Xsend")]
        public async Task Sendtoroom(ITextChannel channel, [Remainder] string message)
        {
            var User = Context.User as SocketGuildUser;
            var role = User.Roles.Where(x => x.Id == 356143807026298892);
            if (role != null) {
                var builder = new EmbedBuilder()
                    .WithDescription(message)
                    .WithColor(new Color(0x000000))
                    .WithTimestamp(DateTime.Now)
                    .WithFooter(footer =>
                    {
                        footer
                        .WithText(Context.User.ToString())
                        .WithIconUrl(Context.User.GetAvatarUrl());
                    })
                    .WithAuthor(author =>
                    {
                        author
                        .WithName("E.R.A. System Message")
                        .WithIconUrl(Context.Client.CurrentUser.GetAvatarUrl());
                    });
                var embed = builder.Build();
                await channel.SendMessageAsync(null, embed: embed)
                .ConfigureAwait(false);
            }
            else
            {
                await Context.Channel.SendMessageAsync("`You cannot use this Command!`");
            }
        }
        [Command("hug")]
        public async Task hug(IUser user)
        {
            IDMChannel dMChannel = await user.GetOrCreateDMChannelAsync();
            await dMChannel.SendMessageAsync(Context.User.ToString()+" Sent you a hug! https://cdn.discordapp.com/attachments/314937091874095116/359130427136671744/de84426f25e6bf383afa8b5118b85770.gif");
        }
        [Command("beep")]
        public async Task beepboop()
        {
            await Context.Channel.SendMessageAsync("boop!");
        }
    }
}
