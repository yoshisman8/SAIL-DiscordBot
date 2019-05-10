using System;
using System.Timers;
using System.Threading.Tasks;
using SAIL.Classes;
using Microsoft.Extensions.DependencyInjection;

namespace SAIL.Services
{
    public class ScheduleService
    {
        private readonly GlobalTimer Timer;
        
        public ScheduleService(GlobalTimer _timer)
        {
            Timer = _timer;

            Timer.OnSecondPassed += CheckEvents;
        }

        private async Task CheckEvents(DateTime MomentOfTrigger)
        {
            MomentOfTrigger = MomentOfTrigger.RoundUp(TimeSpan.FromMinutes(1));
            
        }
    }
}