using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BilliardGame.Meta;
using Nakama.TinyJson;
using UnityEngine;

namespace BilliardGame.Online
{
    /// <summary>
    /// Server economy (ROADMAP M3.1): balances, inventory, crates, shop and the login calendar all live on Nakama
    /// (Server/nakama/modules/src/economy.ts); this class only shows them and sends requests (CLAUDE.md rule 3).
    /// Every spending request carries a fresh request_id so a retry after a timeout is never charged twice.
    /// Crate tables and shop prices come from the server's config, so odds shown before purchase are the server's (rule 6).
    /// </summary>
    public sealed class NakamaEconomyService : IEconomyService
    {
        private readonly OnlineClient online;
        private readonly CatalogItem[] catalog;
        private readonly Dictionary<string, long> wallet = new Dictionary<string, long>();
        private readonly HashSet<string> owned = new HashSet<string>();
        private readonly Dictionary<string, string> equipped = new Dictionary<string, string>();
        private readonly Dictionary<string, int[]> pity = new Dictionary<string, int[]>();
        private CrateDefinition[] crates = Array.Empty<CrateDefinition>();
        private ShopPrices shop;

        public NakamaEconomyService(OnlineClient online, IReadOnlyList<CatalogItem> catalog)
        {
            this.online = online != null ? online : throw new ArgumentNullException(nameof(online));
            this.catalog = new CatalogItem[catalog.Count];
            for (int i = 0; i < this.catalog.Length; i++) this.catalog[i] = catalog[i];
        }

        public event Action Changed;

        /// <summary>True once the first state arrived from the server.</summary>
        public bool Ready { get; private set; }

        public int Coins => Amount("coins");
        public int Gems => Amount("gems");
        public int Shards => Amount("shards");
        public int Crystals => Amount("crystals");
        public IReadOnlyList<CrateDefinition> Crates => crates;
        public IReadOnlyList<CatalogItem> Catalog => catalog;
        public string PlayerId { get; private set; }
        public int LoginDay { get; private set; }
        public bool LoginClaimable { get; private set; }

        public bool Owns(string itemId) => itemId != null && owned.Contains(itemId);

        public string EquippedId(ItemType type) => Slot(type == ItemType.Sticker ? StickerKey(StickerSlot.Cue) : type.ToString());

        public string EquippedSticker(StickerSlot slot) => Slot(StickerKey(slot));

        public int PityRemaining(string crateId, PityRule rule)
        {
            CrateDefinition crate = Array.Find(crates, c => c.id == crateId);
            int index = crate?.pity == null ? -1 : Array.IndexOf(crate.pity, rule);
            int misses = index >= 0 && pity.TryGetValue(crateId, out int[] counters) && index < counters.Length ? counters[index] : 0;
            return rule.count - misses;
        }

        public int ShopPrice(string itemId, Currency currency)
        {
            int[] prices = shop == null ? null : currency == Currency.Gem ? shop.gem : shop.coin;
            int index = Array.FindIndex(catalog, i => i.Id == itemId);
            if (prices == null || index < 0) return 0;
            int rarity = (int)catalog[index].Rarity;
            return rarity < prices.Length ? prices[rarity] : 0;
        }

        /// <summary>Loads crate tables and shop prices (once) and the player's state. False if the server is unreachable.</summary>
        public async Task<bool> RefreshAsync()
        {
            try
            {
                if (crates.Length == 0) crates = JsonUtility.FromJson<CratesConfig>(await Config("crates")).value.crates ?? Array.Empty<CrateDefinition>();
                if (shop == null) shop = JsonUtility.FromJson<ShopConfig>(await Config("shop")).value;
                Apply(await online.RpcAsync("economy_state", "{}"));
                return true;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Economy] Server economy unavailable: {e.Message}");
                return false;
            }
        }

        public async Task<CrateOpenResult> OpenCrateAsync(string crateId, int count)
        {
            string body = "{\"crate_id\":" + Quote(crateId) + ",\"count\":" + count + ",\"request_id\":\"" + NewRequestId() + "\"}";
            try
            {
                var result = (await online.RpcAsync("crate_open", body)).FromJson<Dictionary<string, object>>();
                var drops = new List<CrateDrop>();
                foreach (object entry in (List<object>)result["drops"])
                {
                    var drop = (Dictionary<string, object>)entry;
                    int index = Array.FindIndex(catalog, i => i.Id == (string)drop["id"]);
                    if (index < 0) throw new InvalidOperationException("server dropped unknown item " + drop["id"]);
                    drops.Add(new CrateDrop(catalog[index], (bool)drop["isNew"], ToInt(drop["shards"]), ToInt(drop["crystals"])));
                }

                await RefreshAsync();
                return CrateOpenResult.Ok(drops);
            }
            catch (Exception e)
            {
                return CrateOpenResult.Fail(Friendly(e));
            }
        }

        public async Task<string> BuyAsync(string itemId, Currency currency)
        {
            string body = "{\"item_id\":" + Quote(itemId) + ",\"currency\":\"" + (currency == Currency.Gem ? "gem" : "coin") + "\",\"request_id\":\"" + NewRequestId() + "\"}";
            try
            {
                await online.RpcAsync("shop_buy", body);
                await RefreshAsync();
                return null;
            }
            catch (Exception e)
            {
                return Friendly(e);
            }
        }

        public async Task<IReadOnlyDictionary<string, int>> ClaimLoginAsync()
        {
            try
            {
                var result = (await online.RpcAsync("daily_claim", "{}")).FromJson<Dictionary<string, object>>();
                var reward = new Dictionary<string, int>();
                foreach (KeyValuePair<string, object> pair in (Dictionary<string, object>)result["reward"]) reward[pair.Key] = ToInt(pair.Value);
                await RefreshAsync();
                return reward;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Economy] Login claim failed: {Friendly(e)}");
                return null;
            }
        }

        public Task<bool> EquipAsync(string itemId) => Equip("{\"item_id\":" + Quote(itemId) + "}");

        public Task<bool> EquipStickerAsync(string itemId, StickerSlot slot) =>
            Equip("{\"item_id\":" + (itemId == null ? "null" : Quote(itemId)) + ",\"slot\":\"" + slot + "\"}");

        private async Task<bool> Equip(string body)
        {
            try
            {
                var result = (await online.RpcAsync("equip", body)).FromJson<Dictionary<string, object>>();
                ReadEquipped(result);
                Changed?.Invoke();
                return true;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Economy] Equip failed: {Friendly(e)}");
                return false;
            }
        }

        private async Task<string> Config(string name) => await online.RpcAsync("config_get", "{\"name\":\"" + name + "\"}");

        private void Apply(string json)
        {
            var state = json.FromJson<Dictionary<string, object>>();
            PlayerId = state["pid"] as string;
            wallet.Clear();
            foreach (KeyValuePair<string, object> pair in (Dictionary<string, object>)state["wallet"]) wallet[pair.Key] = Convert.ToInt64(pair.Value);
            owned.Clear();
            foreach (object id in (List<object>)state["owned"]) owned.Add((string)id);
            ReadEquipped(state);
            pity.Clear();
            foreach (KeyValuePair<string, object> pair in (Dictionary<string, object>)state["pity"])
            {
                var list = (List<object>)pair.Value;
                var counters = new int[list.Count];
                for (int i = 0; i < counters.Length; i++) counters[i] = ToInt(list[i]);
                pity[pair.Key] = counters;
            }

            var login = (Dictionary<string, object>)state["login"];
            LoginDay = ToInt(login["index"]);
            LoginClaimable = (bool)login["claimable"];
            Ready = true;
            Changed?.Invoke();
        }

        private void ReadEquipped(Dictionary<string, object> state)
        {
            equipped.Clear();
            foreach (KeyValuePair<string, object> pair in (Dictionary<string, object>)state["equipped"]) equipped[pair.Key] = (string)pair.Value;
        }

        private string Slot(string key) => equipped.TryGetValue(key, out string id) ? id : null;

        private int Amount(string currency) => wallet.TryGetValue(currency, out long value) ? (int)Math.Min(int.MaxValue, value) : 0;

        private static string StickerKey(StickerSlot slot) => "Sticker:" + slot;

        private static int ToInt(object value) => Convert.ToInt32(value);

        private static string NewRequestId() => Guid.NewGuid().ToString("N");

        // Ids come from the catalog (lowercase, digits, '_', '-'), so they need no JSON escaping; anything else is a bug.
        private static string Quote(string id) =>
            id != null && System.Text.RegularExpressions.Regex.IsMatch(id, "^[a-z0-9_-]{1,64}$") ? "\"" + id + "\"" : throw new ArgumentException("bad id " + id);

        private static string Friendly(Exception e) => e is InvalidOperationException ? e.Message : "Connection problem, try again";

        [Serializable]
        private sealed class CratesConfig
        {
            public CrateTableSet value;
        }

        [Serializable]
        private sealed class ShopConfig
        {
            public ShopPrices value;
        }

        [Serializable]
        private sealed class ShopPrices
        {
            public int[] coin;
            public int[] gem;
        }
    }
}
