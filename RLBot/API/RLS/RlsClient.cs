using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RLBot.API.RLS.Data;
using RLBot.API.RLS.Net;
using RLBot.API.RLS.Net.Models;
using RLBot.API.RLS.Net.Requests;

namespace RLBot.API.RLS
{
    public class RlsClient : IDisposable
    {
        private readonly ApiRequester _api;

        public RlsClient(string apiKey, bool throttle = true)
        {
            _api = throttle ? new ApiRequesterThrottle(apiKey) : new ApiRequester(apiKey);
        }

        /// <summary>
        ///     Retrieves platform data.
        /// </summary>
        /// <returns></returns>
        public async Task<Platform[]> GetPlatformsAsync()
        {
            return await _api.Get<Platform[]>("data/platforms");
        }
        
        /// <summary>
        ///     Retrieves season data.
        /// </summary>
        /// <returns></returns>
        public async Task<Season[]> GetSeasonsAsync()
        {
            return await _api.Get<Season[]>("data/seasons");
        }

        /// <summary>
        ///     Retrieves season data.
        /// </summary>
        /// <returns></returns>
        public async Task<Playlist[]> GetPlaylistsAsync()
        {
            return await _api.Get<Playlist[]>("data/playlists");
        }

        /// <summary>
        ///     Retrieves the latest tier data.
        /// </summary>
        /// <returns></returns>
        public async Task<Tier[]> GetTiersAsync()
        {
            return await _api.Get<Tier[]>("data/tiers");
        }

        /// <summary>
        ///     Retrieves the latest tier data.
        /// </summary>
        /// <param name="season">The <see cref="RlsSeason"/> you want tier data from.</param>
        /// <returns></returns>
        public async Task<Tier[]> GetTiersAsync(RlsSeason season)
        {
            return await _api.Get<Tier[]>($"data/tiers/{(int)season}");
        }

        /// <summary>
        ///     Retrieves player data.
        /// </summary>
        /// <param name="platform">The <see cref="RlsPlatform"/> of the player.</param>
        /// <param name="uniqueId">Steam 64 ID / PSN Username / Xbox Gamertag or XUID.</param>
        /// <returns></returns>
        public async Task<Player> GetPlayerAsync(RlsPlatform platform, string uniqueId)
        {
            return await _api.Get<Player>($"player?unique_id={Uri.EscapeDataString(uniqueId)}&platform_id={(int)platform}");
        }

        /// <summary>
        ///     Retrieves multiple players their data.
        ///     If a player is not found, it will be excluded from the response.
        /// </summary>
        /// <param name="players">The players you want to request. The maximum is 10.</param>
        /// <returns></returns>
        public async Task<Player[]> GetPlayersAsync(IEnumerable<PlayerBatchRequest> players)
        {
            if (players.Count() > 10)
            {
                throw new ArgumentException("You are trying to request too many players, the maximum is 10.");
            }

            return await _api.Post<Player[]>("player/batch", players);
        }

        /// <summary>
        ///     Searches for players.
        /// </summary>
        /// <param name="displayName">A part of the player his displayname.</param>
        /// <param name="page">The page number to receive.</param>
        /// <returns></returns>
        public async Task<PlayerSearchPage> SearchPlayerAsync(string displayName, int page = 0)
        {
            return await _api.Get<PlayerSearchPage>($"search/players?display_name={Uri.EscapeDataString(displayName)}&page={page}");
        }

        /// <summary>
        ///     Retrieves the top 100 ranked players of the current season.
        /// </summary>
        /// <param name="playlistRanked">The <see cref="RlsPlaylistRanked"/> you would like to receive.</param>
        /// <returns></returns>
        public async Task<Player[]> GetLeaderboardRankedAsync(RlsPlaylistRanked playlistRanked)
        {
            return await _api.Get<Player[]>($"leaderboard/ranked?playlist_id={(int)playlistRanked}");
        }

        /// <summary>
        ///     Retrieves the top 100 players based on the specified stat type.
        /// </summary>
        /// <param name="statType">The <see cref="RlsStatType"/> you would like to receive.</param>
        /// <returns></returns>
        public async Task<Player[]> GetLeaderboardStatAsync(RlsStatType statType)
        {
            return await _api.Get<Player[]>($"leaderboard/stat?type={statType.ToString().ToLower()}");
        }

        public void Dispose()
        {
            _api?.Dispose();
        }
    }
}
