using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using Discord;
using Newtonsoft.Json;
using Discord.WebSocket;

namespace SAIL.Services
{
    public class HelpService
	{
		public Dictionary<string,Document> HelpDocs { get; private set; }
		private readonly DiscordSocketClient discord;
		public HelpService(DiscordSocketClient _client)
		{
			discord = _client;
			Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), "Data","Docs"));
			var eb = new EmbedBuilder()
				.WithAuthor("Person", "https://cdn.discordapp.com/embed/avatars/0.png", "https://cdn.discordapp.com/embed/avatars/0.png")
				.WithColor(Color.Blue)
				.WithCurrentTimestamp()
				.WithDescription("description")
				.WithFooter("Footer text", "https://cdn.discordapp.com/embed/avatars/0.png")
				.WithImageUrl("https://cdn.discordapp.com/embed/avatars/0.png")
				.WithThumbnailUrl("https://cdn.discordapp.com/embed/avatars/0.png")
				.WithTitle("title")
				.WithUrl("https://cdn.discordapp.com/embed/avatars/0.png")
				.WithDescription("description")
				.AddField("Field a", "Content A", true)
				.AddField("field b ", "Content B", false);
			File.WriteAllText(Path.Combine(Directory.GetCurrentDirectory(), "Data", "Docs", "writetest.json"), JsonConvert.SerializeObject(eb.Build(),Formatting.Indented));
		}
		public Embed GetDocument(string Name)
		{
			if(!HelpDocs.TryGetValue(Name,out Document T))
			{
				return null;
			}
			return T.Embed();
		}
	}
	public class Document
	{
		public Embed Embed()
		{
			throw new NotImplementedException();
		}
	}
}
