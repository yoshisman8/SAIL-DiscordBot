using System;
using System.Linq;
using System.Timers;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Discord;
using Discord.WebSocket;
using SAIL.Classes;
using LiteDB;

namespace SAIL.Services
{
    public class ScheduleService
    {
        private readonly GlobalTimer Timer;
		private readonly DiscordSocketClient client;

		public ScheduleService(GlobalTimer _timer,LiteDatabase _db,DiscordSocketClient _client)
        {
			client = _client;
            Timer = _timer;

            Timer.OnMinutePass += CheckEvents;
        }

        private async Task CheckEvents(DateTime MomentOfTrigger)
        {
            if ((MomentOfTrigger.Minute%15) != 0)
            {
                return;
            }

			var GuildCol = Program.Database.GetCollection<SysGuild>("Guilds");
			var Events= Program.Database.GetCollection<GuildEvent>("Events").IncludeAll().Find(x=>x.Date.TimeOfDay==MomentOfTrigger.TimeOfDay);
			
			foreach(var x in Events)
			{
				switch (x.Repeating)
				{
					case RepeatingState.Anually:
						if (x.Date.DayOfYear != MomentOfTrigger.DayOfYear) continue;
						break;
					case RepeatingState.Monhtly:
						if (x.Date.Day != MomentOfTrigger.Day) continue;
						break;
					case RepeatingState.Weekly:
						if (x.Date.DayOfWeek != MomentOfTrigger.DayOfWeek) continue;
						break;
					case RepeatingState.Once:
						if (x.Date.DayOfYear != MomentOfTrigger.DayOfYear) continue;
						break;
					default:
						continue;
				}
				await x.Server.PrintEvent(client, x);
			}
        }
    }
}