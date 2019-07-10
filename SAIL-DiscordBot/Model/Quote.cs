using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using LiteDB;

using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.WebSocket;
using Discord.Rest;
using Discord;
using System.Net;
using System.Globalization;
using SAIL.Classes;
using System.Text;

namespace SAIL.Classes
{
    public class Quote
    {
        [BsonId]
        public ulong Message {get;set;}
        public ulong Channel {get;set;}
        public ulong Author {get;set;}
        public ulong Guild {get;set;}
        public string SearchText {get;set;}
        [BsonIgnore]
        public QuoteContext Context {get;set;} = new QuoteContext();
        
        public async Task GenerateContext(SocketCommandContext SCC)
        {
            Context.Guild = SCC.Client.GetGuild(Guild);
            if (Context.Guild == null) throw new Exception("Guild not found. I cannot access the server this quote is from. This is an unusual error, and you should contact my owner about this.");
            Context.Channel = Context.Guild.GetTextChannel(Channel);
            if (Context.Channel == null) throw new Exception("Channel not found. I cannot access the channel this quote is from. This can be due to me not having the Read Messages and Read Message History premissions on the channel this quote is from. Consider giving me these permissions in order to avoid this issue in the future.");
            Context.User = Context.Guild.GetUser(Author);
            var _msg = await Context.Channel.GetMessageAsync(Message);
            Context.Message = _msg as IUserMessage;
            if (Context.Message == null) throw new Exception("Message not found. I cannot seem to find this message. It might have been deleted or the message or due to me not having the Read Messages and Read Message History premissions on the channel this quote is from. Consider giving me these permissions in order to avoid this issue in the future.");
        }
    }

    public class QuoteContext
    {
        public IUserMessage Message {get;set;}
        public SocketTextChannel Channel {get;set;}
        public SocketGuildUser User {get;set;}
        public SocketGuild Guild {get;set;}
    }
}