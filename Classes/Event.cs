using System;
using LiteDB;

namespace SAIL.Classes
{
    public class GuildEvent
    {
        [BsonId]
        public int id {get;set;}
        [BsonRef("Guilds")]
        public SysGuild Server {get;set;}
        public DateTime Date {get;set;}
        public string Name {get;set;}
        public string Description {get;set;}
        public RepeatingState Repeating {get;set;} = RepeatingState.Once;
    }
}