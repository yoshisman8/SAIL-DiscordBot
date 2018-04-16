using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System.Threading.Tasks;

namespace ERA20.Modules
{
    public class Emote : ModuleBase<SocketCommandContext>
    {
        [Command("Emotes")]
        [Summary("ERA will DM you all the emotes on the server!")]
        [RequireContext(ContextType.Guild)]
        public async Task ListEmotes(){
            var sb = new StringBuilder();
            var Channel = await Context.User.GetOrCreateDMChannelAsync();
            var server = Context.Guild;
            foreach(var emote in server.Emotes){
                if (sb.Length >= 1800){
                    await Channel.SendMessageAsync(sb.ToString());
                    sb.Clear();
                }
                if (emote.Animated){
                    sb.AppendLine("<a:"+emote.Name+":"+emote.Id+"> "+"`"+emote.Name+"`");
                }
                else{
                    sb.AppendLine(emote+" `:"+emote.Name+":`");
                }
            }
            await Channel.SendMessageAsync(sb.ToString());
            sb.Clear();
        }
    }
}
