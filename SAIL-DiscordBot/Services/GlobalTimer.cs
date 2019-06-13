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
        public event ClockEventHandler OnMinutePass;
        public delegate Task ClockEventHandler(DateTime MomentOfTrigger);

		private const long MILLISECOND_IN_MINUTE = 60 * 1000;
		private const long TICKS_IN_MILLISECOND = 10000;
		private const long TICKS_IN_MINUTE = MILLISECOND_IN_MINUTE * TICKS_IN_MILLISECOND;
		private long nextIntervalTick;

		public GlobalTimer(IServiceProvider provider)
        {
            _provider = provider;

            Clock.AutoReset = true;
            Clock.Interval = GetInitialInterval();   
            Clock.Elapsed += Tick;
			Clock.Start();
        }

        private void Tick(object sender, ElapsedEventArgs e)
        {
			Clock.Interval = GetInterval();
            if(e.SignalTime.Second == 0) OnMinutePass?.Invoke(e.SignalTime);
        }
		private double GetInitialInterval()
		{
			DateTime now = DateTime.Now;
			double timeToNextMin = ((60 - now.Second) * 1000 - now.Millisecond) + 15;
			nextIntervalTick = now.Ticks + ((long)timeToNextMin * TICKS_IN_MILLISECOND);

			return timeToNextMin;
		}
		private double GetInterval()
		{
			nextIntervalTick += TICKS_IN_MINUTE;
			return TicksToMs(nextIntervalTick - DateTime.Now.Ticks);
		}
		private double TicksToMs(long ticks)
		{
			return (double)(ticks / TICKS_IN_MILLISECOND);
		}
	}
}
