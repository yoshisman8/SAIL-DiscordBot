using System;
using System.Timers;
using System.Threading.Tasks;
using SAIL.Classes;

namespace SAIL.Services
{
    public class ScheduleService
    {
        private Timer timer {get;set;} = new Timer(1000);
        public event Func<Task> Tick;
        private System.DateTime _last {get;set;}
        
        public ScheduleService()
        {
            Timer timer = new Timer();
            timer.Elapsed += timer_Tick;
            timer.Enabled = true;
        }
        private double MilliSecondsLeftTilTheHour()
        {
            double interval = (DateTime.Now.RoundUp(TimeSpan.FromMinutes(15))-DateTime.Now).TotalMilliseconds;
            if (interval == 0)
            {
                interval = TimeSpan.FromMinutes(15).TotalMilliseconds;
            }
            return interval;
        }
        void timer_Tick(object sender, EventArgs e)
        {
            timer.Interval = MilliSecondsLeftTilTheHour();
            if(Tick != null)
            {
                Tick();
            }
        }
    }
}