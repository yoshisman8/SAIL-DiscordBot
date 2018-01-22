using Discord.Commands;
using System;
using System.Threading.Tasks;
using Discord;

[Group("Slowmode")]
[RequireUserPermission(GuildPermission.ManageRoles)]
public class Slowmode : ModuleBase<SocketCommandContext>
{

    public Toggles Toggles {get;set;}

    [Command()]
    public async Task ToggleSlowmode(){
        if (Toggles.Slowmode){
            Toggles.Slowmode = false;
            await ReplyAsync("Slowmode turned off!");
        }
        else {
            Toggles.Slowmode = true;
            await ReplyAsync("Slowmode turned off with cooldown set to **"+Toggles.Cooldown+"** Minutes!");
        }
    }

    [Command()]
    public async Task SetTimer(int cooldown){
        Toggles.Cooldown = cooldown;
        await ReplyAsync("Cooldown for commands set to **"+Toggles.Cooldown+"** Minutes!");
    }
}