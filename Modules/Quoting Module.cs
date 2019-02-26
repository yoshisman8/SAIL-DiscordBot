using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using LiteDB;
using Discord.Addons.CommandCache;
using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.WebSocket;
using Discord.Rest;
using Discord;
using System.Net;
using System.Globalization;
using SAIL.Classes;
using System.Text;

namespace SAIL.Modules 
{
    public class Quote
    {
        [BsonId]
        public ulong Message {get;set;}
        public ulong Channel {get;set;}
        public ulong Author {get;set;}
        public ulong Guild {get;set;}
        public QuoteType Type {get;set;}
        public string SearchText {get;set;}
        public List<QuoteReaction> Reactions {get;set;} = new List<QuoteReaction>();
        [BsonIgnore]
        public QuoteContext QContext {get;set;}
        
        public async Task GenerateContext(SocketCommandContext context)
        {
            QContext.Guild = context.Client.GetGuild(Guild);
            if (QContext.Guild == null) throw new Exception("Guild not found. I cannot access the server this quote is from. This is an unusual error, and you should contact my owner about this.");
            QContext.Channel = QContext.Guild.GetTextChannel(Channel);
            if (QContext.Channel == null) throw new Exception("Channel not found. I cannot access the channel this quote is from. This can be due to me not having the Read Messages and Read Message History premissions on the channel this quote is from. Consider giving me these permissions in order to avoid this issue in the future.");
            QContext.User = QContext.Guild.GetUser(Author);
            QContext.Message = await QContext.Channel.GetMessageAsync(Message) as SocketUserMessage;
            if (QContext.Message == null) throw new Exception("Message not found. I cannot seem to find this message. It might have been deleted or the message or due to me not having the Read Messages and Read Message History premissions on the channel this quote is from. Consider giving me these permissions in order to avoid this issue in the future.");
        }
    }

    public class QuoteContext
    {
        public SocketUserMessage Message {get;set;}
        public SocketTextChannel Channel {get;set;}
        public SocketGuildUser User {get;set;}
        public SocketGuild Guild {get;set;}
    }

    public enum QuoteType {Text, Image, Attachment, ImageURL}
    public class QuoteReaction
    {
        public IEmote Emote {get;set;}
        public ReactionMetadata Metadata {get;set;}
    }
    public class QuoteModule : InteractiveBase<SocketCommandContext>, IController
    {
        public QuoteModule(LiteDatabase database, CommandCacheService commandCache, int index) 
        {
            this.Database = database;
                this.CommandCache = commandCache;
                this.Index = index;
               
        }
                public LiteDatabase Database {get;set;}
        public CommandCacheService CommandCache {get;set;}
        public int Index {get; set;} = 0;
        public List<Embed> Pages {get;set;} = new List<Embed>();

        [Command("Quote"),Alias("Q")]
        [RequireContext(ContextType.Guild)]
        public async Task RandomQuote()
        {
            var All = Database.GetCollection<Quote>("Quotes").Find(x=> x.Guild==Context.Guild.Id);
            var rnd = new Random();
            
            var Quote = All.ElementAt(rnd.Next(0,All.Count()-1));
            try{
                await Quote.GenerateContext(Context);
                var emb = await StaticMethods.EmbedMessage(Context,Quote.QContext);
                var emote = new Emoji("❓");

                var msg = await ReplyAsync(".");
                await msg.AddReactionAsync(emote);
                
                CommandCache.Add(Context.Message.Id,msg.Id);

                Interactive.AddReactionCallback(msg,new InlineReactionCallback(Interactive,Context,new ReactionCallbackData
                ("",emb,true,true,TimeSpan.FromMinutes(3))
                    .WithCallback(emote, async (C,R) => await GetContext(C,R,msg,Interactive,Quote))));
            }
            catch (Exception e)
            {
                Database.GetCollection<Quote>("Quotes").Delete(x => x.Message == Quote.Message);
                var msg = await ReplyAsync("It seems like this quote has returned the error `"+e.Message+"` and has beed deleted from the databanks, Appologies!");
                CommandCache.Add(Context.Message.Id,msg.Id);
            }
        }

        public async Task GetContext(SocketCommandContext c, SocketReaction r, IUserMessage msg, InteractiveService interactive, Quote quote)
        {
            var prev = new Emoji("⏮");
            var next = new Emoji("⏭");
            var kill = new Emoji("⏹");
            var channel = msg.Channel as SocketTextChannel;
            var _msgs = await channel.GetMessagesAsync(msg.Id,Direction.Before,5).FlattenAsync();
            var msgs = _msgs.ToList();
            msgs.Add(msg);
            foreach(var x in msgs.OrderBy(x=>x.Timestamp))
            {
                Pages.Add();
            }
        }

        public Task Next(SocketCommandContext c, SocketReaction r, SocketMessage msg)
        {
            
        }
            

        public Task Previous(SocketCommandContext c, SocketReaction r, SocketMessage msg)
        {
            
        }

        public Task ToIndex(SocketCommandContext c, SocketReaction r, SocketMessage msg)
        {
            
        }
    }
}