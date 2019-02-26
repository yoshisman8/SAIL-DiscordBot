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
using SAIL.Modules;
using System.Text;

namespace SAIL.Classes
{
    public static class StaticMethods
    {
        public static bool IsImageUrl(string URL)
        {
            var req = (HttpWebRequest)HttpWebRequest.Create(URL);
            req.Method = "HEAD";
            using (var resp = req.GetResponse())
            {
                return resp.ContentType.ToLower(CultureInfo.InvariantCulture)
                        .StartsWith("image/");
            }
        }
        public static async Task<Embed> EmbedMessage(SocketCommandContext context, SocketTextChannel channel, SocketUserMessage message)
        {
            if (channel == null) throw new Exception("Channel not found. It might have been deleted or I may no longer have the \"Read Message\" and \"Read Message History\" Permissions.");
            if (message == null) throw new Exception("Message not found. It might have been deleted or I may no longer have the \"Read Message History\" permission");
            
            if(channel.IsNsfw == true && (context.Channel as SocketTextChannel).IsNsfw == false)
            return new EmbedBuilder()
                .WithAuthor(context.Client.CurrentUser)
                .WithDescription("This quote is NSFW so it cannot be displayed here!")
                .Build();

            var embed = new EmbedBuilder()
                .WithTitle("SAIL Message Sorage System")
                .WithTimestamp(message.Timestamp)
                .WithFooter("On #"+channel.Name)
                .WithUrl(message.GetJumpUrl());
            if (message.Content != "")
            {
                embed.WithDescription(message.Content);
            }
            if(message.Author == null)
            {
                embed.WithAuthor("[User out of Reach]");
            }
            else
            {
                embed.WithAuthor(message.Author);
            }
            if (message.Attachments.Count > 0)
            {
                if (IsImageUrl(message.Attachments.First().Url))
                {
                    embed.WithImageUrl(message.Attachments.First().Url);
                }
                else
                {
                    foreach (var x in message.Attachments.Take(10))
                    {
                        embed.AddField(x.Filename,"[Download]("+x.Url+")",true);
                    }
                }
            }
            if(message.Reactions.Where(x=> x.Key != new Emoji("📌")).Count() == 0)
            {
                var sb = new StringBuilder();
                foreach (var x in message.Reactions.Where(x=> x.Key != new Emoji("📌")))
                {
                    sb.Append("["+x.Key+" ("+x.Value.ReactionCount+")] ");
                }
                embed.AddField("Reactions",sb.ToString());
            }
            return embed.Build();
        }
    }
}