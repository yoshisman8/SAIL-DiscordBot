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
        public string SearchText {get;set;}
        public List<QuoteReaction> Reactions {get;set;} = new List<QuoteReaction>();
        [BsonIgnore]
        public QuoteContext Context {get;set;}
        
        public async Task GenerateContext(SocketCommandContext SCC)
        {
            Context.Guild = SCC.Client.GetGuild(Guild);
            if (Context.Guild == null) throw new Exception("Guild not found. I cannot access the server this quote is from. This is an unusual error, and you should contact my owner about this.");
            Context.Channel = Context.Guild.GetTextChannel(Channel);
            if (Context.Channel == null) throw new Exception("Channel not found. I cannot access the channel this quote is from. This can be due to me not having the Read Messages and Read Message History premissions on the channel this quote is from. Consider giving me these permissions in order to avoid this issue in the future.");
            Context.User = Context.Guild.GetUser(Author);
            Context.Message = await Context.Channel.GetMessageAsync(Message) as SocketUserMessage;
            if (Context.Message == null) throw new Exception("Message not found. I cannot seem to find this message. It might have been deleted or the message or due to me not having the Read Messages and Read Message History premissions on the channel this quote is from. Consider giving me these permissions in order to avoid this issue in the future.");
        }
    }

    public class QuoteContext
    {
        public SocketUserMessage Message {get;set;}
        public SocketTextChannel Channel {get;set;}
        public SocketGuildUser User {get;set;}
        public SocketGuild Guild {get;set;}
    }
    public class QuoteReaction
    {
        public IEmote Emote {get;set;}
        public ReactionMetadata Metadata {get;set;}
    }
    public class QuoteModule : InteractiveBase<SocketCommandContext>
    {
        public LiteDatabase Database {get;set;}
        public CommandCacheService CommandCache {get;set;}
        
        private Controller Controller {get;set;} = new Controller();

        [Command("Quote"),Alias("Q")]
        [RequireContext(ContextType.Guild)]
        public async Task RandomQuote()
        {
            var All = Database.GetCollection<Quote>("Quotes").Find(x=> x.Guild==Context.Guild.Id);
            var rnd = new Random();
            
            var Quote = All.ElementAt(rnd.Next(0,All.Count()-1));
            try{
                await Quote.GenerateContext(Context);
                var emb = await StaticMethods.EmbedMessage(Context,Quote.Context.Channel,Quote.Context.Message);
                var emote = new Emoji("❓");

                var msg = await ReplyAsync(".") as SocketUserMessage;
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
        [Command("Quote"),Alias("Q")]
        [Priority(2)] [RequireContext(ContextType.Guild)]
        public async Task SearchQuoteText([Remainder] string Query)
        {
            var col = Database.GetCollection<Quote>("Quotes").Find(x=>x.Guild == Context.Guild.Id);

            var results = col.Where(x => x.SearchText.ToLower().Contains(Query.ToLower()));
            if (results.Count() == 0) 
            {
                var msg = await ReplyAsync("There are no quotes that contain the text \""+Query+"\".");
                CommandCache.Add(Context.Message.Id,msg.Id);
            }
            else
            {
                var msg = await ReplyAsync("Searching for \""+Query+"\"...") as SocketUserMessage;
                if(results.Count() > 1)
                {
                    var prev = new Emoji("⏮");
                    await msg.AddReactionAsync(prev);
                    var next = new Emoji("⏭");
                    await msg.AddReactionAsync(next);
                    var kill = new Emoji("⏹");
                    await msg.AddReactionAsync(kill);
                    foreach(var x in results)
                    {
                        await x.GenerateContext(Context);
                        Controller.Pages.Add(await StaticMethods.EmbedMessage(Context,x.Context.Channel,x.Context.Message));
                    }
                    Interactive.AddReactionCallback(msg,new InlineReactionCallback(Interactive,Context,new ReactionCallbackData
                        ("Context (Page "+Controller.Index+1+" of **"+Controller.Pages.Count+"**)",
                        Controller.Pages.First(),false,false,TimeSpan.FromMinutes(3))
                            .WithCallback(prev,(ctx,rea)=>Controller.Previous(ctx,rea,msg))
                            .WithCallback(kill,(ctx,rea)=>Controller.Kill(Interactive,msg))
                            .WithCallback(next,(ctx,rea)=>Controller.Next(ctx,rea,msg))));
                    CommandCache.Add(Context.Message.Id,msg.Id);
                }
                else
                {
                    var Q = results.FirstOrDefault();
                    try
                    {
                        await Q.GenerateContext(Context);
                        await msg.ModifyAsync(x=>x.Content = "");
                        var embed = await StaticMethods.EmbedMessage(Context,Q.Context.Channel,Q.Context.Message);
                        await msg.ModifyAsync(x=>x.Embed = embed);
                        CommandCache.Add(Context.Message.Id,msg.Id);
                    }
                    catch (Exception e)
                    {
                        Database.GetCollection<Quote>("Quotes").Delete(x => x.Message == Q.Message);
                        await msg.ModifyAsync(x=>x.Content = "It seems like this quote has returned the error `"+e.Message+"` and has beed deleted from the databanks, Appologies!");
                        CommandCache.Add(Context.Message.Id,msg.Id);
                    }
                }
            }
        }

        public async Task GetContext(SocketCommandContext c, SocketReaction r, SocketUserMessage msg, InteractiveService interactive, Quote quote)
        {
            await msg.RemoveReactionAsync(r.Emote,r.User.Value);
            interactive.RemoveReactionCallback(msg);

            var prev = new Emoji("⏮");
            await msg.AddReactionAsync(prev);
            var next = new Emoji("⏭");
            await msg.AddReactionAsync(next);
            var kill = new Emoji("⏹");
            await msg.AddReactionAsync(kill);

            var channel = msg.Channel as SocketTextChannel;
            var raw = channel.GetMessagesAsync(msg.Id,Direction.Before,5).Flatten().ToEnumerable().ToList();
            raw.Add(msg);
            var context = raw.OrderBy(x=> x.Timestamp).ToList();
            foreach(var x in context.OrderBy(x=>x.Timestamp))
            {
                Controller.Pages.Add(await StaticMethods.EmbedMessage(c,channel,x as SocketUserMessage));
            }

            interactive.AddReactionCallback(msg,new InlineReactionCallback(Interactive,Context,new ReactionCallbackData
                ("Context (Page "+Controller.Index+1+" of **"+Controller.Pages.Count+"**)",
                Controller.Pages.First(),false,false,TimeSpan.FromMinutes(3))
                .WithCallback(prev,(ctx,rea)=>Controller.Previous(ctx,rea,msg))
                .WithCallback(kill,(ctx,rea)=>Controller.Kill(interactive,msg))
                .WithCallback(next,(ctx,rea)=>Controller.Next(ctx,rea,msg))));
        }
    }
}