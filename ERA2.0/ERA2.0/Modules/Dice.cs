using System;
using System.Text;
using System.Threading.Tasks;
using Discord.Commands;
using DiceNotation;
using System.Text.RegularExpressions;


public class Diceroller : ModuleBase<SocketCommandContext>
{
    IDiceParser parser = new DiceParser();

    [Command("Roll")]
    [Summary("Rolls a die on a xdy expression format. \nUsage: `$Roll <dice expression>`.")]
    public async Task DieRoll([Remainder]string input)
    {
        
        var valid = System.Text.RegularExpressions.Regex.IsMatch(input.ToLower(), @"^[d-dk-k0-9\+\-\s\*]*$");
        if (!valid){
            await ReplyAsync(Context.User.Mention+" This is not a valid dice expression!");
            return;
        }
        var result = parser.Parse(input.ToLower()).Roll();
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
        await Context.Channel.SendMessageAsync(Context.User.Mention + ", Your Roll: " + steps + " = **" + result.Value+"**.");
    }

    [Command("Max")]
    [Summary("Shows the maximum possible roll for this dice roll")]
    public async Task Max([Remainder]string input){
        var valid = System.Text.RegularExpressions.Regex.IsMatch(input.ToLower(), @"^[d-dk-k0-9\+\-\*]*$");
        if (!valid){
            await ReplyAsync(Context.User.Mention+" This is not a valid dice expression!");
            return;
        }
        var result = parser.Parse(input.ToLower()).MaxRoll();
        await ReplyAsync(Context.User.Mention + ", The possible maximum roll for this expression is **"+result.Value+"**.");
    }
    [Command("Min")]
    [Summary("Shows the minimum possible roll for this dice roll")]
    public async Task Min([Remainder]string input){
        var valid = System.Text.RegularExpressions.Regex.IsMatch(input.ToLower(), @"^[d-dk-k0-9\+\-\*]*$");
        if (!valid){
            await ReplyAsync(Context.User.Mention+" This is not a valid dice expression!");
            return;
        }
        var result = parser.Parse(input.ToLower()).MinRoll();
        await ReplyAsync(Context.User.Mention + ", The possible minimum roll for this expression is **"+result.Value+"**.");
    }
}
