using System;
using System.Collections.Generic;
using System.Linq;
using RLBot.Models;
using RLSApi.Data;

namespace RLBot.Data
{
    public static class Global
    {
        private static Dictionary<RLPlaylist, List<RLRank>> PlaylistRanks = new Dictionary<RLPlaylist, List<RLRank>>
        {
            {
                RLPlaylist.Duel,
                new List<RLRank>
                {
                    new RLRank(407225064602468353, 0),
                    new RLRank(407225059963568139, 916),
                    new RLRank(407225057677410304, 1156),
                    new RLRank(407225054590664729, 1309),
                    new RLRank(407225051968962590, 1401)
                }
            },
            {
                RLPlaylist.Doubles,
                new List<RLRank>
                {
                    new RLRank(407224786956189696, 0),
                    new RLRank(407224673001275392, 936),
                    new RLRank(407224549801984001, 1196),
                    new RLRank(407224239079424031, 1396),
                    new RLRank(407224039363444736, 1551)
                }
            },
            {
                RLPlaylist.Standard,
                new List<RLRank>
                {
                    new RLRank(373857511146782721, 0),
                    new RLRank(375020227328475138, 936),
                    new RLRank(375035896753553409, 1196),
                    new RLRank(373857123110617099, 1396),
                    new RLRank(386211454040276992, 1551)
                }
            }
        };

        public static RLRank GetRank(RLPlaylist playlist, short elo)
        {
            PlaylistRanks.TryGetValue(playlist, out List<RLRank> ranks);
            if (ranks == null)
                throw new Exception("Invalid playlist");
            
            return ranks.Where(x => x.RequiredElo <= elo).LastOrDefault();
        }

        public static RLRank GetRank(RlsPlaylistRanked playlist, int elo)
        {
            switch (playlist)
            {
                case RlsPlaylistRanked.Duel:
                    return GetRank(RLPlaylist.Duel, (short)elo);
                case RlsPlaylistRanked.Doubles:
                    return GetRank(RLPlaylist.Doubles, (short)elo);
                case RlsPlaylistRanked.Standard:
                    return GetRank(RLPlaylist.Standard, (short)elo);    
            }
            throw new ArgumentException("Invalid playlist");
        }

        public static List<RLRank> GetRanks(RLPlaylist playlist)
        {
            PlaylistRanks.TryGetValue(playlist, out List<RLRank> ranks);
            return ranks;
        }
    }
}