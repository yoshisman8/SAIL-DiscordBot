using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

using Discord.Addons.Interactive;
using Discord.Addon.InteractiveMenus;
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
    public class QuoteModule : SailBase<SocketCommandContext>
    {
        public MenuService MenuService { get; set; }

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
                
                return;
            }
            var rnd = new Random().Next(0,All.Count()-1);
            
            var Quote = All.ElementAt(rnd);
            try{
                await Quote.GenerateContext(Context);
                var emb = StaticMethods.EmbedMessage(Context,Quote.Context.Channel,Quote.Context.Message);
                var emote = new Emoji("❓");

                var msg = await Context.Channel.SendMessageAsync("",embed: emb);
                
                
                var callback = new ReactionCallbackData("",emb,true,true);
                callback.WithCallback(emote, async (C,R) => await GetContext(C,msg,MenuService,Quote));
                Interactive.AddReactionCallback(msg,new InlineReactionCallback(Interactive,Context,callback));
                await msg.AddReactionAsync(emote);
            }
            catch (Exception e)
            {
                Program.Database.GetCollection<Quote>("Quotes").Delete(x => x.Message == Quote.Message);
                var msg = await ReplyAsync("It seems like this quote has returned the error `"+e.Message+"` and has beed deleted from the databanks, Appologies!");
                
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
                
                return;
            }
            var results = col.Where(x => x.SearchText.ToLower().Contains(Query.ToLower()));
            if (results.Count() == 0) 
            {
                var msg = await ReplyAsync("There are no quotes that contain the text \""+Query+"\".");
                
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
					var menu = new PagedEmbed("Quote Search Results for \"" + Query + "\"", Pages.ToArray());

					var msg = await MenuService.CreateMenu(Context, menu, false);
                    
                }
                else
                {
                    var Q = results.FirstOrDefault();
                    try
                    {
                        await Q.GenerateContext(Context);
                        var embed = StaticMethods.EmbedMessage(Context,Q.Context.Channel,Q.Context.Message);
                        var msg = await ReplyAsync("Found one result for '"+Query+"'.",embed: embed);
                        
                    }
                    catch (Exception e)
                    {
                        Program.Database.GetCollection<Quote>("Quotes").Delete(x => x.Message == Q.Message);
                        var msg = await ReplyAsync("It seems like this quote has returned the error `"+e.Message+"` and has beed deleted from the databanks, Appologies!");
                        
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
                
                return;
            }
            var results = col.Where(x => x.Channel == channel.Id);
            if (results.Count() == 0) 
            {
                var msg = await ReplyAsync("There are no quotes from "+channel+" on record.");
                
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
                    
                    
                    var callback = new ReactionCallbackData("",emb,true,true);
                    callback.WithCallback(emote, async (C,R) => await GetContext(C,msg,MenuService,Quote));
                    Interactive.AddReactionCallback(msg,new InlineReactionCallback(Interactive,Context,callback));
                    await msg.AddReactionAsync(emote);
                }
                catch (Exception e)
                {
                    Program.Database.GetCollection<Quote>("Quotes").Delete(x => x.Message == Quote.Message);
                    var msg = await ReplyAsync("It seems like this quote has returned the error `"+e.Message+"` and has beed deleted from the databanks, Appologies!");
                    
                }
            }
        }

        public async Task GetContext(SocketCommandContext c, RestUserMessage msg, MenuService menuService, Quote quote)
        {
            await msg.RemoveAllReactionsAsync();

            var channel = c.Guild.GetTextChannel(quote.Context.Channel.Id);
            var raw = await quote.Context.Channel.GetMessagesAsync(quote.Context.Message.Id,Direction.Before,5).FlattenAsync();
            var context = raw.OfType<IUserMessage>().OrderBy(x=>x.Timestamp);
            var Pages = new List<Embed>();
            foreach(var x in context)
            {
                Pages.Add(StaticMethods.EmbedMessage(c,channel,x));
            }
            Pages.Add(StaticMethods.EmbedMessage(c,quote.Context.Channel,quote.Context.Message));

            var menu = new PagedEmbed("Context for a quote.", Pages.ToArray());
			await menuService.CreateMenu(c, menu, false);
        }
    }
}