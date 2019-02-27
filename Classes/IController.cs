using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Discord;
using Discord.WebSocket;
using Discord.Commands;
using Discord.Addons.Interactive;

namespace SAIL.Classes
{
    public class Controller
    {
        public int Index {get;set;}
        public List<Embed> Pages {get;set;} = new List<Embed>();
        
        public async Task Next(SocketCommandContext c, SocketReaction r, IUserMessage msg)
        {
            if(Index+1 >= Pages.Count) 
            {
                await msg.RemoveReactionAsync(r.Emote,r.User.Value);
                return;
            }
            else
            {
                Index++;
                await msg.ModifyAsync(x => x.Embed = Pages.ElementAt(Index));
                await msg.RemoveReactionAsync(r.Emote,r.User.Value);
            }
        }
        public async Task Previous(SocketCommandContext c, SocketReaction r, IUserMessage msg)
        {
            if(Index-1 < 0) 
            {
                await msg.RemoveReactionAsync(r.Emote,r.User.Value);
                return;
            }
            else
            {
                Index--;
                await msg.ModifyAsync(x => x.Embed = Pages.ElementAt(Index));
                await msg.RemoveReactionAsync(r.Emote,r.User.Value);
            }
        }
        public async Task ToIndex(SocketCommandContext c, SocketReaction r, IUserMessage msg, int target)
        {
            if (Math.Abs(target) >= Pages.Count)
            {
                await msg.RemoveReactionAsync(r.Emote,r.User.Value);
                return;
            }
            else
            {
                await msg.ModifyAsync(x => x.Embed = Pages.ElementAt(target));
                await msg.RemoveReactionAsync(r.Emote,r.User.Value);
            }
        }
        public async Task First(SocketReaction r, IUserMessage msg)
        {
            await msg.ModifyAsync(x=> x.Embed = Pages.First());
            await msg.RemoveReactionAsync(r.Emote,r.User.Value);
        }
        public async Task Last(SocketReaction r, IUserMessage msg)
        {
            await msg.ModifyAsync(x=> x.Embed = Pages.Last());
            await msg.RemoveReactionAsync(r.Emote,r.User.Value);
        }
        public async Task Kill(InteractiveService interactive, IUserMessage msg)
        {
            await msg.RemoveAllReactionsAsync();
            interactive.RemoveReactionCallback(msg.Id);
        }
    }
}