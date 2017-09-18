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
    
    public class Warning
    {
        public int Id { get; set; }
        public ulong Issuer { get; set; }
        public DateTime Date { get; set; }
        public ulong Outlier { get; set; }
        public string Reason { get; set; }
    }
    public class WarningService : ModuleBase<SocketCommandContext>
    {
        [Command("Warn")]
        [Summary("Admin command. Issue Warnings to a person.\nUsage: `$warn <@person> <Reason>`")]
        public async Task Warn(IUser _Outlier, [Remainder] string _Reason)
        {

            Directory.CreateDirectory(@"Data/warnings/");
            IRole mods = Context.Guild.GetRole(356143807026298892);
            IMessageChannel channel = Context.Guild.GetTextChannel(324474414609727488);
            IDMChannel DM = await Context.User.GetOrCreateDMChannelAsync();

            ulong outlier = Convert.ToUInt64(_Outlier.Id.ToString());

            var warning = new Warning();
                
            var User = Context.User as SocketGuildUser;
            var role = User.Roles.Where(x => x.Id == mods.Id);
            if (role != null && (Context.Channel == channel || Context.Channel == DM))
            {
                warning.Date = DateTime.Now;
                warning.Outlier = outlier;
                warning.Issuer = Convert.ToUInt64(Context.User.Id.ToString());
                warning.Reason = _Reason;
                string json = JsonConvert.SerializeObject(warning);
                File.WriteAllText("Data/Warnings/"+_Outlier.Id+"/"+DateTime.UtcNow+".json", json);
                await EmbedWarning(warning);
                var totalwarns = Directory.EnumerateFiles("Data/Warnings/" + _Outlier.Id + "/");
                if (totalwarns.Count() >= 3)
                {
                    await channel.SendMessageAsync(Context.Guild.Owner.Mention + "! " + _Outlier.Mention + " Has 3 warnings!");
                }
            }
            else
            {
                await Context.Channel.SendMessageAsync("`You dont have permission to use this command or are using it in the incorrect channel!`");
            }
        }
        [Command("Warnings")]
        [Alias("Warns")]
        [Summary("Admin command, Display the warnings for a given person.\nUsage: `$warnings <person>`.")]
        public async Task Warning(IUser user)
        {
            IDMChannel dMChannel = await Context.User.GetOrCreateDMChannelAsync();

            IMessageChannel adminChannel = Context.Guild.GetTextChannel(324474414609727488);
                
            var User = Context.User as SocketGuildUser;

            Directory.CreateDirectory(@"Data/warnings/");

            var warns = Directory.EnumerateFiles("Data/Warnings/" + user.Id+ "/");

            var role = User.Roles.Where(x => x.Id == 356143807026298892);

            if (User != null && Context.Channel == adminChannel)
            {
                foreach (var x in warns)
                {
                    var warn = JsonConvert.DeserializeObject<Warning>(File.ReadAllText(x));
                    await EmbedWarning(warn);
                }
            }
            else if (User != null && warns == null)
            {
                await Context.Channel.SendMessageAsync("`This user has no warnings.`");
            }
            else
            {
                await Context.Channel.SendMessageAsync("`You dont have permission to use this command or are using it in the incorrect channel!`");
            }
        }
        [Command("Support")]
        [Alias("AdminDM")]
        [Summary("Send a message to adming staff anonymously. \nUsage: `$Support <Type> <Message>` \nType can me either 'S' for Suggestion or 'R' for Report/Complaint.")]
        public async Task Report(char type, [Remainder] string report)
        {
            ITextChannel adminChannel = Context.Guild.GetTextChannel(324474414609727488);
            switch (type)
            {
                case 's':
                    string x = "Suggestion";
                    await EmbedReport(report, x, adminChannel);
                    break;
                case 'r':
                    x = "Report/Complaing";
                    await EmbedReport(report, x, adminChannel);
                    break;
                default:
                    await Context.Channel.SendFileAsync("Invalid report type. Valid types are `s` for Suggestions or `r` for reports and complaints.");
                    break;
            }
        }
        public async Task EmbedWarning(Warning warning )
        {
            IUser outlier = Context.Guild.GetUser(warning.Outlier);
            IUser issuer = Context.Guild.GetUser(warning.Issuer);
            var builder = new EmbedBuilder()
                    .WithTitle("Warning form")
                    .WithDescription("Warning ID#"+warning.Id)
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
                    .AddField("Outlier:", outlier.Mention)
                    .AddField("Warning details:", warning.Reason);
            var embed = builder.Build();
            await Context.Channel.SendMessageAsync("", embed: embed)
            .ConfigureAwait(false);
        }
        public async Task EmbedReport(string report, string type, ITextChannel channel)
        {
            var builder = new EmbedBuilder()
            .WithTitle("New "+type)
            .WithDescription(report)
            .WithColor(new Color(0x663300))
            .WithTimestamp(DateTime.Now)
        	.WithAuthor(author => {
                 author
                .WithName("E.R.A. Anonymous report unit")
                .WithIconUrl(Context.Guild.CurrentUser.GetAvatarUrl());
            });
            var embed = builder.Build();
            await channel.SendMessageAsync("", embed: embed)
                .ConfigureAwait(false);
        }
    }
}
