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
using LiteDB;

namespace ERA20.Modules
{
    public class Warning
    {
        [BsonId]
        public int ID { get; set; }
        public ulong Issuer { get; set; }
        public DateTime Date { get; set; }
        public ulong Outlier { get; set; }
        public string Reason { get; set; }
    }
    [Group("Warnings"), Alias("Warn", "Warning","Warns")]
    public class WarningService : ModuleBase<SocketCommandContext>
    {
        public DiscordSocketClient Client {get;set;}
        private IRole admins { get =>
        Client.GetGuild(311970313158262784).GetRole(405885961738780693);}
        public LiteDatabase Database { get; set; }

        [Command("Issue"), Alias("Give", "New", "Create")]
        [Summary("Admin command. Issue Warnings to a person.\nUsage: `/warn <person> <Reason>`")]
        [RequireUserPermission(GuildPermission.ManageRoles)]
        public async Task Warn(IUser Outlier, [Remainder] string _Reason)
        {
            {
                var col = Database.GetCollection<Warning>("Warnings");
                IMessageChannel staffLounge = Context.Guild.GetTextChannel(358635970632876043);
                

                var warning = new Warning();
                var User = Context.User as SocketGuildUser;
                {
                    warning.Date = DateTime.Now;
                    warning.Outlier = Outlier.Id;
                    warning.Issuer = Convert.ToUInt64(Context.User.Id.ToString());
                    warning.Reason = _Reason;

                    col.Insert(warning);
                    col.EnsureIndex(x => x.Outlier);

                    await ReplyAsync("Warning issued!");
                    await staffLounge.SendMessageAsync("", embed: EmbedWarning(warning));

                    if (col.Find(x => x.Outlier == Outlier.Id).Count() >= 3)
                    {
                        await ReplyAsync(admins.Mention + "! " + Outlier.Mention + " Has 3 or more warnings!");
                    }
                }
            }
        }
        [Command()]
        [RequireUserPermission(GuildPermission.ManageRoles)]
        public async Task Warning(IUser user)
        {
            
            if (user == null)
            {
                await Context.Channel.SendMessageAsync("Incorrect command ussage! Correct ussage is `/Warns <user>`");
            }
            else
            {
                IMessageChannel staffLounge = Context.Guild.GetTextChannel(364657346443739136);
                var col = Database.GetCollection<Warning>("Warnings");
                var User = Context.User as SocketGuildUser;

                var warns = col.Find(x => x.Outlier == user.Id);

                if (warns.Count() != 0 && Context.Channel == staffLounge)
                {
                    string msg = "User "+user.Username + " has "+warns.Count()+" warnings: \n";
                    foreach (Warning x in warns)
                    {
                        msg += "Warning ID #" + x.ID + " issued on " + x.Date+".\n";
                    }
                    await ReplyAsync(msg);
                    if (warns.Count() >= 3)
                    {
                        await staffLounge.SendMessageAsync(admins.Mention + "! " + user.Mention + " Has 3 or more warnings!");
                    }
                }
                else if (warns.Count() == 0)
                {
                    await Context.Channel.SendMessageAsync("`This user has no warnings.`");
                }
            }
        }

        [Command()]
        [RequireUserPermission(GuildPermission.ManageRoles)]
        public async Task getbyid(int ID)
        {
            var col = Database.GetCollection<Warning>("Warnings");

            var warn = col.FindById(ID);

            if (warn == null)
            {
                await ReplyAsync("This warnings doesn't exist!");
            }
            else
            {
                await ReplyAsync("", embed: EmbedWarning(warn));
            }
        }

        [Command("Delete"), Alias("remove")]
        [RequireUserPermission(GuildPermission.ManageRoles)]
        public async Task delet(int ID)
        {
            var col = Database.GetCollection<Warning>("Warnings");

            var warn = col.FindById(ID);

            if (warn == null)
            {
                await ReplyAsync("This warnings doesn't exist!");
            }
            else
            {
                col.Delete(warn.ID);
                await ReplyAsync("Warning ID #" + warn.ID + " issued to " + Context.Guild.GetUser(warn.Outlier).Mention + " Deleted!");
            }
        }


        public Embed EmbedWarning(Warning warning )
        {
            
            IUser outlier = Context.Guild.GetUser(warning.Outlier);
            IUser issuer = Context.Guild.GetUser(warning.Issuer);
            var builder = new EmbedBuilder()
                    .WithTitle("Warning form")
                    .WithDescription("Warning ID #"+warning.ID)
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
            return embed;
        }

    }
    public class ModerationTools : ModuleBase<SocketCommandContext>
    {
        [Command("Support")]
        [RequireContext(ContextType.DM)]
        [Summary("Sends an Annonymous message to the Admin's feedback channel. Usage: `/Support <Message>`")]
        public async Task Report([Remainder] string report = "")
        {
            {
                SocketGuild server = Context.Client.GetGuild(311970313158262784);
                IMessageChannel channel = server.GetTextChannel(358635970632876043);
                await EmbedReport(report, channel);
            }
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
    }
}
