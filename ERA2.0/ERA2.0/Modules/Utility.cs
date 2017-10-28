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
    [Name("Miscellaneus")]
    [Summary("Random or otherwise fun commands with little real use.")]
    public class TestModule : ModuleBase<SocketCommandContext>
    {
        [Command("Status")]
        [RequireUserPermission(GuildPermission.ManageGuild)]
        [Summary("Set the bot's 'Playing' status. Usage: `$Status <text>`")]
        public async Task StatusSet([Remainder] string _text)
        {
            await Context.Client.SetGameAsync(_text);
        }
        [Command("Xsend")]
        [RequireUserPermission(GuildPermission.ManageRoles)]
        [RequireContext(ContextType.Guild)]
        [Summary("Sends a message to specific channel under ERA's name. Ussage: $Xsend <Channel> <Message>")]
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
        [Command ("ModKill")]
        [Summary("'Kills' Someone. Usage: `$modkill <target>`")]
        [RequireUserPermission(GuildPermission.ManageRoles)]
        public async Task Modkill([Remainder] string _Victim)
        {
            await Context.Channel.SendMessageAsync("```fix\nBoom!```\n" + _Victim + " Is now dead.");
        }
        [Command("Ban")]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.ManageRoles)]
        [Summary("'Bans' someone. Ussage: `$Ban <name>`")]
        public async Task Ban([Remainder] string  _Target)
        {
            IRole Admins = Context.Guild.GetRole(311989788540665857);
            IRole trialadmin = Context.Guild.GetRole(364633182357815298);
            var User = Context.User as SocketGuildUser;
            IUser Target = GetUser(_Target);
                if (Target == null)
                {
                    await Context.Channel.SendMessageAsync(_Target+ " ur banne https://cdn.discordapp.com/attachments/314912846037254144/366611543263019009/ban1.png");
                }
                else
                {
                    await Context.Channel.SendMessageAsync(Target.Mention + " ur banne https://cdn.discordapp.com/attachments/314912846037254144/366611543263019009/ban1.png");
                }
        }
        [Command("Beep")]
        public async Task Beepboop()
        {
            await Context.Channel.SendMessageAsync("boop!");
        }
        [Command("Boop")]
        public async Task Boobbeep()
        {
            await Context.Channel.SendMessageAsync("I'm the one who boops! >:c");
        }
        [Command("Hug")]
        [RequireContext(ContextType.Guild)]
        [Summary("Sends a hug to someone! Usage: `$Hug <name>`")]
        public async Task Hug([Remainder] string _User)
        {
            IUser User = GetUser(_User);
            if ( User != null)
            {
                IDMChannel dMChannel = await User.GetOrCreateDMChannelAsync();
                await Context.Channel.SendMessageAsync(Context.User.Mention + ", Hug sent successfully!");
                await dMChannel.SendMessageAsync(Context.User.ToString() + " Sent you a hug!\n https://cdn.discordapp.com/attachments/314937091874095116/359130427136671744/de84426f25e6bf383afa8b5118b85770.gif");
            }
            else
            {
                await Context.Channel.SendMessageAsync("I can't find this user!");
            }
        }
        public ITextChannel GetTextChannel(string Name)
        {
            var channel = Context.Guild.Channels.Where(x => x.Name.ToLower() == Name.ToLower());
            return channel.First() as ITextChannel;
        }
        public IUser GetUser(string name)
        {
            var user = Context.Guild.Users.Where(x => x.Username.ToLower().Contains(name.ToLower()));
            return user.First() as IUser;
        }

    }

}
