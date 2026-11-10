using System;
using System.IO;
using BilliardGame.Content;
using BilliardGame.Meta;
using BilliardGame.Online;
using BilliardGame.Presentation;
using UnityEngine;

namespace BilliardGame.UI
{
    /// <summary>
    /// Owns the economy service and the skin catalog, and keeps every <see cref="SkinTarget"/> showing the equipped items.
    /// The economy is the server's (<see cref="NakamaEconomyService"/>); the local <see cref="MockEconomyService"/> is only
    /// for STORE_MOCK builds and the editor with "Server Economy" off (CLAUDE.md rule 3).
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public sealed class GameServices : MonoBehaviour
    {
        private const string SaveFile = "economy_mock.json";
        private const string ProfileFile = "profile_mock.json";

        [SerializeField] private SkinCatalog catalog;
        [SerializeField] private TextAsset crateTables;
        [SerializeField] private SkinTarget[] equipTargets = new SkinTarget[0];
        [SerializeField] private OnlineClient online;
        [Tooltip("Editor only: off = offline mock economy (local file). Player builds always use the server, except STORE_MOCK.")]
        [SerializeField] private bool serverEconomy = true;

        private IEconomyService economy;
        private NakamaEconomyService server;
        private NakamaLiveOpsService liveOps;
        private MockProfileService profile;

        public IEconomyService Economy => economy;
        /// <summary>Mock-only top-ups and prizes for testing without payments; null with the server economy.</summary>
        public MockEconomyService Mock => economy as MockEconomyService;
        public SkinCatalog Catalog => catalog;
        public MockProfileService Profile => profile;
        /// <summary>Missions, Season Pass, events and the daily shop (server only); null with the mock economy.</summary>
        public NakamaLiveOpsService LiveOps => liveOps;

        // Per local profile, so the editor and a dev bot on the same PC keep separate saves (LocalProfile).
        private string SavePath => Path.Combine(Application.persistentDataPath, LocalProfile.FileName(SaveFile));
        private string ProfilePath => Path.Combine(Application.persistentDataPath, LocalProfile.FileName(ProfileFile));

        private void Awake()
        {
            if (UseMock())
            {
                CrateTableSet tables = JsonUtility.FromJson<CrateTableSet>(crateTables.text);
                economy = new MockEconomyService(Load<EconomyState>(SavePath), catalog.ToCatalogItems(), tables.crates, s => Save(SavePath, s));
            }
            else
            {
                economy = server = new NakamaEconomyService(online, catalog.ToCatalogItems());
                liveOps = new NakamaLiveOpsService(online, server);
            }

            economy.Changed += ApplyEquipped;
            profile = new MockProfileService(Load<ProfileState>(ProfilePath), s => Save(ProfilePath, s));
        }

        /// <summary>Settings ▸ Account ▸ Delete: wipes the local profile, economy and preferences (Play data-deletion rule).</summary>
        public void DeleteLocalAccount()
        {
            foreach (string path in new[] { SavePath, ProfilePath })
            {
                try
                {
                    if (File.Exists(path)) File.Delete(path);
                }
                catch (IOException e)
                {
                    Debug.LogError($"[Account] Could not delete {path}: {e.Message}");
                }
            }

            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
        }

        private bool UseMock()
        {
#if STORE_MOCK
            return true;
#else
            if (online == null) throw new System.InvalidOperationException("GameServices needs the OnlineClient (rebuild the scene).");
            return Application.isEditor && !serverEconomy;
#endif
        }

        private void Start()
        {
            ApplyEquipped();
            _ = RefreshEconomy();
        }

        /// <summary>Re-reads balances, inventory and LiveOps from the server (no-op for the mock). False if unreachable.</summary>
        public async System.Threading.Tasks.Task<bool> RefreshEconomy()
        {
            if (server == null) return true;
            bool ok = await server.RefreshAsync();
            return await liveOps.RefreshAsync() && ok;
        }

        private void OnDestroy()
        {
            if (economy != null) economy.Changed -= ApplyEquipped;
        }

        public SkinDefinition Equipped(ItemType type) => catalog.Find(economy.EquippedId(type));

        private void ApplyEquipped()
        {
            SkinDefinition cue = Equipped(ItemType.Cue);
            SkinDefinition table = Equipped(ItemType.Table);
            SkinDefinition cueSticker = catalog.Find(economy.EquippedSticker(StickerSlot.Cue));
            SkinDefinition tableSticker = catalog.Find(economy.EquippedSticker(StickerSlot.Table));
            foreach (SkinTarget target in equipTargets)
            {
                target.Apply(cue);
                target.Apply(table);
                target.ApplyStickers(cueSticker, tableSticker);
            }
        }

        private static T Load<T>(string path) where T : class
        {
            try
            {
                return File.Exists(path) ? JsonUtility.FromJson<T>(File.ReadAllText(path)) : null;
            }
            catch (Exception e)
            {
                // A corrupt mock save must not block the game; start fresh and keep the bad file for inspection.
                Debug.LogWarning($"[Save] Could not read {path}, starting fresh: {e.Message}");
                File.Copy(path, path + ".bad", true);
                return null;
            }
        }

        private static void Save<T>(string path, T state)
        {
            try
            {
                File.WriteAllText(path, JsonUtility.ToJson(state));
            }
            catch (Exception e)
            {
                Debug.LogError($"[Save] Could not save {path}: {e.Message}");
            }
        }
    }
}
