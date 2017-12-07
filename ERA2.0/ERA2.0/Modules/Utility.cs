using Discord.Commands;
using Discord;
using Discord.WebSocket;
using System.Linq;
using LiteDB;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Collections.Generic;

namespace ERA20.Modules
{
    [Name("Miscellaneus")]
    [Summary("Random or otherwise fun commands with little real use.")]
    public class TestModule : ModuleBase<SocketCommandContext>
    {
        public LiteDatabase Database { get; set; }

        [Command("Test")]
        public async Task Test([Remainder] string text)
        {
            try
            {
                var col = Database.GetCollection<string>("Test");
                col.Insert(text);
                
                //var x = col.FindOne(y => y == text);
                await ReplyAsync("Saved " + text + " Successfully!");
            }
            catch (Exception e)
            {
                await ReplyAsync(e.ToString());
            }

        }
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
        [Command("Ban")]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.ManageRoles)]
        [Summary("'Bans' someone. Ussage: `$Ban <name>`")]
        public async Task Ban([Remainder] string _Target)
        {
            IRole Admins = Context.Guild.GetRole(311989788540665857);
            IRole trialadmin = Context.Guild.GetRole(364633182357815298);
            var User = Context.User as SocketGuildUser;
            IUser Target = GetUser(_Target);
            if (Target is null)
            {
                await Context.Channel.SendMessageAsync(_Target + " ur banne https://cdn.discordapp.com/attachments/314912846037254144/366611543263019009/ban1.png");
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
            if (User is null)
            {
                await Context.Channel.SendMessageAsync("I can't find this user!");
            }
            else
            {
                IDMChannel dMChannel = await User.GetOrCreateDMChannelAsync();
                await Context.Channel.SendMessageAsync(Context.User.Mention + ", Hug sent successfully!");
                await dMChannel.SendMessageAsync(Context.User.ToString() + " Sent you a hug!\n https://cdn.discordapp.com/attachments/314937091874095116/359130427136671744/de84426f25e6bf383afa8b5118b85770.gif");
            }
        }
        [Command("Pause")]
        [RequireContext(ContextType.Guild)]
        [Summary("Creates a Pause code for the last 5 messagse sent. Usage: `$Pause <Code>`.")]
        public async Task Pause([Remainder] string _Code)
        {
            var code = new PauseCode()
            {
                Code = _Code,
                Channel = Context.Channel.Id
            };
            var History = await Context.Channel.GetMessagesAsync(fromMessage: Context.Message, dir: Direction.Before, limit: 5).Flatten();
            foreach (IMessage x in History)
            {
                code.Messages.Add(x.Id);
            }
            code.Save();
            await Context.Channel.SendMessageAsync("------------------`Pause code: " + code.Code + "`------------------");
        }
        [Command("Resume")]
        [RequireContext(ContextType.Guild)]
        [Summary("Load a Pause code. Usage: `$Resume <Code>`.")]
        public async Task Resume([Remainder] string _Code)
        {
            var code = new PauseCode().GetPauseCode(_Code);
            if (code == null)
            {
                await Context.Channel.SendMessageAsync("I couldn't find that code, or multiple codes were found. Please type the *entire* Pause Code for me to look it up!");
            }
            else
            {
                await code.GenerateListAsync(Context);
                foreach (var x in code.IMessages)
                {
                    var builder = new EmbedBuilder()
                    .WithFooter("E.R.A. Pause Code clerk.", Context.Client.CurrentUser.GetAvatarUrl())
                    .WithColor(new Color(0, 210, 210))
                    .WithAuthor(x.Author)
                    .WithDescription(x.Content + "\n")
                    .WithTimestamp(x.Timestamp);
                    await Context.Channel.SendMessageAsync("", embed: builder.Build());
                }
                code.Delete();
            }
        }
        [Command("Codes")]
        [Summary("Shows all the currently stored pause codes.")]
        public async Task GetAllCodes()
        {
            var codes = new PauseCode().GetAllCodes();
            string msg = "Here are all the pause codes available as of right now: ";
            foreach (PauseCode x in codes)
            {
                msg += "`" + x.Code + "` ";
            }
            await ReplyAsync(msg);
        }
        [Command("Avatar")]
        [Alias("Avi","Icon")]
        [RequireContext(ContextType.Guild)]
        [Summary("Returns someone's avatar URL. Usage: `$Avatar <User>`. You dont have to mention the user")]
        public async Task Avatar([Remainder] string User)
        {
            var user = GetUser(User);            
            await Context.Channel.SendMessageAsync(user.GetAvatarUrl().Replace("?size=128", ""));
        }
        public ITextChannel GetTextChannel(string Name)
        {
            var channel = Context.Guild.Channels.Where(x => x.Name.ToLower() == Name.ToLower());
            return channel.First() as ITextChannel;
        }
        public IUser GetUser(string name)
        {
            var user = Context.Guild.Users.Where(x => x.Username.ToLower().Contains(name.ToLower()));
            if (user.Count() == 0) { return null; }
            else { return user.First() as IUser; }
        }
        
        public class PauseCode
        {
            public string Code { get; set; }
            public ulong Channel { get; set; }
            public List<ulong> Messages { get; set; } = new List<ulong> { };
            
            [JsonIgnore]
            public IOrderedEnumerable<IMessage> IMessages { get; set; }
            public async Task GenerateListAsync(SocketCommandContext context)
            {
                var channel = context.Guild.GetTextChannel(Channel);
                List<IMessage> unsorted = new List<IMessage> { };
                foreach (ulong x in Messages)
                {
                    unsorted.Add(await channel.GetMessageAsync(x));
                }
                IMessages = unsorted.OrderBy(x => x.Timestamp);
            }
            public void Save()
            {
                Directory.CreateDirectory(@"Data/Codes/");
                string json = JsonConvert.SerializeObject(this);
                File.WriteAllText(@"Data/Codes/" + Code + ".json", json);
            }
            public PauseCode GetPauseCode(string Code)
            {
                Directory.CreateDirectory(@"Data/Codes/");
                var files = Directory.EnumerateFiles(@"Data/Codes/");
                List<PauseCode> Codes = new List<PauseCode> { };
                foreach (string x in files)
                {
                    Codes.Add(JsonConvert.DeserializeObject<PauseCode>(File.ReadAllText(x)));
                }
                var query = Codes.Where(x => x.Code.ToLower() == Code.ToLower());
                if (query.Count() == 1)
                {
                    return query.First();
                }
                else
                {
                    return null;
                }
            }
            public void Delete()
            {
                File.Delete(@"Data/Codes/" + Code + ".json");
            }
            public List<PauseCode> GetAllCodes()
            {
                Directory.CreateDirectory(@"Data/Codes/");
                var files = Directory.EnumerateFiles(@"Data/Codes/");
                List<PauseCode> Codes = new List<PauseCode> { };
                foreach (string x in files)
                {
                    Codes.Add(JsonConvert.DeserializeObject<PauseCode>(File.ReadAllText(x)));
                }
                return Codes;
            }
        }
    }
    
}
