using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Discord.Commands;
using Discord.WebSocket;
using Discord;
using Newtonsoft.Json;
using System.Linq;
using LiteDB;

namespace ERA20.Modules
{
    public class Quoting : ModuleBase<SocketCommandContext>
    {
        public LiteDatabase Database { get; set; }
        [Command("Quote")]
        [Alias("Q")]
        [Summary("Finds a quote from the Quote database. Usage: `/Quote <search>`. React with :speaking_head: to add a message to the database.")]
        public async Task FindQuote(string Quote = "")
        {
            var col = Database.GetCollection<Quote>("Quotes");
            var debug = col.FindAll();
            if (Quote == "")
            {
                var rnd = new Random().Next(0, col.Count());
                var quote = col.FindAll().ElementAt(rnd);

                ITextChannel channel = GetChannel(quote.Channel);
                ITextChannel Ch = Context.Channel as ITextChannel;
                var Message = await GetMessageAsync(quote.Message, channel);
                if (Message == null) 
                { 
                    await ReplyAsync("This quote contains a null message ID! (Maybe the original message was deleted?) and has been removed from the database!"); 
                    col.Delete(quote.QuoteId);
                    return; 
                }
                if (channel.IsNsfw == Ch.IsNsfw || channel.IsNsfw == false)
                {
                    if (Message.Embeds.Count() == 0)
                    {
                        await Context.Channel.SendMessageAsync("", embed: EmbedQuote(Message));
                    }
                    else if (Message.Embeds.Count() >= 1)
                    {
                        Embed embed = (Embed)Message.Embeds.First();
                        await ReplyAsync("```Quote by: " + GetUser(quote.User).Username + " on: " + quote.Date.ToShortDateString() + quote.Date.ToShortTimeString() + "```\n" + quote.Content, embed: embed);
                    }
                }
                else
                {
                    await Context.Channel.SendMessageAsync("This quote is NSFW and thus it can't be sent here!");
                }
                return;
            }
            var all = col.FindAll();
            var Quotes = new List<IMessage>();
            foreach (var x in all){
                Quotes.Add(await GetMessageAsync(x.Message,Context.Guild.GetTextChannel(x.Channel)));
            }
            var result = Quotes.Where(x => x.Content.ToLower().Contains(Quote.ToLower()));
            if (result.Count() >=1)
            {
                var Message = result.FirstOrDefault();
                ITextChannel channel = result.FirstOrDefault().Channel as ITextChannel;
                ITextChannel Ch = Context.Channel as ITextChannel;
                if (Message == null) { await ReplyAsync("This quote contains a null message ID! (Maybe the original message was deleted?)"); return; }
                if (channel.IsNsfw == Ch.IsNsfw || channel.IsNsfw == false)
                {
                    if (Message.Embeds.Count() == 0)
                    {
                        await Context.Channel.SendMessageAsync("", embed: EmbedQuote(Message));
                    }
                    else
                    {
                        Embed embed = (Embed)Message.Embeds.First();
                        await ReplyAsync("```Quote by: " + GetUser(Message.Author.Id).Username + " on: " + Message.CreatedAt.DateTime.ToShortDateString() + Message.CreatedAt.DateTime.ToShortTimeString() + "```\n" + Message.Content, embed: embed);
                    }
                }
                else
                {
                    await Context.Channel.SendMessageAsync("This quote is NSFW and thus it can't be sent here!");
                }
            }           
            else
            {
                await ReplyAsync("There is no quote that contains the word \"" + Quote + "\"");
            }
        }
        public SocketUser GetUser(ulong id)
        {
            SocketUser user = Context.Guild.GetUser(id);
            return user;
        }
        public SocketTextChannel GetChannel(ulong id)
        {
            SocketTextChannel channel = Context.Guild.GetTextChannel(id);
            return channel;
        }
        public async Task<IMessage> GetMessageAsync(ulong id, ITextChannel channel)
        {
            IMessage message = null;
            message = await channel.GetMessageAsync(id);
            return message;
   
        }
        public Embed EmbedQuote (IMessage quote)
        {
            var builder = new EmbedBuilder()
                        .WithAuthor("E.R.A. Quoting system", Context.Client.CurrentUser.GetAvatarUrl())
                        .WithDescription(quote.Content + "\n- On " + GetChannel(quote.Channel.Id).Mention)
                        .WithFooter(GetUser(quote.Author.Id).Username, GetUser(quote.Author.Id).GetAvatarUrl())
                        .WithTimestamp(quote.CreatedAt)
                        .WithColor(new Color(0, 153, 153));
            return builder.Build();
        }
    }
}
public class Quote : ModuleBase<SocketCommandContext>
{
    public int QuoteId { get; set; }
    public string Content { get; set; }
    public DateTime Date { get; set; }
    public ulong Message { get; set; }
    public ulong User { get; set; }
    public ulong Channel { get; set; }
}
