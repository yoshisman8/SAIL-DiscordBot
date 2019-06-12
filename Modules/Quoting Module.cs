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
    
    [Name("Message Quoting")]
    [Summary("This module contains all commands related to Finding Quotes! Keep in mind that even if you disable this module, existing quotes will not be deleted.")]
    public class QuoteModule : InteractiveBase<SocketCommandContext>
    {
        public CommandCacheService CommandCache {get;set;}
        

        [Command("Quote"),Alias("Q")]
        [RequireGuildSettings]
        [Summary("Fetches a random quote in this server.")]
        [RequireContext(ContextType.Guild)]
        public async Task RandomQuote()
        {
            var All = Program.Database.GetCollection<Quote>("Quotes").Find(x=> x.Guild==Context.Guild.Id);
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

                var msg = await Context.Channel.SendMessageAsync("",embed: emb);
                
                CommandCache.Add(Context.Message.Id,msg.Id);
                var callback = new ReactionCallbackData("",emb,false,false,TimeSpan.FromMinutes(3));
                callback.WithCallback(emote, async (C,R) => await GetContext(Context,R,msg,Interactive,Quote,callback));
                Interactive.AddReactionCallback(msg,new InlineReactionCallback(Interactive,Context,callback));
                await msg.AddReactionAsync(emote);
            }
            catch (Exception e)
            {
                Program.Database.GetCollection<Quote>("Quotes").Delete(x => x.Message == Quote.Message);
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
            var col = Program.Database.GetCollection<Quote>("Quotes").Find(x=>x.Guild == Context.Guild.Id);
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
                if(results.Count() > 1 && results.Count() < 10)
                {
                    var Pages = new List<Embed>();
                    foreach(var x in results)
                    {
                        await x.GenerateContext(Context);
                        Pages.Add(StaticMethods.EmbedMessage(Context,x.Context.Channel,x.Context.Message));
                    }
                    var msg = await new Controller(Pages,"Done reading quotes.").Start(Context,Interactive);
                    CommandCache.Add(Context.Message.Id,msg.Id);
                }
                else
                {
                    var Q = results.FirstOrDefault();
                    try
                    {
                        await Q.GenerateContext(Context);
                        var embed = StaticMethods.EmbedMessage(Context,Q.Context.Channel,Q.Context.Message);
                        var msg = await ReplyAsync("Found one result for '"+Query+"'.",embed: embed);
                        CommandCache.Add(Context.Message.Id,msg.Id);
                    }
                    catch (Exception e)
                    {
                        Program.Database.GetCollection<Quote>("Quotes").Delete(x => x.Message == Q.Message);
                        var msg = await ReplyAsync("It seems like this quote has returned the error `"+e.Message+"` and has beed deleted from the databanks, Appologies!");
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
            var col = Program.Database.GetCollection<Quote>("Quotes").Find(x=>x.Guild == Context.Guild.Id);
            if (col.Count() == 0)
            {
                var msg = await ReplyAsync("This server has no recorded quotes. React with 📌 on a message said by someone on the server to add the first quote.");
                CommandCache.Add(Context.Message.Id,msg.Id);
                return;
            }
            var results = col.Where(x => x.Channel == channel.Id);
            if (results.Count() == 0) 
            {
                var msg = await ReplyAsync("There are no quotes from "+channel+" on record.");
                CommandCache.Add(Context.Message.Id,msg.Id);
            }
            else
            {
                var rnd = new Random().Next(0,results.Count()-1);
                
                var Quote = results.ElementAt(rnd);
                try
                {
                    await Quote.GenerateContext(Context);
                    var emb = StaticMethods.EmbedMessage(Context,Quote.Context.Channel,Quote.Context.Message);
                    var emote = new Emoji("❓");

                    var msg = await Context.Channel.SendMessageAsync("",embed: emb);
                    
                    CommandCache.Add(Context.Message.Id,msg.Id);
                    var callback = new ReactionCallbackData("",emb,false,false,TimeSpan.FromMinutes(3));
                    callback.WithCallback(emote, async (C,R) => await GetContext(Context,R,msg,Interactive,Quote,callback));
                    Interactive.AddReactionCallback(msg,new InlineReactionCallback(Interactive,Context,callback));
                    await msg.AddReactionAsync(emote);
                }
                catch (Exception e)
                {
                    Program.Database.GetCollection<Quote>("Quotes").Delete(x => x.Message == Quote.Message);
                    var msg = await ReplyAsync("It seems like this quote has returned the error `"+e.Message+"` and has beed deleted from the databanks, Appologies!");
                    CommandCache.Add(Context.Message.Id,msg.Id);
                }
            }
        }

        public async Task GetContext(SocketCommandContext c, SocketReaction r, RestUserMessage msg, InteractiveService interactive, Quote quote, ReactionCallbackData callback)
        {
            await msg.RemoveAllReactionsAsync();
            interactive.RemoveReactionCallback(msg);

            var channel = c.Guild.GetTextChannel(quote.Context.Channel.Id);
            var raw = await quote.Context.Channel.GetMessagesAsync(quote.Context.Message.Id,Direction.Before,5).FlattenAsync();
            var context = raw.OfType<IUserMessage>().OrderBy(x=>x.Timestamp);
            var Pages = new List<Embed>();
            foreach(var x in context)
            {
                Pages.Add(StaticMethods.EmbedMessage(c,channel,x));
            }
            Pages.Add(StaticMethods.EmbedMessage(c,quote.Context.Channel,quote.Context.Message));

            await new Controller(Pages,"Finished Reading Context a Quote.",msg).Start(Context,Interactive);

            await msg.ModifyAsync(x=> x.Content = "Showing the last 5 messages before this Quote.");
        }
    }
}