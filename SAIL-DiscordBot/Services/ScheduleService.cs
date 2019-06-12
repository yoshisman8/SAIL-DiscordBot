using System;
using System.Timers;
using System.Threading.Tasks;
using SAIL.Classes;
using Microsoft.Extensions.DependencyInjection;
using LiteDB;

namespace SAIL.Services
{
    public class ScheduleService
    {
        private readonly GlobalTimer Timer;
        private readonly LiteDatabase Database;
        
        public ScheduleService(GlobalTimer _timer,LiteDatabase _db)
        {
            Timer = _timer;
            Database = _db;

            Timer.OnSecondPassed += CheckEvents;
        }

        private async Task CheckEvents(DateTime MomentOfTrigger)
        {
            if ((MomentOfTrigger.Minute%15) != 0)
            {
                return;
            }
        }
    }
}