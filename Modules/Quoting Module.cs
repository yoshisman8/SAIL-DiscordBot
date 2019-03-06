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
        public List<IEmote> Reactions {get;set;} = new List<IEmote>();
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
    public class QuoteReaction
    {
        public IEmote Emote {get;set;}
        public ReactionMetadata Metadata {get;set;}
    }
    [Name("Message Quoting")]
    [Summary("This module contains all commands related to Finding Quotes! Keep in mind that even if you disable this module, existing quotes will not be deleted.")]
    public class QuoteModule : InteractiveBase<SocketCommandContext>
    {
        public LiteDatabase Database {get;set;}
        public CommandCacheService CommandCache {get;set;}
        
        private Controller Controller {get;set;} = new Controller();

        [Command("Quote"),Alias("Q")]
        [RequireGuildSettings]
        [Summary("Fetches a random quote in this server.")]
        [RequireContext(ContextType.Guild)]
        public async Task RandomQuote()
        {
            var All = Database.GetCollection<Quote>("Quotes").Find(x=> x.Guild==Context.Guild.Id);
            if (All.Count() == 0)
            {
                var msg = await ReplyAsync("This server has no recorded quotes. React with 📌 on a message said by someone on the server to add the first quote.");
                CommandCache.Add(Context.Message.Id,msg.Id);
                return;
            }
            var rnd = new Random().Next(0,All.Count()-1);
            
            var Quote = All.ElementAt(rnd);
            try{
                await Quote.GenerateContext(Context);
                var emb = StaticMethods.EmbedMessage(Context,Quote.Context.Channel,Quote.Context.Message);
                var emote = new Emoji("❓");

                var msg = await ReplyAsync("",embed: emb);
                
                CommandCache.Add(Context.Message.Id,msg.Id);
                var callback = new ReactionCallbackData("",emb,false,false,TimeSpan.FromMinutes(3));
                callback.WithCallback(emote, async (C,R) => await GetContext(Context,R,msg,Interactive,Quote,callback));
                Interactive.AddReactionCallback(msg,new InlineReactionCallback(Interactive,Context,callback));
                await msg.AddReactionAsync(emote);
            }
            catch (Exception e)
            {
                Database.GetCollection<Quote>("Quotes").Delete(x => x.Message == Quote.Message);
                var msg = await ReplyAsync("It seems like this quote has returned the error `"+e.Message+"` and has beed deleted from the databanks, Appologies!");
                CommandCache.Add(Context.Message.Id,msg.Id);
            }
        }
        [Command("Quote"),Alias("Q")]
        [RequireGuildSettings]
        [Summary("Searches for a quote whose message contents contain a string of text. This is not case sensitive.")]
        [Priority(0)] [RequireContext(ContextType.Guild)]
        public async Task SearchQuoteText([Remainder] string Query)
        {
            var col = Database.GetCollection<Quote>("Quotes").Find(x=>x.Guild == Context.Guild.Id);
            if (col.Count() == 0)
            {
                var msg = await ReplyAsync("This server has no recorded quotes. React with 📌 on a message said by someone on the server to add the first quote.");
                CommandCache.Add(Context.Message.Id,msg.Id);
                return;
            }
            var results = col.Where(x => x.SearchText.ToLower().Contains(Query.ToLower()));
            if (results.Count() == 0) 
            {
                var msg = await ReplyAsync("There are no quotes that contain the text \""+Query+"\".");
                CommandCache.Add(Context.Message.Id,msg.Id);
            }
            else
            {
                var msg = await ReplyAsync("Searching for \""+Query+"\"...");
                if(results.Count() > 1)
                {
                    var prev = new Emoji("⏮");
                    await msg.AddReactionAsync(prev);
                    var kill = new Emoji("⏹");
                    await msg.AddReactionAsync(kill);
                    var next = new Emoji("⏭");
                    await msg.AddReactionAsync(next);
                    Controller.Pages.Clear();
                    foreach(var x in results)
                    {
                        await x.GenerateContext(Context);
                        Controller.Pages.Add(StaticMethods.EmbedMessage(Context,x.Context.Channel,x.Context.Message));
                    }
                    await msg.ModifyAsync(x=>x.Content= "Found "+results.Count()+" results for '"+Query+"'.");
                    await msg.ModifyAsync(x=> x.Embed = Controller.Pages.ElementAt(Controller.Index));

                    Interactive.AddReactionCallback(msg,new InlineReactionCallback(Interactive,Context,
                    new ReactionCallbackData("",null,false,false,TimeSpan.FromMinutes(3))
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
                        await msg.ModifyAsync(x=>x.Content = "Found one result for '"+Query+"'.");
                        var embed = StaticMethods.EmbedMessage(Context,Q.Context.Channel,Q.Context.Message);
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
        [Command("Quote"),Alias("Q")]
        [RequireGuildSettings]
        [Summary("Searches for a random quote from the listed channel.")]
        [Priority(1)] [RequireContext(ContextType.Guild)]
        public async Task SearchChannel(ITextChannel channel)
        {
            var col = Database.GetCollection<Quote>("Quotes").Find(x=>x.Guild == Context.Guild.Id);
            if (col.Count() == 0)
            {
                var msg = await ReplyAsync("This server has no recorded quotes. React with 📌 on a message said by someone on the server to add the first quote.");
                CommandCache.Add(Context.Message.Id,msg.Id);
                return;
            }
            var results = col.Where(x => x.Channel == channel.Id);
            if (results.Count() == 0) 
            {
                var msg = await ReplyAsync("There are no quotes from "+channel+" on the database.");
                CommandCache.Add(Context.Message.Id,msg.Id);
            }
            else
            {
                var rnd = new Random().Next(0,results.Count()-1);
                
                var Quote = results.ElementAt(rnd);
                try{
                    await Quote.GenerateContext(Context);
                    var emb = StaticMethods.EmbedMessage(Context,Quote.Context.Channel,Quote.Context.Message);
                    var emote = new Emoji("❓");

                    var msg = await ReplyAsync("",embed: emb);
                    
                    CommandCache.Add(Context.Message.Id,msg.Id);
                    var callback = new ReactionCallbackData("",emb,false,false,TimeSpan.FromMinutes(3));
                    callback.WithCallback(emote, async (C,R) => await GetContext(Context,R,msg,Interactive,Quote,callback));
                    Interactive.AddReactionCallback(msg,new InlineReactionCallback(Interactive,Context,callback));
                    await msg.AddReactionAsync(emote);
                }
                catch (Exception e)
                {
                    Database.GetCollection<Quote>("Quotes").Delete(x => x.Message == Quote.Message);
                    var msg = await ReplyAsync("It seems like this quote has returned the error `"+e.Message+"` and has beed deleted from the databanks, Appologies!");
                    CommandCache.Add(Context.Message.Id,msg.Id);
                }
            }
        }
        [Command("Quote"),Alias("Q")]
        [RequireGuildSettings]
        [Priority(2)] [RequireContext(ContextType.Guild)]
        [Summary("Searches for a quote that has any of the specified reactions on it. You can list the reactions by separating them by spaces or commas like so: `Quote 😀 😒 😂`.")]
        public async Task SearchFromReactions(params string[] Emotes)
        {
            var col = Database.GetCollection<Quote>("Quotes").Find(x=>x.Guild == Context.Guild.Id).ToList();
            if (col.Count() == 0)
            {
                var msg = await ReplyAsync("This server has no recorded quotes. React with 📌 on a message said by someone on the server to add the first quote.");
                CommandCache.Add(Context.Message.Id,msg.Id);
                return;
            }
            var results = col.Where(x=>x.Reactions.Exists(y=>Emotes.ToList().Contains(y.Name)));
            if (results.Count() == 0) 
            {
                var msg = await ReplyAsync("There are no quotes that have any of these reactions: "+String.Join(',',Emotes.ToList())+".");
                CommandCache.Add(Context.Message.Id,msg.Id);
            }
            else
            {
                var msg = await ReplyAsync("Searching for \""+String.Join(',',Emotes.ToList())+"\"...");
                if(results.Count() > 1)
                {
                    var prev = new Emoji("⏮");
                    await msg.AddReactionAsync(prev);
                    var kill = new Emoji("⏹");
                    await msg.AddReactionAsync(kill);
                    var next = new Emoji("⏭");
                    await msg.AddReactionAsync(next);
                    foreach(var x in results.Take(10))
                    {
                        await x.GenerateContext(Context);
                        Controller.Pages.Add(StaticMethods.EmbedMessage(Context,x.Context.Channel,x.Context.Message));
                    }
                    await msg.ModifyAsync(x=>x.Content= "Found "+results.Count()+" results for '"+String.Join(',',Emotes.ToList())+"'.");
                    await msg.ModifyAsync(x=> x.Embed = Controller.Pages.ElementAt(Controller.Index));

                    Interactive.AddReactionCallback(msg,new InlineReactionCallback(Interactive,Context,new ReactionCallbackData
                        ("",null,false,false,TimeSpan.FromMinutes(3))
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
                        await msg.ModifyAsync(x=>x.Content = "Found one result for '"+String.Join(',',Emotes.ToList())+"'.");
                        var embed = StaticMethods.EmbedMessage(Context,Q.Context.Channel,Q.Context.Message);
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

        public async Task GetContext(SocketCommandContext c, SocketReaction r, IUserMessage msg, InteractiveService interactive, Quote quote, ReactionCallbackData callback)
        {
            await msg.RemoveAllReactionsAsync();

            var prev = new Emoji("⏮");
            await msg.AddReactionAsync(prev);
            var kill = new Emoji("⏹");
            await msg.AddReactionAsync(kill);
            var next = new Emoji("⏭");
            await msg.AddReactionAsync(next);

            var channel = c.Guild.GetTextChannel(msg.Channel.Id);
            var raw = await quote.Context.Channel.GetMessagesAsync(quote.Context.Message.Id,Direction.Before,5).FlattenAsync();
            var context = raw.OfType<IUserMessage>().OrderBy(x=>x.Timestamp);
            Controller.Pages.Clear();
            foreach(var x in context)
            {
                Controller.Pages.Add(StaticMethods.EmbedMessage(c,channel,x));
            }
            Controller.Pages.Add(StaticMethods.EmbedMessage(c,quote.Context.Channel,quote.Context.Message));

            await msg.ModifyAsync(x=> x.Embed = Controller.Pages.ElementAt(Controller.Index));
            await msg.ModifyAsync(x=> x.Content = "Showing the last 5 messages before this Quote.\n"+
                "Use ⏮ and ⏭ to navigate. Press ⏹ to end navigation\n"+
                "Note: Navigation will be automatically dissabled after 3 minutes");
            
            callback.WithCallback(prev,(ctx,rea)=>Controller.Previous(c,rea,msg))
                .WithCallback(kill,(ctx,rea)=>Controller.Kill(interactive,msg))
                .WithCallback(next,(ctx,rea)=>Controller.Next(c,rea,msg));
            
            interactive.AddReactionCallback(msg,new InlineReactionCallback(Interactive,c,callback));
        }
    }
}