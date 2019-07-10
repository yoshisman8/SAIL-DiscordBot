using System;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using Discord.Commands;
using Discord.Addons.CommandCache;
using System.Text.RegularExpressions;
using SAIL.Classes;
using Dice;

namespace SAIL.Modules
{
    [Name("Dice Roller")]
    [Summary("This module contains multiple commands for rolling all sorts of dice. Useful for games and roleplay!\n More info about dice notation [here](https://github.com/DarthPedro/OnePlat.DiceNotation/blob/master/docs/DiceNotationExamples.md).")]
    public class DiceModule : ModuleBase<SocketCommandContext>
    {
		public CommandCacheService cache { get; set; }

        [Command("Roll"), Alias("r")]
        [RequireGuildSettings]
        [Summary("Rolls a die on the dice notation format. More info about dice notation [here](https://github.com/DarthPedro/OnePlat.DiceNotation/blob/master/docs/DiceNotationExamples.md).")]
        public async Task DieRoll([Remainder]string DiceExpression = "1d20")
        {
			try
			{
				var result = Roller.Roll(DiceExpression);


				if (cache.Any(x => x.Key == Context.Message.Id	)) cache.Remove(Context.Message.Id);
				if(Context.Guild!=null)await Context.Message.DeleteAsync();
				await ReplyAsync(Context.User.Mention+", ["+result.Expression+ "] " + result.ToString().Split("=>")[1] + " ⇒ **" +result.Value+"**.");
			}
			catch (Exception e)
			{
				var msg = await ReplyAsync(Context.User.Mention + ", Error, your dice expression is incorrect!");
				cache.Add(Context.Message.Id, msg.Id);
			}
        }
		
        [Command("Max")]
        [RequireGuildSettings]
        [Summary("Shows the maximum possible roll for this dice roll.")]
        public async Task Max([Remainder]string DiceExpression)
		{
			try
			{
				var result = Roller.Max(DiceExpression);
				if (cache.Any(x => x.Key == Context.Message.Id)) cache.Remove(Context.Message.Id);

				if (Context.Guild != null) await Context.Message.DeleteAsync();
				await ReplyAsync(Context.User.Mention + ", Maximum result for [" + result.Expression + "] "+ result.ToString().Split("=>")[1] + " ⇒ **" +result.Value+"**.");
			}
			catch (Exception e)
			{
				var msg = await ReplyAsync(Context.User.Mention + ", Error, your dice expression is incorrect!");
				cache.Add(Context.Message.Id, msg.Id);
			}
		}
        [Command("Min")]
        [RequireGuildSettings]
        [Summary("Shows the minimum possible roll for this dice roll.")]
        public async Task Min([Remainder]string DiceExpression)
		{
			try
			{
				var result = Roller.Min(DiceExpression);
				if (cache.Any(x => x.Key == Context.Message.Id)) cache.Remove(Context.Message.Id);

				if (Context.Guild != null) await Context.Message.DeleteAsync();
				await ReplyAsync(Context.User.Mention + ", Minimum result for [" + result.Expression + "] "+ result.ToString().Split("=>")[1] + " ⇒ **" +result.Value+"**.");
			}
			catch (Exception e)
			{
				var msg = await ReplyAsync(Context.User.Mention + ", Error, your dice expression is incorrect!");
				cache.Add(Context.Message.Id, msg.Id);
			}
		}
		public async Task Autoroll(SocketCommandContext context, [Remainder]string DiceExpression)
		{

		}
    }

}