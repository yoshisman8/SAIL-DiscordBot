using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Newtonsoft.Json;
using System.Linq;
using Discord.Commands;
using Discord.WebSocket;
using Discord;
using System.Threading.Tasks;
namespace ERA.Modules
{
    public class Warnlist
    {
        public ulong Outiler { get; set; }
        public List<Warning> Warns { get; set; } = new List<Warning>();
    }
    public class Warning
    {
        public ulong Issuer { get; set; }
        public DateTime Date { get; set; }
        public ulong Outlier { get; set; }
        public string Reason { get; set; }
    }
    public class WarningService : ModuleBase<SocketCommandContext>
    {
        [Command("Warn")]
        [Summary("Admin command. Issue Warnings to a person.\nUsage: `$warn <@person> <Reason>`")]
        public async Task Warn(string _Outlier = null, [Remainder] string _Reason = "")
        {
            IUser Outlier = GetUser(_Outlier);
            if (Outlier == null || _Reason == "")
            {
                await Context.Channel.SendMessageAsync("Incorrect command ussage! Correct ussage is `$Warn <User> <Reason>`.");
            }
            else
            {
                Directory.CreateDirectory(@"Data/warnings/");
                IRole Admins = Context.Guild.GetRole(311989788540665857);
                IRole trialadmin = Context.Guild.GetRole(364633182357815298);
                IMessageChannel staffLounge = Context.Guild.GetTextChannel(364657346443739136);

                Warnlist warnlist = new Warnlist();
                var warning = new Warning();

                if (File.Exists(@"Data/warnings/" + Outlier.Id.ToString() + ".json"))
                {
                    warnlist = JsonConvert.DeserializeObject<Warnlist>(File.ReadAllText(@"Data/warnings/" + Outlier.Id.ToString() + ".json"));
                }

                var User = Context.User as SocketGuildUser;

                if ((User.Roles.Contains(trialadmin) == true || User.Roles.Contains(Admins) == true) && Context.Channel == staffLounge)
                {
                    warning.Date = DateTime.Now;
                    warning.Outlier = Outlier.Id;
                    warning.Issuer = Convert.ToUInt64(Context.User.Id.ToString());
                    warning.Reason = _Reason;
                    warnlist.Outiler = Outlier.Id;
                    warnlist.Warns.Add(warning);

                    string json = JsonConvert.SerializeObject(warnlist);
                    File.WriteAllText(@"Data/warnings/" + Outlier.Id.ToString() + ".json", json);
                    await EmbedWarning(warning);
                    if (warnlist.Warns.Count >= 3)
                    {
                        await staffLounge.SendMessageAsync(Context.Guild.Owner.Mention + "! " + Outlier.Mention + " Has 3 warnings!");
                    }
                }
                else
                {
                    await Context.Channel.SendMessageAsync("`You dont have permission to use this command or are using it in the incorrect channel!`");
                }
            }
        }
        [Command("Warnings")]
        [Alias("Warns")]
        [Summary("Admin command, Display the warnings for a given person.\nUsage: `$warnings <person>`.")]
        public async Task Warning(string _User = null)
        {
            IUser user = GetUser(_User);
            if (user == null)
            {
                await Context.Channel.SendMessageAsync("Incorrect command ussage! Correct ussage is `$Warns <user>`");
            }
            else
            {
                IRole Admins = Context.Guild.GetRole(311989788540665857);
                IRole trialadmin = Context.Guild.GetRole(364633182357815298);
                IMessageChannel staffLounge = Context.Guild.GetTextChannel(364657346443739136);

                var User = Context.User as SocketGuildUser;

                Directory.CreateDirectory(@"Data/warnings/");

                var warnlist = JsonConvert.DeserializeObject<Warnlist>(File.ReadAllText(@"Data/warnings/" + user.Id.ToString() + ".json"));

                if ((User.Roles.Contains(trialadmin) == true || User.Roles.Contains(Admins) == true) && Context.Channel == staffLounge)
                {
                    foreach (Warning x in warnlist.Warns)
                    {
                        await EmbedWarning(x);
                    }
                    if (warnlist.Warns.Count >= 3)
                    {
                        await staffLounge.SendMessageAsync(Context.Guild.Owner.Mention + "! " + user.Mention + " Has 3 or more warnings!");
                    }
                }
                else if ((User.Roles.Contains(trialadmin) == true || User.Roles.Contains(Admins) == true) && warnlist.Warns == null)
                {
                    await Context.Channel.SendMessageAsync("`This user has no warnings.`");
                }
                else
                {
                    await Context.Channel.SendMessageAsync("`You dont have permission to use this command or are using it in the incorrect channel!`");
                }
            }
        }
        [Command("Support")]
        public async Task Report([Remainder] string report = "")
        {
            if (report == "")
            {
                await Context.Channel.SendMessageAsync("Incorrect command ussage! Correct ussage is `$Support <Message>`");
            }
            else
            {
                SocketGuild server = Context.Client.GetGuild(311970313158262784);
                IMessageChannel channel = server.GetTextChannel(358635970632876043);
                await EmbedReport(report, channel);
            }
        }
        public async Task EmbedWarning(Warning warning )
        {
            IMessageChannel channel = Context.Guild.GetTextChannel(358635970632876043);
            IUser outlier = Context.Guild.GetUser(warning.Outlier);
            IUser issuer = Context.Guild.GetUser(warning.Issuer);
            var builder = new EmbedBuilder()
                    .WithTitle("Warning form")
                    .WithDescription("Warning Issued!")
                    .WithColor(new Color(0xff0000))
                    .WithFooter(footer =>
                    {
                        footer
                            .WithText("Warning Issued by "+Context.Guild.GetUser(warning.Issuer).ToString()+" on: " + warning.Date)
                            .WithIconUrl(issuer.GetAvatarUrl());
                    })
                    .WithThumbnailUrl(outlier.GetAvatarUrl())
                    .WithAuthor(author => {
                        author
                            .WithName("E.R.A. MODERATOR UNIT")
                            .WithIconUrl(Context.Client.CurrentUser.GetAvatarUrl());
                    })
                    .AddField("Outlier:", outlier.Username)
                    .AddField("Warning details:", warning.Reason);
            var embed = builder.Build();
            await channel.SendMessageAsync("", embed: embed)
            .ConfigureAwait(false);
        }
        public async Task EmbedReport(string report, IMessageChannel channel)
        {
            SocketGuild server = Context.Client.GetGuild(311970313158262784);
            var builder = new EmbedBuilder()
            .WithTitle("New Suggestion/Report/Complaint!")
            .WithDescription(report)
            .WithColor(new Color(0x663300))
            .WithTimestamp(DateTime.Now)
        	.WithAuthor(author => {
                 author
                .WithName("E.R.A. Anonymous report unit")
                .WithIconUrl(server.CurrentUser.GetAvatarUrl());
            });
            var embed = builder.Build();
            await channel.SendMessageAsync("", embed: embed)
                .ConfigureAwait(false);
        }
        public ITextChannel GetTextChannel(string Name)
        {
            var channel = Context.Guild.Channels.Where(x => x.Name.ToLower() == Name);
            return channel.First() as ITextChannel;
        }
        public IUser GetUser(string name)
        {
            var user = Context.Guild.Users.Where(x => x.Username.ToLower().Contains(name));
            return user.First() as IUser;
        }
    }

}
