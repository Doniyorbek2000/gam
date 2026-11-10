using System;
using System.Threading.Tasks;
using BilliardGame.Meta;
using UnityEngine;

namespace BilliardGame.Online
{
    /// <summary>
    /// Missions, Season Pass, events and the daily shop (ROADMAP M3.2). Everything is read from the server's
    /// <c>liveops_state</c> RPC (Server/nakama/modules/src/liveops.ts) and every claim or purchase is decided there;
    /// afterwards both this state and the wallet are re-read (CLAUDE.md rule 3).
    /// </summary>
    public sealed class NakamaLiveOpsService
    {
        private readonly OnlineClient online;
        private readonly NakamaEconomyService economy;
        private float fetchedAt;

        public NakamaLiveOpsService(OnlineClient online, NakamaEconomyService economy)
        {
            this.online = online != null ? online : throw new ArgumentNullException(nameof(online));
            this.economy = economy ?? throw new ArgumentNullException(nameof(economy));
        }

        public event Action Changed;

        /// <summary>Last state from the server; null until the first successful refresh.</summary>
        public LiveOpsState State { get; private set; }

        /// <summary>Milliseconds since <see cref="State"/> arrived, for the countdowns.</summary>
        public long ElapsedMs => (long)((Time.realtimeSinceStartup - fetchedAt) * 1000f);

        public long Remaining(long msAtFetch) => LiveOpsRules.Remaining(msAtFetch, ElapsedMs);

        /// <summary>False if the server is unreachable (the old state, if any, stays).</summary>
        public async Task<bool> RefreshAsync()
        {
            try
            {
                LiveOpsState state = JsonUtility.FromJson<LiveOpsState>(await online.RpcAsync("liveops_state", "{}"));
                State = state ?? throw new InvalidOperationException("empty liveops state");
                fetchedAt = Time.realtimeSinceStartup;
                Changed?.Invoke();
                return true;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[LiveOps] Server state unavailable: {e.Message}");
                return false;
            }
        }

        public Task<string> ClaimMissionAsync(bool weekly, int index) =>
            Send("mission_claim", "{\"kind\":\"" + (weekly ? "weekly" : "daily") + "\",\"index\":" + index + "}", false);

        public Task<string> RerollMissionAsync(int index) => Send("mission_reroll", "{\"index\":" + index + "}", false);

        /// <summary>One reward (level ≥ 1) or, with level 0, every reward reached so far.</summary>
        public Task<string> ClaimPassAsync(int level, bool premium) =>
            Send("pass_claim", "{\"level\":" + level + ",\"track\":\"" + (premium ? "premium" : "free") + "\",\"request_id\":\"" + NewRequestId() + "\"}", true);

        public Task<string> BuyPassAsync() => Send("pass_buy", "{}", true);

        public Task<string> BuyOfferAsync(int slot) => Send("daily_shop_buy", "{\"slot\":" + slot + ",\"request_id\":\"" + NewRequestId() + "\"}", true);

        /// <summary>Sends a request; null on success, else a message for the player. Re-reads state (and the wallet).</summary>
        private async Task<string> Send(string rpc, string body, bool walletChanges)
        {
            string error = null;
            try
            {
                await online.RpcAsync(rpc, body);
            }
            catch (Exception e)
            {
                error = e is InvalidOperationException ? e.Message : "Connection problem, try again";
                Debug.LogWarning($"[LiveOps] {rpc} failed: {e.Message}");
            }

            // Mission rewards and pass items change the wallet and the inventory too.
            if (error == null && (walletChanges || rpc == "mission_claim")) await economy.RefreshAsync();
            await RefreshAsync();
            return error;
        }

        private static string NewRequestId() => Guid.NewGuid().ToString("N");
    }
}
