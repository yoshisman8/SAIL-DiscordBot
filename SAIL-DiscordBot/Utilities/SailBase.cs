using Discord;

using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.Rest;
using SAIL.Services;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace SAIL
{
	
	public class SailBase<T> : InteractiveBase<T>
		where T : SocketCommandContext
	{
		public CommandHandlingService Command { get; set; }
		public async Task<RestUserMessage> ReplyAsync(string Content, Embed Embed = null, bool isTTS = false)
		{
			if (Command.Cache.TryGetValue(Context.Message.Id, out ulong id))
			{
				var msg = (RestUserMessage)await Context.Channel.GetMessageAsync(id);
				if (msg == null)
				{
					return await Context.Channel.SendMessageAsync(Content, isTTS, Embed);
				}
				else
				{
					await msg.ModifyAsync(x => x.Content = Content);
					await msg.ModifyAsync(x => x.Embed = Embed);
					return msg;
				}
			}
			else
			{
				var msg = await Context.Channel.SendMessageAsync(Content, isTTS, Embed);
				Command.Cache.Add(Context.Message.Id, msg.Id);
				return msg;
			}
		}
	}
}
