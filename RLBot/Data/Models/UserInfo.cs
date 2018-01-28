using System;

namespace RLBot.Data.Models
{
    public class UserInfo
    {
        public ulong UserID { get; set; }
        public string UniqueID { get; set; }
        public DateTime JoinDate { get; set; }
        public short Elo1s { get; set; }
        public short Elo2s { get; set; }
        public short Elo3s { get; set; }
    }
}