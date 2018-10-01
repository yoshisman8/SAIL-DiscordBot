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
using CommonMark;
using System.Text;

namespace ERA20.Modules
{
    [Name("Miscellaneus")]
    [Summary("Random or otherwise fun commands with little real use.")]
    public class TestModule : ModuleBase<SocketCommandContext>
    {
        public LiteDatabase Database { get; set; }
        public IConfiguration Config { get; set; }

        [Command("Status")]
        [RequireUserPermission(GuildPermission.ManageGuild)]
        [Summary("Set the bot's 'Playing' status. Usage: `/Status <text>`")]
        public async Task StatusSet([Remainder] string _text)
        {
            await Context.Client.SetGameAsync(_text);
        }
       
        [Command("Xsend")]
        [RequireUserPermission(GuildPermission.ManageRoles)]
        [RequireContext(ContextType.Guild)]
        [Summary("Sends a message to specific channel under ERA's name. Ussage: /Xsend <Channel> <Message>")]
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
        [Command("Pause")]
        [RequireContext(ContextType.Guild)]
        [Summary("Creates a Pause code for the last 5 messagse sent. Usage: `/Pause <Code>`.")]
        public async Task Pause([Remainder] string _Code)
        {
            var code = new PauseCode()
            {
                Code = _Code,
                Channel = Context.Channel.Id
            };
            var History = await Context.Channel.GetMessagesAsync(fromMessage: Context.Message, dir: Direction.Before, limit: 5).FlattenAsync();
            foreach (IMessage x in History)
            {
                code.Messages.Add(x.Id);
            }
            code.Save();
            await Context.Channel.SendMessageAsync("------------------`Pause code: " + code.Code + "`------------------");
        }

        [Command("Resume")]
        [RequireContext(ContextType.Guild)]
        [Summary("Load a Pause code. Usage: `/Resume <Code>`.")]
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
        [Command("Preview")]
        [RequireContext(ContextType.Guild)]
        [Summary("Preview a Pause code in DMs. Usage: `/Preview <Code>`.")]
        public async Task preview([Remainder] string _Code)
        {
            var code = new PauseCode().GetPauseCode(_Code);
            if (code == null)
            {
                await Context.Channel.SendMessageAsync("I couldn't find that code, or multiple codes were found. Please type the *entire* Pause Code for me to look it up!");
            }
            else
            {
                var dms = await Context.User.GetOrCreateDMChannelAsync();
                await code.GenerateListAsync(Context);
                foreach (var x in code.IMessages)
                {
                    var builder = new EmbedBuilder()
                    .WithFooter("E.R.A. Pause Code clerk.", Context.Client.CurrentUser.GetAvatarUrl())
                    .WithColor(new Color(0, 210, 210))
                    .WithAuthor(x.Author)
                    .WithDescription(x.Content + "\n")
                    .WithTimestamp(x.Timestamp);
                    await dms.SendMessageAsync("", embed: builder.Build());
                }
                await ReplyAsync("You've been DM'd this code for previewing!");
            }
        }
        [Command("Avatar")]
        [Alias("Avi","Icon")]
        [RequireContext(ContextType.Guild)]
        [Summary("Returns someone's avatar URL. Usage: `/Avatar <User>`. You dont have to mention the user")]
        public async Task Avatar([Remainder] IUser User)
        {
            var user = Context.Client.GetUser(User.Id);
            await Context.Channel.SendMessageAsync(user.GetAvatarUrl().Replace("?size=128", ""));
            
        }
        [Command("User"), Alias("Whois","UserStats")]
        [RequireContext(ContextType.Guild)]
        public async Task whois(IUser Username)
        {
            var user = Username as SocketGuildUser;
            var builder = new EmbedBuilder()
                .WithAuthor(Context.Client.CurrentUser)
                .WithColor(new Color(0, 0, 255))
                .WithTitle(user.Nickname + " [" + user.Username +"#"+ user.Discriminator+ "]")
                .WithThumbnailUrl(user.GetAvatarUrl())
                .WithUrl(user.GetAvatarUrl())
                .AddField("Id", user.Id, true)
                .AddField("Account created at", user.CreatedAt.Month+"/"+user.CreatedAt.Day+"/"+user.CreatedAt.Year, true)
                .AddField("Joined the server at", user.JoinedAt.Value.Month + "/" +user.JoinedAt.Value.Day + "/" +user.JoinedAt.Value.Year, true)
                .AddField("Roles", Buildroles(user), true)
                .AddField("Playing", Gamebuilder(user),true)
                .AddField("Other data", miscbuilder(user),true);
            await ReplyAsync("", embed: builder.Build());
        }
        [Command("MessagePurge"), Alias("Delete","Deletdis")]
        [RequireUserPermission(ChannelPermission.ManageMessages)]
        [Summary("Deletes an X amount of messages without any filters. Usage: `/MessagePurge <Ammount (Starting from your command)>`")]
        public async Task Delete(int Amount){
            ulong checkpoint = Context.Message.Id;
            for (int i = 1; i <= Amount; i++){
                var msg = await Context.Channel.GetMessagesAsync(checkpoint,Direction.Before,1).FlattenAsync();
                checkpoint = msg.First().Id;
                await msg.First().DeleteAsync();
                await Task.Delay(800);
            }
            await ReplyAsync("Successfully deleted "+Amount+" Messages");
        }
        [Command("DeleteFrom")]
        [RequireUserPermission(ChannelPermission.ManageMessages)]
        [Summary("Deletes messages form a specific users. Usage: `/DeleteFrom <Amount of messages (Starting from your command)> <Specified users, seprated by a space>`")]
        public async Task DeleteFrom(int Amount, params IUser[] _users){
            var msgs = new List<IMessage>();
            var users = _users.ToList();
            int Count = 0;
            ulong checkpoint = Context.Message.Id;
            for (int i = 1; i <= Amount; i++){
                var msg = await Context.Channel.GetMessagesAsync(checkpoint,Direction.Before,1).FlattenAsync();
                checkpoint = msg.First().Id;
                if(users.Exists(x => x.Id == msg.First().Author.Id)) {await msg.First().DeleteAsync(); Count++;}
                await Task.Delay(800);
            }
            await ReplyAsync("Successfully deleted "+Count+" messages.");
        }
        [Command("DeleteIf"), Alias("KeywordDelete")]
        [RequireUserPermission(ChannelPermission.ManageMessages)]
        [Summary("Deletes messages that contain a word or phrase (Be careful with this one). Usage: `/DeleteFrom <Amount of messages (Starting from your command)> <Keyword>`")]
        public async Task DeleteFrom(int Amount,[Remainder] string Keyword){
            var msgs = new List<IMessage>();
            int Count = 0;
            ulong checkpoint = Context.Message.Id;
            for (int i = 1; i <= Amount; i++){
                var msg = await Context.Channel.GetMessagesAsync(checkpoint,Direction.Before,1).FlattenAsync();
                checkpoint = msg.First().Id;
                if(msg.First().Content.ToLower().Contains(Keyword.ToLower())) {await msg.First().DeleteAsync(); Count++;}
                await Task.Delay(800);
            }
            await ReplyAsync("Successfully deleted "+Count+" messages.");
        }
        [Command("Log"), Alias("GenerateLog")]
        [Summary("Generates a dated word document of a chatroom. Logs take a while to generate. Please be patient! Usage: /log")]
        [RequireUserPermission(ChannelPermission.ManageMessages)]
        public async Task LogChannel(){
            await Context.Client.SetGameAsync("Currnetly Logging: "+Context.Channel.Name);
            await Context.Channel.TriggerTypingAsync();
            var Messages = new List<IMessage>().AsEnumerable();
            var Buffer = new List<IMessage>().AsEnumerable();

            Messages = Messages.Concat(await Context.Channel.GetMessagesAsync(Context.Message, Discord.Direction.Before, 100).FlattenAsync());
            bool loop = true;
            do{
                Buffer = await Context.Channel.GetMessagesAsync(Messages.Last(), Discord.Direction.Before, 100).FlattenAsync();
                if (Buffer.Count() == 0) {loop = false;}
                else {Messages = Messages.Concat(Buffer); await Task.Delay(TimeSpan.FromSeconds(1));}
            } while (loop);
            var file = File.CreateText(Directory.GetCurrentDirectory()+"/"+Context.Channel.Name+".html");
            var output = new StringBuilder();
            output.AppendLine(Context.Channel.Name+"\n\n");
            foreach(var x in Messages.Reverse()){
                output.AppendLine("\n["+x.Author.Username+"] "+x.Content);
                if (x.Attachments.Count >0){
                    output.Append(" [Attached: "+x.Attachments.First().Url+"]");
                }
            }
            file.Write(CommonMarkConverter.Convert(output.ToString()));
            file.Close();
            await Context.Channel.SendFileAsync(Directory.GetCurrentDirectory()+"/" + Context.Channel.Name + ".html","Channel logged Successfully! Here is your log file, Enjoy!");
            await Context.Message.DeleteAsync();
            File.Delete(Directory.GetCurrentDirectory()+"/"+Context.Channel.Name+".html");
            await Context.Client.SetGameAsync(Config["status"]);
        }
        public string Buildroles(SocketGuildUser User)
        {
            string roles = "";
            foreach (SocketRole X in User.Roles)
            {
                roles += X.Mention + ", ";
            }
            return roles.Remove(roles.Length - 2) + ".";
        }
        public string Gamebuilder(SocketGuildUser user)
        {
            
            if (user.Activity == null)
            {
                return "This user isn't playing anything at the moment.";
            }
            else
            {
                var game = user.Activity;
                if ((game.Type == ActivityType.Streaming))
                {
                    var stream = game as StreamingGame;
                    return user.Username + " is streaming **" + stream.Name + "** over at " + stream.Url+".";
                }
                else if (game.Type == ActivityType.Playing)
                {
                    return user.Username + " Is playing **" + game.Name + "**.";
                }
                else{
                    return "This user isn't playing anything at the moment.";
                }
            }
        }
        public string miscbuilder(SocketGuildUser user)
        {
            string msg = "";

            msg += ((user.IsSelfMuted || user.IsSelfDeafened) || (user.IsMuted || user.IsDeafened)) ? "Mute status: :mute:\n" : "Mute status: :speaker:\n";
            msg += (user.IsBot) ? "This user is a bot :robot:\n" : "This user is a human :bust_in_silhouette:\n";
            msg += user.IsSuppressed ? "This user is Suppressed!" : "This user is not suppresed.";
            return msg;

        }
        public ITextChannel GetTextChannel(string Name)
        {
            var channel = Context.Guild.Channels.Where(x => x.Name.ToLower() == Name.ToLower());
            return channel.First() as ITextChannel;
        }
        public SocketGuildUser GetUser(string name)
        {
            var user = Context.Guild.Users.Where(x => x.Username.ToLower().Contains(name.ToLower()));
            if (user.Count() == 0) { return null; }
            else { return user.First(); }
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
