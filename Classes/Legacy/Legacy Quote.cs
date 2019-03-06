using System;

namespace SAIL.Classes.Legacy
{
    public class Quote 
    {
        public int QuoteId { get; set; }
        public string Content { get; set; }
        public ulong Message { get; set; } //Message ID as per Discord
        public ulong User { get; set; } //User ID 
        public ulong Channel { get; set; } //Channel ID 
    }
}