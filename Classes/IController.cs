using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Discord;
using Discord.WebSocket;
using Discord.Commands;

namespace SAIL.Classes
{
    public interface IController
    {
        int Index {get;set;}
        List<Embed> Pages {get;set;}
        
        Task Next(SocketCommandContext c, SocketReaction r, SocketMessage msg);
        Task Previous(SocketCommandContext c, SocketReaction r, SocketMessage msg);
        Task ToIndex(SocketCommandContext c, SocketReaction r, SocketMessage msg);
    }
}