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
        public async Task FindQuote([Remainder] string Quote = "")
        {
            var col = Database.GetCollection<Quote>("Quotes");
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
                        await Context.Channel.SendMessageAsync("", embed: EmbedQuote(Message,quote.QuoteId));
                    }
                    else if (Message.Embeds.Count() >= 1)
                    {
                        Embed embed = (Embed)Message.Embeds.First();
                        await ReplyAsync("```Quote by: " + GetUser(quote.User).Username + " on: " + Message.CreatedAt.DateTime.ToShortDateString() + Message.CreatedAt.DateTime.ToShortTimeString() + "```\n" + quote.Content, embed: embed);
                    }
                }
                else
                {
                    await Context.Channel.SendMessageAsync("This quote is NSFW and thus it can't be sent here!");
                }
                return;
            }
            var result = col.Find(x => x.Content.Contains(Quote.ToLower()));
            if (result.Count() == 1)
            {
                var quote = result.FirstOrDefault();
                SocketTextChannel channel = Context.Guild.GetTextChannel(quote.Channel);
                SocketTextChannel CurrChan = Context.Channel as SocketTextChannel;
                var Message = await channel.GetMessageAsync(quote.Message);
                if (Message == null) { await ReplyAsync("This quote contains a null message ID! (Maybe the original message was deleted?) and has been deleted from the database."); col.Delete(quote.QuoteId); return; }
                if (CurrChan.IsNsfw == channel.IsNsfw || channel.IsNsfw == false)
                {
                    if (Message.Attachments.Count() == 0)
                    {
                        await Context.Channel.SendMessageAsync("", embed: EmbedQuote(Message,quote.QuoteId));
                    }
                    else if (Message.Attachments.Count() > 1){
                        await Context.Channel.SendMessageAsync(Message.Attachments.First().Url,false,EmbedQuote(Message,quote.QuoteId));
                    }
                }
                else
                {
                    await Context.Channel.SendMessageAsync("This quote is from a NSFW and thus it can't be viewed here!");
                }
            }
            else if (result.Count() > 1){
                var rnd = new Random();
                int index = rnd.Next(0,result.Count()-1);
                var quote = result.ElementAt(index);
                SocketTextChannel channel = Context.Guild.GetTextChannel(quote.Channel);
                SocketTextChannel CurrChan = Context.Channel as SocketTextChannel;
                var Message = await channel.GetMessageAsync(quote.Message);
                if (Message == null) { await ReplyAsync("This quote contains a null message ID! (Maybe the original message was deleted?) and has been deleted from the database."); col.Delete(quote.QuoteId); return; }
                if (CurrChan.IsNsfw == channel.IsNsfw || channel.IsNsfw == false)
                {
                    if (Message.Attachments.Count() == 0)
                    {
                        await Context.Channel.SendMessageAsync("", embed: EmbedQuote(Message,quote.QuoteId));
                    }
                    else if (Message.Attachments.Count() > 1){
                        await Context.Channel.SendMessageAsync(Message.Attachments.First().Url,false,EmbedQuote(Message,quote.QuoteId));
                    }
                }
                else
                {
                    await Context.Channel.SendMessageAsync("This quote is from a NSFW and thus it can't be viewed here!");
                }
            }
            else
            {
                await ReplyAsync("There is no quote that contains the word \"" + Quote + "\"");
            }
        }
        [Command("Context")]
        [Summary("Get some context on a quote. Usage: /Context <Quote ID>")]
        public async Task getcontext(int id){
            var col = Database.GetCollection<Quote>("Quotes");
            var quote = col.FindOne(x => x.QuoteId == id);
            var channel = Context.Guild.GetChannel(quote.Channel) as SocketTextChannel;
            var msg = await channel.GetMessageAsync(quote.Message);
            var msgs = await channel.GetMessagesAsync(quote.Message,Direction.Before,5).FlattenAsync();
            foreach (var x in msgs){
                var builder = new EmbedBuilder()
                        .WithAuthor(Context.Client.CurrentUser.Username+" Context dispenser", Context.Client.CurrentUser.GetAvatarUrl())
                        .WithDescription(quote.Content + "\n- On " + GetChannel(x.Channel.Id).Mention)
                        .WithFooter(GetUser(x.Author.Id).Username, GetUser(x.Author.Id).GetAvatarUrl())
                        .WithTimestamp(x.CreatedAt)
                        .WithColor(new Color(0, 153, 153));
                await ReplyAsync("",embed: builder.Build());
            }
            await ReplyAsync("",embed: EmbedQuote(msg,quote.QuoteId));
        }

        [Command("FixQuotes"),Alias("Update quotes")]
        [RequireUserPermission(GuildPermission.ManageChannels)]
        public async Task fix(){
            var col = Database.GetCollection<Quote>("Quotes");
            var quotes = col.FindAll();
            var type = Context.Channel.EnterTypingState();
            File.Delete(@"Data/Temp/quotelog.txt");
            var log = File.CreateText(@"Data/Temp/quotelog.txt");
            int Deleted = 0;
            int Updated = 0;
            foreach(var x in quotes){
                ITextChannel channel = Context.Guild.GetTextChannel(x.Channel);
                var message = await channel.GetMessageAsync(x.Message);
                if (message == null || message.Content == ""){
                    log.WriteLine("["+DateTime.Now.ToShortDateString()+"]"+DateTime.Now.ToShortTimeString()+" Deleted Quote ID"+x.QuoteId+", Quote empty or invalid/deleted.");
                    col.Delete(x.QuoteId);
                    Deleted++;
                    continue;
                }
                x.Content = message.Content;
                log.WriteLine("["+DateTime.Now.ToShortDateString()+"]"+DateTime.Now.ToShortTimeString()+" Parsed/Updated quote ID"+x.QuoteId+".");
                Updated++;
                col.Update(x);
            }
            type.Dispose();
            log.Flush();
            await Context.Channel.SendFileAsync(@"Data/Temp/quotelog.txt","Finished Parsing all quotes. Updated "+Updated+" quotes and Deleted "+Deleted+" quotes.");
            File.Delete(@"Data/Temp/quotelog.txt");
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
        public Embed EmbedQuote (IMessage quote,int ID)
        {
            var builder = new EmbedBuilder()
                        .WithAuthor(Context.Client.CurrentUser.Username+" Quoting System", Context.Client.CurrentUser.GetAvatarUrl())
                        .WithDescription(quote.Content + "\n- On " + GetChannel(quote.Channel.Id).Mention)
                        .WithFooter("ID: "+ID+" "+GetUser(quote.Author.Id).Username, GetUser(quote.Author.Id).GetAvatarUrl())
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
    public ulong Message { get; set; } //Message ID as per Discord
    public ulong User { get; set; } //User ID 
    public ulong Channel { get; set; } //Channel ID 
}
