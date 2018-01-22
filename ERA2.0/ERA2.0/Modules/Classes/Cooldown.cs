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
        public DateTime LastCommandUsed {get;set;} = DateTime.Now;
        public List<Command> Commands {get;set;} = new List<Command>() {};

    }

    public class Command{
        public string Name {get;set;}
        public DateTime LastUsed {get;set;}
    }
    public async Task<bool> ValidateTimer(SocketCommandContext context, LiteDatabase database, TimeSpan Cooldown, CommandService service){
        var col = database.GetCollection<User>("User");
        if (!col.Exists(x => x.ID == context.User.Id)){
            col.Insert(new User(){
                ID = context.User.Id,
                LastCommandUsed = DateTime.Now
            });
            col.EnsureIndex(x => x.ID);
        }
        var user = col.FindOne(x => x.ID == context.User.Id);
        try {
            var searchResult = service.Search(context, context.Message.Content.Substring(1).Split(' ')[0]);
            var command = searchResult.Commands.FirstOrDefault();

            if (!user.Commands.Exists(x => x.Name == command.Command.Name)){
                user.Commands.Add(new Command(){
                    Name = command.Command.Name,
                    LastUsed = DateTime.Now
                });
                col.Update(user);
                return true;
            }
            var index = user.Commands.FindIndex(x => x.Name == command.Command.Name);
            var diff = DateTime.Now - user.Commands.ElementAt(index).LastUsed;
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
        catch{
            return true;
        }
        
    }
    public async Task<bool> GobalValidate(SocketCommandContext context, LiteDatabase database, TimeSpan Cooldown){
        var col = database.GetCollection<User>("User");
        if (!col.Exists(x => x.ID == context.User.Id)){
            col.Insert(new User(){
                ID = context.User.Id,
                LastCommandUsed = DateTime.Now
            });
            col.EnsureIndex(x => x.ID);
            return true;
        }
        var user = col.FindOne(x => x.ID == context.User.Id);
        var diff = DateTime.Now - user.LastCommandUsed;
        if (diff >= Cooldown){
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