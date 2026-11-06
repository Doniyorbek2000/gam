using System;
using System.IO;
using BilliardGame.Content;
using BilliardGame.Meta;
using UnityEngine;

namespace BilliardGame.UI
{
    /// <summary>
    /// Owns the economy service and the skin catalog, and keeps every <see cref="SkinTarget"/> showing the equipped items.
    /// M1 uses <see cref="MockEconomyService"/> saved to persistentDataPath; M3 swaps in the Nakama implementation.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public sealed class GameServices : MonoBehaviour
    {
        private const string SaveFile = "economy_mock.json";
        private const string ProfileFile = "profile_mock.json";

        [SerializeField] private SkinCatalog catalog;
        [SerializeField] private TextAsset crateTables;
        [SerializeField] private SkinTarget[] equipTargets = new SkinTarget[0];

        private MockEconomyService mock;
        private MockProfileService profile;

        public IEconomyService Economy => mock;
        /// <summary>Mock-only top-ups for testing without payments (STORE_MOCK / editor).</summary>
        public MockEconomyService Mock => mock;
        public SkinCatalog Catalog => catalog;
        public MockProfileService Profile => profile;

        private string SavePath => Path.Combine(Application.persistentDataPath, SaveFile);
        private string ProfilePath => Path.Combine(Application.persistentDataPath, ProfileFile);

        private void Awake()
        {
            CrateTableSet tables = JsonUtility.FromJson<CrateTableSet>(crateTables.text);
            mock = new MockEconomyService(Load<EconomyState>(SavePath), catalog.ToCatalogItems(), tables.crates, s => Save(SavePath, s));
            mock.Changed += ApplyEquipped;
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

        private void Start() => ApplyEquipped();

        private void OnDestroy()
        {
            if (mock != null) mock.Changed -= ApplyEquipped;
        }

        public SkinDefinition Equipped(ItemType type) => catalog.Find(mock.EquippedId(type));

        private void ApplyEquipped()
        {
            SkinDefinition cue = Equipped(ItemType.Cue);
            SkinDefinition table = Equipped(ItemType.Table);
            SkinDefinition cueSticker = catalog.Find(mock.EquippedSticker(StickerSlot.Cue));
            SkinDefinition tableSticker = catalog.Find(mock.EquippedSticker(StickerSlot.Table));
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
