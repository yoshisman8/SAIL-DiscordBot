using System;
using Discord.WebSocket;
using Discord.Commands;
using System.Linq;
using System.Collections.Generic;
using Discord;
using System.Threading.Tasks;
using LiteDB;

public class CommandTimer {
    public class User{
        [BsonId]
        public ulong ID {get;set;}
        public DateTime LastCommandUsed {get;set;}
        public List<Command> Commands {get;set;}

    }

    public class Command{
        public string Name {get;set;}
        public DateTime LastUsed {get;set;}
    }
    public async Task<bool> ValidateTimer(SocketCommandContext context, LiteDatabase database, TimeSpan Cooldown, CommandService service){
        var col = database.GetCollection<User>("User");
        bool N = false;
        if (!col.Exists(x => x.ID == context.User.Id)){
            col.Insert(new User(){
                ID = context.User.Id,
                LastCommandUsed = DateTime.Now
            });
            col.EnsureIndex(x => x.ID);
            N = true;
        }
        var user = col.FindOne(x => x.ID == context.User.Id);
        var command = service.Search(context,0).Commands.FirstOrDefault().Command.Name;

        if (!user.Commands.Exists(x => x.Name == command)){
            user.Commands.Add(new Command(){
                Name = command,
                LastUsed = DateTime.Now
            });
        }
        if (N){
            return true;
        }
        var index = user.Commands.FindIndex(x => x.Name == command);
        var diff = user.Commands.ElementAt(index).LastUsed - DateTime.Now;
        if (diff >= Cooldown){
            user.Commands.ElementAt(index).LastUsed = DateTime.Now;
            user.LastCommandUsed = DateTime.Now;
            col.Update(user);
            return true;
        }
        else{
            var dms = await context.User.GetOrCreateDMChannelAsync();
            var left = Cooldown - diff;
            await dms.SendMessageAsync("You need to wait "+left.Minutes+" Minutes and "+left.Seconds+" Seconds to use this command again!");
            return false;
        }
    }
}