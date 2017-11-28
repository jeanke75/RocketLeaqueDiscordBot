using System;
using System.Collections.Generic;
using Discord.WebSocket;

namespace RLBot.Models
{
    public class RLQueue
    {
        public DateTime created;
        public SocketGuildChannel channel;
        public List<SocketUser> users = new List<SocketUser>();
        public bool isOpen = false;
    }
}
