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

namespace ERA.Modules
{
    public class Quoting : ModuleBase<SocketCommandContext>
    {
        [Command("Quote")]
        [Alias("Q")]
        [Summary("Finds a quote from the Quote database. Usage: `$Quote <search>`. React with :speaking_head: to add a message to the database.")]
        public async Task FindQuote(string Quote = "")
        {
            Directory.CreateDirectory(@"Data/Quotes/");
            var files = Directory.EnumerateFiles(@"Data/Quotes/");
            List<Quote> db = new List<Quote>() { };
            foreach (string x in files)
            {
                db.Add(JsonConvert.DeserializeObject<Quote>(File.ReadAllText(x)));
            }
            var result = db.Where(x => x.Content.ToLower().Contains(Quote.ToLower()));
            if (result.Count() != 0)
            {
                
                var quote = result.First();
                IChannel channel = GetChannel(quote.Channel);

                if (channel.IsNsfw == Context.Channel.IsNsfw || channel.IsNsfw == false)
                {
                    var builder = new EmbedBuilder()
                        .WithAuthor("E.R.A. Quoting system", Context.Client.CurrentUser.GetAvatarUrl())
                        .WithDescription(quote.Content + "\n- On " + GetChannel(quote.Channel).Mention)
                        .WithFooter(GetUser(quote.User).Username, GetUser(quote.User).GetAvatarUrl())
                        .WithTimestamp(quote.Date)
                        .WithColor(new Color(0, 153, 153));
                    await Context.Channel.SendMessageAsync("", embed: builder.Build());
                }
                else
                {
                    await Context.Channel.SendMessageAsync("This quote is NSFW and thus it can't be sent here!");
                }
            }
            else if (Quote == "")
            {
                var rnd = new Random();
                var quote = db.ElementAt(rnd.Next(db.Count));
                var builder = new EmbedBuilder()
                    .WithAuthor("E.R.A. Quoting system", Context.Client.CurrentUser.GetAvatarUrl())
                    .WithDescription(quote.Content + "\n- On " + GetChannel(quote.Channel).Mention)
                    .WithFooter(GetUser(quote.User).Username, GetUser(quote.User).GetAvatarUrl())
                    .WithTimestamp(quote.Date)
                    .WithColor(new Color(0, 153, 153));
                await Context.Channel.SendMessageAsync("", embed: builder.Build());
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
    }
}
public class Quote : ModuleBase<SocketCommandContext>
{

    public string Content { get; set; }
    public DateTime Date { get; set; }
    public ulong User { get; set; }
    public ulong Channel { get; set; }
}
