using System;
using System.Timers;
using System.Threading.Tasks;
using SAIL.Classes;
using Microsoft.Extensions.DependencyInjection;

namespace SAIL.Services
{
    public class GlobalTimer
    {
        private Timer Clock = new Timer();
        private readonly IServiceProvider _provider;
        public event ClockEventHandler OnSecondPassed;
        public delegate Task ClockEventHandler(DateTime MomentOfTrigger);
        public GlobalTimer(IServiceProvider provider)
        {
            _provider = provider;

            Clock.Elapsed += Tick;
            Clock.AutoReset = true;
            Clock.Interval = TimeSpan.FromMilliseconds(10).Milliseconds;
            Clock.Enabled = true;
        }

        private void Tick(object sender, ElapsedEventArgs e)
        {
            OnSecondPassed?.Invoke(DateTime.Now);
        }
    }
}
