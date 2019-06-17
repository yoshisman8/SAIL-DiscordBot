using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.Rest;
using Discord.WebSocket;

namespace Discord.Addon.InteractiveMenus
{
	public abstract class Menu
	{
		public ICriteria<SocketReaction>[] Criterias { get; set; }
		public Dictionary<IEmote,Task<bool>> Buttons = new Dictionary<IEmote, Task<bool>>();
		public SocketCommandContext Context;
		public RestUserMessage Message;
		public MenuService Service;
		public virtual async Task<RestUserMessage> Initialize(SocketCommandContext commandContext,MenuService service)
		{
			Service = service;
			Context = commandContext;
			Message = await Context.Channel.SendMessageAsync("Loading Menu...");
			return Message;
		}
		public abstract Task<bool> HandleButtonPress(SocketReaction reaction);

		public async Task<bool> JudgeCriteriaAsync(SocketReaction reaction)
		{
			if (Criterias == null || Criterias.Length == 0) return true;
			bool[] results = new bool[Criterias.Length];
			for(int i = 0;i<=results.Length;i++)
			{
				results[i] = await Criterias[i].JudgeCriteria(reaction);
			}
			return results.All(x => x == true);
		}
	}
}
