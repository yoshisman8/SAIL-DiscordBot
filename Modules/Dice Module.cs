using System;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using Discord.Commands;
using DiceNotation;
using System.Text.RegularExpressions;
using Discord.Addons.CommandCache;


namespace SAIL.Modules
{
    [Name("Dice Roller")]
    [Summary("This module contains multiple commands for rolling all sorts of dice. Useful for games and roleplay!")]
    public class Diceroller : CommandCacheModuleBase<SocketCommandContext>
    {
        IDiceParser parser = new DiceParser();

        [Command("Roll"), Alias("r")]
        [Summary("Rolls a die on a xdy + z expression format.")]
        public async Task DieRoll([Remainder]string DiceExpression = "d20")
        {
            string[] sDiceExpression = new string[0];
            if (DiceExpression.Contains(">>")){
                sDiceExpression = DiceExpression.Split(">>",StringSplitOptions.RemoveEmptyEntries);
                if (sDiceExpression.Length == 1){
                    await ReplyAsync("You didn't add a conditional!");
                    return;
                }
                if (sDiceExpression.Length > 2){
                    await ReplyAsync("You can't add more than one conditional!");
                    return;
                }
                if (!int.TryParse(sDiceExpression[1], out int n)){
                    await ReplyAsync("This expression has an invalid/non-numeric conditional!");
                    return;
                }
                int conditional = int.Parse(sDiceExpression[1]);
                var valid = System.Text.RegularExpressions.Regex.IsMatch(sDiceExpression[0].ToLower(), @"^[d-dk-k0-9\+\-\s\*]*$");
                if (!valid){
                    await ReplyAsync(Context.User.Mention+" This is not a valid dice expression!");
                    return;
                }
                var result = parser.Parse(sDiceExpression[0].ToLower()).Roll();
                string steps = "";
                var successes = result.Results.Where(x => x.Value >= conditional);
                foreach (var x in result.Results){
                    if (x.Value >= conditional){
                        steps += "**"+ x.Value + "**, ";
                    }
                    else{
                        steps += x.Value + ", ";
                    }
                }
                steps = steps.Substring(0,steps.Length - 2);
                if (successes.Count() == 0){
                    await ReplyAsync(Context.User.Mention+" has **failed** all their rolls! ("+steps+")");
                    return;
                }
                else {
                    await ReplyAsync(Context.User.Mention+" has **succeeded "+successes.Count()+" times.** ("+steps+")");
                    return;
                }
            }

            if (DiceExpression.Contains(">")){
                sDiceExpression = DiceExpression.Split(">",StringSplitOptions.RemoveEmptyEntries);
                if (sDiceExpression.Length == 1){
                    await ReplyAsync("You didn't add a conditional!");
                    return;
                }
                if (sDiceExpression.Length > 2){
                    await ReplyAsync("You can't add more than one conditional!");
                    return;
                }
                if (!int.TryParse(sDiceExpression[1], out int n)){
                    await ReplyAsync("This expression has an invalid/non-numeric conditional!");
                    return;
                }
                int conditional = int.Parse(sDiceExpression[1]);
                var valid = System.Text.RegularExpressions.Regex.IsMatch(sDiceExpression[0].ToLower(), @"^[d-dk-k0-9\+\-\s\*]*$");
                if (!valid){
                    await ReplyAsync(Context.User.Mention+" This is not a valid dice expression!");
                    return;
                }
                var result = parser.Parse(sDiceExpression[0].ToLower()).Roll();
                string steps = "";
                foreach(var x in result.Results){
                if (x.Scalar == -1){
                    steps += "-"+x.Value + " + ";
                }
                else if (x.Scalar >= 2 || x.Scalar <= -2){
                    steps += x.Value+"x"+x.Scalar + " + ";
                }
                else {
                steps += x.Value + " + ";
                }
            }
                steps = steps.Substring(0,steps.Length - 2);
                if (result.Value < conditional){
                    await ReplyAsync(Context.User.Mention+" has **failed**. (Total: "+result.Value+", lower than "+conditional+"; "+steps+")");
                    return;
                }
                else {
                    await ReplyAsync(Context.User.Mention+" has **succeeded**. "+ "(Total: "+result.Value+", greater than "+conditional+"; "+steps+")");
                    return;
                }
            }

            if (DiceExpression.Contains("<<")){
                sDiceExpression = DiceExpression.Split("<<",StringSplitOptions.RemoveEmptyEntries);
                if (sDiceExpression.Length == 1){
                    await ReplyAsync("You didn't add a conditional!");
                    return;
                }
                if (sDiceExpression.Length > 2){
                    await ReplyAsync("You can't add more than one conditional!");
                    return;
                }
                if (!int.TryParse(sDiceExpression[1], out int n)){
                    await ReplyAsync("This expression has an invalid/non-numeric conditional!");
                    return;
                }
                int conditional = int.Parse(sDiceExpression[1]);
                var valid = System.Text.RegularExpressions.Regex.IsMatch(sDiceExpression[0].ToLower(), @"^[d-dk-k0-9\+\-\s\*]*$");
                if (!valid){
                    await ReplyAsync(Context.User.Mention+" This is not a valid dice expression!");
                    return;
                }
                var result = parser.Parse(sDiceExpression[0].ToLower()).Roll();
                string steps = "";
                var successes = result.Results.Where(x => x.Value < conditional);
                foreach (var x in result.Results){
                    if (x.Value < conditional){
                        steps += "**"+ x.Value + "**, ";
                    }
                    else{
                        steps += x.Value + ", ";
                    }
                }
                steps = steps.Substring(0,steps.Length - 2);
                if (successes.Count() == 0){
                    await ReplyAsync(Context.User.Mention+" has **failed** all their rolls! ("+steps+")");
                    return;
                }
                else {
                    await ReplyAsync(Context.User.Mention+" has **succeeded "+successes.Count()+" times.** ("+steps+")");
                    return;
                }
            }

            if (DiceExpression.Contains("<")){
                sDiceExpression = DiceExpression.Split("<",StringSplitOptions.RemoveEmptyEntries);
                if (sDiceExpression.Length == 1){
                    await ReplyAsync("You didn't add a conditional!");
                    return;
                }
                if (sDiceExpression.Length > 2){
                    await ReplyAsync("You can't add more than one conditional!");
                    return;
                }
                if (!int.TryParse(sDiceExpression[1], out int n)){
                    await ReplyAsync("This expression has an invalid/non-numeric conditional!");
                    return;
                }
                int conditional = int.Parse(sDiceExpression[1]);
                var valid = System.Text.RegularExpressions.Regex.IsMatch(sDiceExpression[0].ToLower(), @"^[d-dk-k0-9\+\-\s\*]*$");
                if (!valid){
                    await ReplyAsync(Context.User.Mention+" This is not a valid dice expression!");
                    return;
                }
                var result = parser.Parse(sDiceExpression[0].ToLower()).Roll();
                string steps = "";
                foreach(var x in result.Results){
                if (x.Scalar == -1){
                    steps += "-"+x.Value + " + ";
                }
                else if (x.Scalar >= 2 || x.Scalar <= -2){
                    steps += x.Value+"x"+x.Scalar + " + ";
                }
                else {
                steps += x.Value + " + ";
                }
            }
                steps = steps.Substring(0,steps.Length - 2);
                if (result.Value > conditional){
                    await ReplyAsync(Context.User.Mention+" has **failed**. (Total: "+result.Value+", lower than "+conditional+"; "+steps+")");
                    return;
                }
                else {
                    await ReplyAsync(Context.User.Mention+" has **succeeded**. "+ "(Total: "+result.Value+", greater than "+conditional+"; "+steps+")");
                    return;
                }
            }
            else {
            var valid = System.Text.RegularExpressions.Regex.IsMatch(DiceExpression.ToLower(), @"^[d-dk-k0-9\+\-\s\*]*$");
            if (!valid){
                await ReplyAsync(Context.User.Mention+" This is not a valid dice expression!");
                return;
            }
            var result = parser.Parse(DiceExpression.ToLower()).Roll();
            string steps = "";
            foreach(var x in result.Results){
                if (x.Scalar == -1){
                    steps += "-"+x.Value + " + ";
                }
                else if (x.Scalar >= 2 || x.Scalar <= -2){
                    steps += x.Value+"x"+x.Scalar + " + ";
                }
                else {
                steps += x.Value + " + ";
                }
            }
            steps = steps.Substring(0,steps.Length - 2).Replace(" + -", " - ");
            await ReplyAsync(Context.User.Mention + ", Your Roll: " + steps + " = **" + result.Value+"**.");
            }
        }

        [Command("Max")]
        [Summary("Shows the maximum possible roll for this dice roll.")]
        public async Task Max([Remainder]string DiceExpression){
            var valid = System.Text.RegularExpressions.Regex.IsMatch(DiceExpression.ToLower(), @"^[d-dk-k0-9\+\-\s\*]*$");
            if (!valid){
                await ReplyAsync(Context.User.Mention+" This is not a valid dice expression!");
                return;
            }
            var result = parser.Parse(DiceExpression.ToLower()).MaxRoll();
            await ReplyAsync(Context.User.Mention + ", The possible maximum roll for this expression is **"+result.Value+"**.");
        }
        [Command("Min")]
        [Summary("Shows the minimum possible roll for this dice roll.")]
        public async Task Min([Remainder]string DiceExpression){
            var valid = System.Text.RegularExpressions.Regex.IsMatch(DiceExpression.ToLower(), @"^[d-dk-k0-9\+\-\s\*]*$");
            if (!valid){
                await ReplyAsync(Context.User.Mention+" This is not a valid dice expression!");
                return;
            }
            var result = parser.Parse(DiceExpression.ToLower()).MinRoll();
            await ReplyAsync(Context.User.Mention + ", The possible minimum roll for this expression is **"+result.Value+"**.");
        }
        [Command("Fate")]
        [Summary("Roll a set of [Fate dice](https://fate-srd.com/fate-core/what-you-need-play). This command only accepts non-decimal numbers added to it (Such as -5 or +2).")]
        public async Task FateRoll ([Remainder] int DiceExpression = 0)
        {
            var results = parser.Parse("4d6").Roll();
            var dice = new List<string>();
            int value = DiceExpression;
            string Rating = "Invalid";
            foreach (var x in results.Results)
            {
                switch (x.Value)
                {
                    case 1:
                        dice.Add("[➖]");
                        value--;
                        break;
                    case 2:
                        dice.Add("[➖]");
                        value--;
                        break;
                    case 3:
                        dice.Add("[⬛]");
                        break;
                    case 4:
                        dice.Add("[⬛]");
                        break;
                    case 5:
                        dice.Add("[➕]");
                        value++;
                        break;
                    case 6:
                        dice.Add("[➕]");
                        value++;
                        break;
                }
            }
            if (value > 8) Rating = "Legendary+";
            else if(value < -2) Rating = "Terrible+";
            else Rating = FateLadder.GetValueOrDefault(value);
            await ReplyAsync(Context.User.Mention+", You got a "+Rating+" ("+value+") roll."+
                "\n"+string.Join(", ",dice)+" "+(DiceExpression == 0 ? "" :"+ ("+DiceExpression.ToString()+")")+".");
            
        }
        private Dictionary<int,string> FateLadder = new Dictionary<int, string>()
        {
            {8,"Legendary"},
            {7,	"Epic"},
            {6,	"Fantastic"},
            {5,	"Superb"},
            {4,	"Great"},
            {3,	"Good"},
            {2,	"Fair"},
            {1,	"Average"},
            {0,	"Mediocre"},
            {-1, "Poor"},
            {-2, "Terrible"}
        };
        public async Task Autoroll(SocketCommandContext context, [Remainder]string DiceExpression)
        {
            string[] sDiceExpression = new string[0];
            if (DiceExpression.Contains(">>")){
                sDiceExpression = DiceExpression.Split(">>",StringSplitOptions.RemoveEmptyEntries);
                if (sDiceExpression.Length == 1){
                    await context.Channel.SendMessageAsync("You didn't add a conditional!");
                    return;
                }
                if (sDiceExpression.Length > 2){
                    await context.Channel.SendMessageAsync("You can't add more than one conditional!");
                    return;
                }
                if (!int.TryParse(sDiceExpression[1], out int n)){
                    await context.Channel.SendMessageAsync("This expression has an invalid/non-numeric conditional!");
                    return;
                }
                int conditional = int.Parse(sDiceExpression[1]);
                var valid = System.Text.RegularExpressions.Regex.IsMatch(sDiceExpression[0].ToLower(), @"^[d-dk-k0-9\+\-\s\*]*$");
                if (!valid){
                    await context.Channel.SendMessageAsync(context.User.Mention+" This is not a valid dice expression!");
                    return;
                }
                var result = parser.Parse(sDiceExpression[0].ToLower()).Roll();
                string steps = "";
                var successes = result.Results.Where(x => x.Value >= conditional);
                foreach (var x in result.Results){
                    if (x.Value >= conditional){
                        steps += "**"+ x.Value + "**, ";
                    }
                    else{
                        steps += x.Value + " + ";
                    }
                }
                steps = steps.Substring(0,steps.Length - 2);
                if (successes.Count() == 0){
                    await context.Channel.SendMessageAsync(context.User.Mention+" has **failed** all their rolls! ("+steps+")");
                    return;
                }
                else {
                    await context.Channel.SendMessageAsync(context.User.Mention+" has **succeeded "+successes.Count()+" times.** ("+steps+")");
                    return;
                }
            }

            if (DiceExpression.Contains(">")){
                sDiceExpression = DiceExpression.Split(">",StringSplitOptions.RemoveEmptyEntries);
                if (sDiceExpression.Length == 1){
                    await context.Channel.SendMessageAsync("You didn't add a conditional!");
                    return;
                }
                if (sDiceExpression.Length > 2){
                    await context.Channel.SendMessageAsync("You can't add more than one conditional!");
                    return;
                }
                if (!int.TryParse(sDiceExpression[1], out int n)){
                    await context.Channel.SendMessageAsync("This expression has an invalid/non-numeric conditional!");
                    return;
                }
                int conditional = int.Parse(sDiceExpression[1]);
                var valid = System.Text.RegularExpressions.Regex.IsMatch(sDiceExpression[0].ToLower(), @"^[d-dk-k0-9\+\-\s\*]*$");
                if (!valid){
                    await context.Channel.SendMessageAsync(context.User.Mention+" This is not a valid dice expression!");
                    return;
                }
                var result = parser.Parse(sDiceExpression[0].ToLower()).Roll();
                string steps = "";
                foreach(var x in result.Results){
                if (x.Scalar == -1){
                    steps += "-"+x.Value + " + ";
                }
                else if (x.Scalar >= 2 || x.Scalar <= -2){
                    steps += x.Value+"x"+x.Scalar + " + ";
                }
                else {
                steps += x.Value + " + ";
                }
            }
                steps = steps.Substring(0,steps.Length - 2);
                if (result.Value < conditional){
                    await context.Channel.SendMessageAsync(context.User.Mention+" has **failed**. (total: "+result.Value+", lower than "+conditional+"; "+steps+")");
                    return;
                }
                else {
                    await context.Channel.SendMessageAsync(context.User.Mention+" has **succeeded**. "+ "(total: "+result.Value+", greater than "+conditional+"; "+steps+")");
                    return;
                }
            }

            else {
            var valid = System.Text.RegularExpressions.Regex.IsMatch(DiceExpression.ToLower(), @"^[d-dk-k0-9\+\-\s\*]*$");
            if (!valid){
                await context.Channel.SendMessageAsync(context.User.Mention+" This is not a valid dice expression!");
                return;
            }
            var result = parser.Parse(DiceExpression.ToLower()).Roll();
            string steps = "";
            foreach(var x in result.Results){
                if (x.Scalar == -1){
                    steps += "-"+x.Value + " + ";
                }
                else if (x.Scalar >= 2 || x.Scalar <= -2){
                    steps += x.Value+"x"+x.Scalar + " + ";
                }
                else {
                steps += x.Value + " + ";
                }
            }
            steps = steps.Substring(0,steps.Length - 2).Replace(" + -", " - ");
            await context.Channel.SendMessageAsync(context.User.Mention + ", Your Roll: " + steps + " = **" + result.Value+"**.");
            }
        }
         
    }
}