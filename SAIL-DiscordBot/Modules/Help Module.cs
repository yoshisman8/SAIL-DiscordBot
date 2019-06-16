using System;
using System.Collections.Generic;
using System.Text;
using Discord.Commands;
using Discord.WebSocket;
using Discord.Addons.Interactive;
using Discord.Addons.CommandCache;
using SAIL.Classes;

namespace SAIL.Modules
{
	[Name("Help Module")] [Untoggleable]
	[Summary("Has all commands related to obtaining help.")]
    public class Help_Module : InteractiveBase<SocketCommandContext>
	{

	}
}
