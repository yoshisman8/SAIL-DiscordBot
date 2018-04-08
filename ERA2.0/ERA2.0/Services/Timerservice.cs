using System;
using System.Threading; // 1) Add this namespace
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Discord.Addons.Interactive;
using LiteDB;
using System.Globalization;
using System.Text;
using ERA20.Modules;

namespace ERA20.Services
{
    public class TimerService
{
    private readonly Timer _timer; // 2) Add a field like this
    // This example only concerns a single timer.
    // If you would like to have multiple independant timers,
    // you could use a collection such as List<Timer>,
    // or even a Dictionary<string, Timer> to quickly get
    // a specific Timer instance by name.
    public LiteDatabase Database;
    public TimerService(DiscordSocketClient client)
    {
        _timer = new Timer(async _ =>
        {
            if (Database != null){
                var col = Database.GetCollection<ScheduledEvents>("Events");
                var dt = DateTime.UtcNow.AddSeconds(-DateTime.UtcNow.Second).AddMilliseconds(-DateTime.UtcNow.Millisecond);
                var CurrentTime = new EventTime().DateTimeToEventTime(dt); CurrentTime.DayOfWeek = DayOfWeek.Monday;
                ITextChannel Channel = client.GetChannel(390586066723143691) as ITextChannel;
                var Events = col.Find(x => x.ScheduledTime == CurrentTime);
                if (Events.Count() != 0) {
                    foreach (var x in Events){
                        await Channel.SendMessageAsync("**EVENT ALERT**: "+x.Name+
                        "\nSchedule: "+x.ScheduledTime.DayOfWeek+" at "+x.ScheduledTime.Hour+":"+x.ScheduledTime.Minute+
                        "\nEvent Description: "+x.Description);
                        if(x.Disposable == true){
                            col.Delete(x.Id);
                        }
                    }
                }
                CurrentTime.DayOfYear = 0;
                CurrentTime.DayOfWeek = DateTime.UtcNow.DayOfWeek;
                var Recurents = col.Find(x => x.ScheduledTime == CurrentTime);
                if(Recurents.Count() != 0){
                    foreach(var x in Recurents){
                        await Channel.SendMessageAsync("**EVENT ALERT**: "+x.Name+
                        "\nSchedule: Every "+x.ScheduledTime.DayOfWeek+" at "+x.ScheduledTime.Hour+":"+x.ScheduledTime.Minute+
                        "\nEvent Description: "+x.Description);
                    }
                }
            }
        },
        null,
        TimeSpan.FromTicks(1),  // 4) Time that message should fire after the timer is created
        TimeSpan.FromMinutes(1)); // 5) Time after which message should repeat (use `Timeout.Infinite` for no repeat)
    }

    public void Stop() // 6) Example to make the timer stop running
    {
        _timer.Change(Timeout.Infinite, Timeout.Infinite);
    }

    public void Restart() // 7) Example to restart the timer
    {
        _timer.Change(TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(60));
    }

    public void SetUpDatabase(LiteDatabase database){
        Database = database;
    }   
}
}