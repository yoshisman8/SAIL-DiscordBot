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
            if (Quote == "")
            {
                var rnd = new Random().Next(0, col.Count());
                var quote = col.FindAll().ElementAt(rnd);

                ITextChannel channel = GetChannel(quote.Channel);
                ITextChannel Ch = Context.Channel as ITextChannel;
                var Message = await GetMessageAsync(quote.Message);
                if (Message == null) { await ReplyAsync("This quote contains a null message ID! (Maybe the original message was deleted?)"); return; }
                if (channel.IsNsfw == Ch.IsNsfw || channel.IsNsfw == false)
                {
                    if (Message.Embeds.Count() == 0)
                    {
                        await Context.Channel.SendMessageAsync("", embed: EmbedQuote(quote));
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
            var result = col.FindOne(x => x.Content.Contains(Quote.ToLower()));
            if (result != null)
            {
                ITextChannel channel = GetChannel(result.Channel);
                ITextChannel Ch = Context.Channel as ITextChannel;
                var Message = await GetMessageAsync(result.Message);
                if (Message == null) { await ReplyAsync("This quote contains a null message ID! (Maybe the original message was deleted?)"); return; }
                if (channel.IsNsfw == Ch.IsNsfw || channel.IsNsfw == false)
                {
                    if (Message.Embeds.Count() == 0)
                    {
                        await Context.Channel.SendMessageAsync("", embed: EmbedQuote(result));
                    }
                    else
                    {
                        Embed embed = (Embed)Message.Embeds.First();
                        await ReplyAsync("```Quote by: " + GetUser(result.User).Username + " on: " + result.Date.ToShortDateString() + result.Date.ToShortTimeString() + "```\n" + result.Content, embed: embed);
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
        public async Task<IMessage> GetMessageAsync(ulong id)
        {
            IMessage message = null;
            foreach (SocketTextChannel X in Context.Guild.TextChannels)
            {
                var Y = await X.GetMessageAsync(id);
                if (Y != null)
                {
                    message = Y;
                    return message;
                }
            }
            return message;
        }
        public Embed EmbedQuote (Quote quote)
        {
            var builder = new EmbedBuilder()
                        .WithAuthor("E.R.A. Quoting system", Context.Client.CurrentUser.GetAvatarUrl())
                        .WithDescription(quote.Content + "\n- On " + GetChannel(quote.Channel).Mention)
                        .WithFooter(GetUser(quote.User).Username, GetUser(quote.User).GetAvatarUrl())
                        .WithTimestamp(quote.Date)
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
