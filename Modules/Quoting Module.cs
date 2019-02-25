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

namespace Familiar.Modules 
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

        public async Task<Embed> Embed(SocketCommandContext context)
        {
            var C = context.Client.GetChannel(Channel) as SocketTextChannel;
            if (C == null) throw new Exception("The channel this quote was on has been deleted.");
            var U = context.Client.GetUser(Author);
            var M = await C.GetMessageAsync(Message);
            if (M == null) throw new Exception("The message this quote linked to has been deleted.");
            if(C.IsNsfw && (context.Channel as SocketTextChannel).IsNsfw) return new EmbedBuilder().WithAuthor(context.Client.CurrentUser).WithDescription("This quote is NSFW so it cannot be displayed here!").Build();

            var embed = new EmbedBuilder()
                .WithTimestamp(M.Timestamp);
            if (M.Content != "")
            {
                embed.WithDescription(M.Content);
            }
            if(U == null)
            {
                embed.WithAuthor("[User out of Reach]");
            }
            else
            {
                embed.WithAuthor(U);
            }
            if (M.Attachments.Count > 0)
            {
                switch (Type)
                {
                    case QuoteType.Attachment:
                        foreach (var x in M.Attachments)
                        {
                            embed.AddField(x.Filename,"[Download]("+x.Url+")",true);
                        }
                        break;
                    case QuoteType.Image:
                        embed.WithImageUrl(M.Attachments.First().Url);
                        break;
                }
            }
            return embed.Build();
        }
        public bool IsImageUrl(string URL)
        {
            var req = (HttpWebRequest)HttpWebRequest.Create(URL);
            req.Method = "HEAD";
            using (var resp = req.GetResponse())
            {
                return resp.ContentType.ToLower(CultureInfo.InvariantCulture)
                        .StartsWith("image/");
            }
        }

        
    }
    public enum QuoteType {Text, Image, Attachment, ImageURL}
    public class QuoteReaction
    {
        public IEmote Emote {get;set;}
        public ReactionMetadata Metadata {get;set;}
    }
    public class QuoteModule : InteractiveBase<SocketCommandContext>
    {
        public LiteDatabase Database {get;set;}
        public CommandCacheService CommandCache {get;set;}

        [Command("Quote"),Alias("Q")]
        [RequireContext(ContextType.Guild)]
        public async Task RandomQuote()
        {
            var All = Database.GetCollection<Quote>("Quotes").Find(x=> x.Guild==Context.Guild.Id);
            var rnd = new Random();

            var Quote = All.ElementAt(rnd.Next(0,All.Count()-1));
            try
            {
                var emb = await Quote.Embed(Context);
                var msg = await ReplyAsync("",embed: emb);
                CommandCache.Add(Context.Message.Id,msg.Id);
            }
            catch (Exception e)
            {
                var msg = await ReplyAsync("Uh oh, it seems like this quote has returned the error `"+e.Message+"` and has beed deleted from the databanks, sorry!");
                CommandCache.Add(Context.Message.Id,msg.Id);
            }
        }
    }
}