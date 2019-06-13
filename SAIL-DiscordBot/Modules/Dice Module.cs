using System;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using Discord.Commands;
using Discord.Addons.CommandCache;
using System.Text.RegularExpressions;
using SAIL.Classes;
using OnePlat.DiceNotation;
using OnePlat.DiceNotation.DieRoller;

namespace SAIL.Modules
{
    [Name("Dice Roller")]
    [Summary("This module contains multiple commands for rolling all sorts of dice. Useful for games and roleplay!\n More info about dice notation [here](https://github.com/DarthPedro/OnePlat.DiceNotation/blob/master/docs/DiceNotationExamples.md).")]
    public class DiceModule : ModuleBase<SocketCommandContext>
    {
		public CommandCacheService cache { get; set; }
		DiceParser Parser = new DiceParser();

        [Command("Roll"), Alias("r")]
        [RequireGuildSettings]
        [Summary("Rolls a die on the dice notation format. More info about dice notation [here](https://github.com/DarthPedro/OnePlat.DiceNotation/blob/master/docs/DiceNotationExamples.md).")]
        public async Task DieRoll([Remainder]string DiceExpression = "1d20")
        {
			try
			{
				var result = Parser.Parse(DiceExpression, new DiceConfiguration() { DefaultDieSides = 20 }, new MathNetDieRoller());
				if (cache.Any(x => x.Key == Context.Message.Id)) cache.Remove(Context.Message.Id);
				await Context.Message.DeleteAsync();

				await ReplyAsync("[" + DiceExpression + "] ⇒ " + result.Value);
			}
			catch (Exception e)
			{
				var msg = await ReplyAsync("Error, your dice expression is incorrect!");
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
				var result = Parser.Parse(DiceExpression, new DiceConfiguration() { DefaultDieSides = 20 }, new MaxDieRoller());
				if (cache.Any(x => x.Key == Context.Message.Id)) cache.Remove(Context.Message.Id);

				await Context.Message.DeleteAsync();
				await ReplyAsync("Maximum result for [" + DiceExpression + "] ⇒ " + result.Value);
			}
			catch (Exception e)
			{
				var msg = await ReplyAsync("Error, your dice expression is incorrect!");
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
				var result = Parser.Parse(DiceExpression, new DiceConfiguration() { DefaultDieSides = 20 }, new ConstantDieRoller(1));
				if (cache.Any(x => x.Key == Context.Message.Id)) cache.Remove(Context.Message.Id);

				await Context.Message.DeleteAsync();
				await ReplyAsync("Minimum result for [" + DiceExpression + "] ⇒ " + result.Value);
			}
			catch (Exception e)
			{
				var msg = await ReplyAsync("Error, your dice expression is incorrect!");
				cache.Add(Context.Message.Id, msg.Id);
			}
		}
		public async Task Autoroll(SocketCommandContext context, [Remainder]string DiceExpression)
		{

		}
    }

	public class MaxDieRoller : RandomDieRollerBase
	{
		protected override int GetNextRandom(int sides)
		{
			return sides;
		}
	}
}