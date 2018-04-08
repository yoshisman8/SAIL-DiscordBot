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
using ERA20.Services;

namespace ERA20.Modules{
    public class ScheduledEvents{
    [BsonId]
    public int Id {get;set;}
    public string Name {get;set;}
    public string Description {get;set;}
    public EventTime ScheduledTime {get;set;} = new EventTime();
    public DateTime FixedDate {get;set;}
    public bool Disposable {get;set;} = false;

}
public class EventTime{
    public DayOfWeek DayOfWeek {get;set;}
    public int DayOfYear {get;set;} = 0;
    public int Hour {get;set;}
    public int Minute {get;set;}
    public EventTime DateTimeToEventTime(DateTime dateTime)
    {
        return new EventTime(){
            DayOfWeek = dateTime.DayOfWeek,
            Hour = dateTime.Hour,
            Minute = dateTime.Minute,
            DayOfYear = dateTime.DayOfYear
        };
    }
}

public class TimerModule : InteractiveBase<SocketCommandContext>
{
    private TimerService _service;
    public LiteDatabase Database {get;set;}

    public TimerModule(TimerService service) // Make sure to configure your DI with your TimerService instance
    {
        _service = service;
    }

    // Example commands
    [Command("NewRecurrentEvent"), Alias("NRE")]
    [RequireUserPermission(ChannelPermission.ManageMessages)]
    [Summary("Create a new event to add to the event database. Usage: `/NewRecurrentEvent <Name of the Event> <Description of the event> <Day of the week> <Hour in which it happens (in 24h format)>`.\n"+
        "Note: The date")]
    public async Task NewEvent(string Name, string Description, string Day, string Time)
    {
        var col = Database.GetCollection<ScheduledEvents>("Events");

        if (col.Exists(x => x.Name == Name.ToLower())){
            await ReplyAsync("There's already an event with that name! Pick a different name for the event please!");
            return;
        }
        var Event = new ScheduledEvents(){
            Name = Name,
            Description = Description,
        };
        try {
            Enum.TryParse<DayOfWeek>(Day, out var r);
            var h = DateTime.ParseExact(Time,"HH:mm",CultureInfo.InvariantCulture);
            Event.ScheduledTime.DayOfWeek = (DayOfWeek) Enum.Parse(typeof(DayOfWeek),Day);
            Event.ScheduledTime.Hour = h.Hour;
            Event.ScheduledTime.Minute = h.Minute;
            Event.ScheduledTime.DayOfYear = 0;

            col.Insert(Event);
            col.EnsureIndex(x => x.Name, "LOWER($.Name)");
            col.EnsureIndex(x => x.ScheduledTime);
            col.EnsureIndex(x=> x.ScheduledTime.DayOfYear);
            await ReplyAndDeleteAsync("Event **"+Event.Name+"** Created successfully! This event will be broadcasted on the Server Event's channel every "+Event.ScheduledTime.DayOfWeek+" at "+Event.ScheduledTime.Hour+":"+Event.ScheduledTime.Minute+" UTC time.", timeout: TimeSpan.FromSeconds(10));
        }
        catch {
            await ReplyAndDeleteAsync("It seems something was incorrect. Make sure you spelt out the day of the week and the time (in 24h format) correctly!", timeout: TimeSpan.FromSeconds(5));
        }
    }
    [Command("NewSingleEvent"), Alias("NSE")]
    [RequireUserPermission(ChannelPermission.ManageMessages)]
    [Summary("Adds a single-run event (Delets itself after it transpires) to the database. Usage: `/NSE <Name> <Description> <MM-dd date> <hh:mm Hour>`\n"+
        "Note: Date **needs** to have that dash (eg: 5-22)")]
    public async Task NewSEvent(string Name, string Description, string Date, string Time)
    {
        var col = Database.GetCollection<ScheduledEvents>("Events");

        if (col.Exists(x => x.Name == Name.ToLower())){
            await ReplyAsync("There's already an event with that name! Pick a different name for the event please!");
            return;
        }
        var Event = new ScheduledEvents(){
            Name = Name,
            Description = Description,
        };
        try {
            var h = DateTime.ParseExact(Time,"HH:mm",CultureInfo.InvariantCulture);
            var d = DateTime.ParseExact(Date, "MM-dd",CultureInfo.InvariantCulture);
            Event.ScheduledTime.DayOfWeek = DayOfWeek.Monday;
            Event.ScheduledTime.DayOfYear = d.DayOfYear;
            Event.ScheduledTime.Hour = h.Hour;
            Event.ScheduledTime.Minute = h.Minute;
            Event.Disposable = true;

            col.Insert(Event);
            col.EnsureIndex(x => x.Name, "LOWER($.Name)");
            col.EnsureIndex(x => x.ScheduledTime);
            col.EnsureIndex(x=> x.ScheduledTime.DayOfYear);
            await ReplyAndDeleteAsync("Event **"+Event.Name+"** Created successfully! This event will be broadcasted on the Server Event's channel every "+Event.ScheduledTime.DayOfWeek+" at "+Event.ScheduledTime.Hour+":"+Event.ScheduledTime.Minute+" UTC time.", timeout: TimeSpan.FromSeconds(10));
        }
        catch {
            await ReplyAndDeleteAsync("It seems something was incorrect. Make sure you spelt out the day of the week and the time (in 24h format) correctly!", timeout: TimeSpan.FromSeconds(5));
        }
    }
    [Command("NewYearlyEvent"), Alias("NYE")]
    [RequireUserPermission(ChannelPermission.ManageMessages)]
    [Summary("Adds a yearly event to the database. Usage: `/NYE <Name> <Description> <MM-dd date> <hh:mm Hour>`\n"+
        "Note: Date **needs** to have that dash (eg: 5-22)")]
    public async Task NewYEvent(string Name, string Description, string Date, string Time)
    {
        var col = Database.GetCollection<ScheduledEvents>("Events");

        if (col.Exists(x => x.Name == Name.ToLower())){
            await ReplyAsync("There's already an event with that name! Pick a different name for the event please!");
            return;
        }
        var Event = new ScheduledEvents(){
            Name = Name,
            Description = Description,
        };
        try {
            var h = DateTime.ParseExact(Time,"HH:mm",CultureInfo.InvariantCulture);
            var d = DateTime.ParseExact(Date, "MM-dd",CultureInfo.InvariantCulture);
            Event.ScheduledTime.DayOfWeek = DayOfWeek.Monday;
            Event.ScheduledTime.DayOfYear = d.DayOfYear;
            Event.ScheduledTime.Hour = h.Hour;
            Event.ScheduledTime.Minute = h.Minute;
            Event.Disposable = false;
            
            col.Insert(Event);
            col.EnsureIndex(x => x.Name, "LOWER($.Name)");
            col.EnsureIndex(x => x.ScheduledTime);
            col.EnsureIndex(x=> x.ScheduledTime.DayOfYear);
            await ReplyAndDeleteAsync("Event **"+Event.Name+"** Created successfully! This event will be broadcasted on the Server Event's channel every "+Date+" at "+Event.ScheduledTime.Hour+":"+Event.ScheduledTime.Minute+" UTC time.", timeout: TimeSpan.FromSeconds(10));
        }
        catch {
            await ReplyAndDeleteAsync("It seems something was incorrect. Make sure you spelt out the day of the week and the time (in 24h format) correctly!", timeout: TimeSpan.FromSeconds(5));
        }
    }
    [Command("DeleteEvent")]
    [RequireUserPermission(ChannelPermission.ManageMessages)]
    public async Task Delete(string Name)
    {
        var col = Database.GetCollection<ScheduledEvents>("Events");
        var Event = col.FindOne(x => x.Name.StartsWith(Name.ToLower()));
        if (Event == null) {
            await ReplyAndDeleteAsync("There is no scheduled event with this name",timeout: TimeSpan.FromSeconds(5)); 
            return;
        }
        col.Delete(Event.Id);
        await ReplyAsync("Event \""+Event.Name+"\" deleted from the record.");
    }
    [Command("Events"), Alias("Calendar")]
    [Summary("Shows all upcomming events for today and The next 3 days")]
    public async Task Calendar(){
        var col = Database.GetCollection<ScheduledEvents>("Events");
        var dt = DateTime.UtcNow.AddSeconds(-DateTime.UtcNow.Second).AddMilliseconds(-DateTime.UtcNow.Millisecond);
        var CurrentTime = new EventTime().DateTimeToEventTime(dt); CurrentTime.DayOfWeek = DayOfWeek.Monday;
        var Events = col.Find(x => x.ScheduledTime.DayOfYear == CurrentTime.DayOfYear);
        CurrentTime.DayOfYear = 0;
        CurrentTime.DayOfWeek = DateTime.UtcNow.DayOfWeek;
        var weeklies = col.Find(x => x.ScheduledTime.DayOfYear == 0);

        var sb = new StringBuilder();

        var embed = new EmbedBuilder()
        .WithAuthor(Context.Client.CurrentUser)
        .WithTitle("Calendar")
        .WithCurrentTimestamp();
        foreach(var x in Events){
            sb.AppendLine(x.Name +"["+x.ScheduledTime.Hour+":"+x.ScheduledTime.Minute+"]");
        }
        if (sb.ToString() == "") sb.AppendLine("There are no Yearly/Special Events today.");
        embed.AddField("Today's Events",sb.ToString(),true);
        sb.Clear();
        foreach(var x in weeklies){
            sb.AppendLine(x.Name +"["+x.ScheduledTime.DayOfWeek+" "+x.ScheduledTime.Hour+":"+x.ScheduledTime.Minute+"]");
        }
        if (sb.ToString() == "") sb.AppendLine("There are no Events set to run weekly.");
        embed.AddField("Weekly Events", sb.ToString(),true);
        await ReplyAsync("", embed: embed.Build());
    }
    [Command("Start")]
    [RequireUserPermission(GuildPermission.ManageRoles)]
    public async Task Start(){
        _service.SetUpDatabase(Database);
        _service.Restart();
        await ReplyAndDeleteAsync("Timer service started.", timeout: TimeSpan.FromSeconds(5));
    }
}
}