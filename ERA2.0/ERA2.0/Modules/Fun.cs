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

        [Command("Ban")]
        [RequireContext(ContextType.Guild)]
        [Summary("'Bans' someone. Ussage: `$Ban <name>`")]
        public async Task Ban([Remainder] IUser _Target)
        {
            var col = Database.GetCollection<TimeoutControl>("Timeout");

            if (!col.Exists(x => x.UserID == Context.User.Id))
            {
                col.Insert(new TimeoutControl()
                {
                    UserID = Context.User.Id,
                    LastUse = DateTime.Now
                });
                col.EnsureIndex(x => x.UserID);
            }
            var user = col.FindOne(x => x.UserID == Context.User.Id);

            var result = VerifyTimer(user.LastUse, TimeSpan.FromMinutes(5));

            if (result.Result)
            {
                IRole Admins = Context.Guild.GetRole(311989788540665857);
                IRole trialadmin = Context.Guild.GetRole(364633182357815298);
                {
                    await Context.Channel.SendMessageAsync(_Target.Mention + " ur banne https://cdn.discordapp.com/attachments/314912846037254144/366611543263019009/ban1.png");
                }
            }
            else
            {
                var dm = await Context.User.GetOrCreateDMChannelAsync();
                await dm.SendMessageAsync("You need to wait " + result.Timer.Minutes + " Minutes and " + result.Timer.Seconds + " seconds to use this command again!");
            }
        }

        [Command("Nuke")]
        [RequireContext(ContextType.Guild)]
        [Summary("'Nuke' someone or something. Ussage: `$Nuke <Thing>`")]
        public async Task nuke([Remainder] string _Target)
        {
            var col = Database.GetCollection<TimeoutControl>("Timeout");

            if (!col.Exists(x => x.UserID == Context.User.Id))
            {
                col.Insert(new TimeoutControl()
                {
                    UserID = Context.User.Id,
                    LastUse = DateTime.Now
                });
                col.EnsureIndex(x => x.UserID);
            }
            var user = col.FindOne(x => x.UserID == Context.User.Id);

            var result = VerifyTimer(user.LastUse, TimeSpan.FromMinutes(5));

            if (result.Result)
            {
                {
                    await Context.Channel.SendMessageAsync("Deploying recreational nukes at " + _Target + "'s location! https://media.giphy.com/media/DAataETSDUXqo/giphy.gif");
                }
            }
            else
            {
                var dm = await Context.User.GetOrCreateDMChannelAsync();
                await dm.SendMessageAsync("You need to wait " + result.Timer.Minutes + " Minutes and " + result.Timer.Seconds + " seconds to use this command again!");
            }
        }

        [Command("Beep")]
        public async Task Beepboop()
        {
            var col = Database.GetCollection<TimeoutControl>("Timeout");

            if (!col.Exists(x => x.UserID == Context.User.Id))
            {
                col.Insert(new TimeoutControl()
                {
                    UserID = Context.User.Id,
                    LastUse = DateTime.Now
                });
                col.EnsureIndex(x => x.UserID);
            }
            var user = col.FindOne(x => x.UserID == Context.User.Id);

            var result = VerifyTimer(user.LastUse, TimeSpan.FromMinutes(1));

            if (result.Result)
            {
                await Context.Channel.SendMessageAsync("boop!");
            }
            else
            {
                var dm = await Context.User.GetOrCreateDMChannelAsync();
                await dm.SendMessageAsync("You need to wait " + result.Timer.Minutes + " Minutes and " + result.Timer.Seconds + " seconds to use this command again!");
            }
        }

        [Command("Boop")]
        public async Task Boobbeep()
        {
            var col = Database.GetCollection<TimeoutControl>("Timeout");

            if (!col.Exists(x => x.UserID == Context.User.Id))
            {
                col.Insert(new TimeoutControl()
                {
                    UserID = Context.User.Id,
                    LastUse = DateTime.Now
                });
                col.EnsureIndex(x => x.UserID);
            }
            var user = col.FindOne(x => x.UserID == Context.User.Id);

            var result = VerifyTimer(user.LastUse, TimeSpan.FromMinutes(1));

            if (result.Result)
            {
                await Context.Channel.SendMessageAsync("I'm the one who boops! >:c");
            }
            else
            {
                var dm = await Context.User.GetOrCreateDMChannelAsync();
                await dm.SendMessageAsync("You need to wait " + result.Timer.Minutes + " Minutes and " + result.Timer.Seconds + " seconds to use this command again!");
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
                var col = Database.GetCollection<TimeoutControl>("Timeout");

                if (!col.Exists(x => x.UserID == Context.User.Id))
                {
                    col.Insert(new TimeoutControl()
                    {
                        UserID = Context.User.Id,
                        LastUse = DateTime.Now
                    });
                    col.EnsureIndex(x => x.UserID);
                }
                var user = col.FindOne(x => x.UserID == Context.User.Id);

                var result = VerifyTimer(user.LastUse, TimeSpan.FromMinutes(5));

                if (result.Result)
                {
                    IDMChannel dMChannel = await _User.GetOrCreateDMChannelAsync();
                    await Context.Channel.SendMessageAsync(Context.User.Mention + ", Hug sent successfully!");
                    await dMChannel.SendMessageAsync(Context.User.ToString() + " Sent you a hug!\n https://cdn.discordapp.com/attachments/314937091874095116/359130427136671744/de84426f25e6bf383afa8b5118b85770.gif");
                }
                else
                {
                    var dm = await Context.User.GetOrCreateDMChannelAsync();
                    await dm.SendMessageAsync("You need to wait " + result.Timer.Minutes + " Minutes and " + result.Timer.Seconds + " seconds to use this command again!");
                }
            }
        }

        [Command("kill")]
        public async Task Kill(string victim)
        {
            var col = Database.GetCollection<TimeoutControl>("Timeout");

            if (!col.Exists(x => x.UserID == Context.User.Id))
            {
                col.Insert(new TimeoutControl()
                {
                    UserID = Context.User.Id,
                    LastUse = DateTime.Now
                });
                col.EnsureIndex(x => x.UserID);
            }
            var user = col.FindOne(x => x.UserID == Context.User.Id);

            var result = VerifyTimer(user.LastUse, TimeSpan.FromMinutes(5));

            if (result.Result)
            {
                await Context.Channel.SendMessageAsync(Context.User.Mention+ " went 🔪🔪 on "+ victim);
            }
            else
            {
                var dm = await Context.User.GetOrCreateDMChannelAsync();
                await dm.SendMessageAsync("You need to wait " + result.Timer.Minutes + " Minutes and " + result.Timer.Seconds + " seconds to use this command again!");
            }
        }

        internal TimerResult VerifyTimer(DateTime date, TimeSpan Minutes)
        {
            var x = DateTime.Now-date;
            if (x.Minutes >= Minutes.Minutes)
            {
                return new TimerResult()
                {
                    Result = true
                };
            }
            else
            {
                return new TimerResult()
                {
                    Timer = Minutes - x,
                    Result = false
                };
            }
        }
    }

    internal class TimerResult
    {
        public bool Result { get; set; } = false;
        public TimeSpan Timer { get; set; } = new TimeSpan();
    }
    public class TimeoutControl
    {
        [BsonId]
        public ulong UserID { get; set; }
        public DateTime LastUse { get; set; } = DateTime.Now;
    }

}
