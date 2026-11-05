using System;
using System.IO;
using BilliardGame.Content;
using BilliardGame.Meta;
using UnityEngine;

namespace BilliardGame.UI
{
    /// <summary>
    /// Owns the economy service and the skin catalog, and keeps every <see cref="SkinTarget"/> showing the equipped items.
    /// M1 uses <see cref="MockEconomyService"/> saved to persistentDataPath; M2 swaps in the Nakama implementation.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public sealed class GameServices : MonoBehaviour
    {
        private const string SaveFile = "economy_mock.json";

        [SerializeField] private SkinCatalog catalog;
        [SerializeField] private TextAsset crateTables;
        [SerializeField] private SkinTarget[] equipTargets = new SkinTarget[0];

        private MockEconomyService mock;

        public IEconomyService Economy => mock;
        /// <summary>Mock-only top-ups for testing without payments (STORE_MOCK / editor).</summary>
        public MockEconomyService Mock => mock;
        public SkinCatalog Catalog => catalog;

        private string SavePath => Path.Combine(Application.persistentDataPath, SaveFile);

        private void Awake()
        {
            CrateTableSet tables = JsonUtility.FromJson<CrateTableSet>(crateTables.text);
            mock = new MockEconomyService(LoadState(), catalog.ToCatalogItems(), tables.crates, SaveState);
            mock.Changed += ApplyEquipped;
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
            foreach (SkinTarget target in equipTargets)
            {
                target.Apply(cue);
                target.Apply(table);
            }
        }

        private EconomyState LoadState()
        {
            try
            {
                return File.Exists(SavePath) ? JsonUtility.FromJson<EconomyState>(File.ReadAllText(SavePath)) : null;
            }
            catch (Exception e)
            {
                // A corrupt mock save must not block the game; start a fresh profile and keep the bad file for inspection.
                Debug.LogWarning($"[Economy] Could not read {SavePath}, starting fresh: {e.Message}");
                File.Copy(SavePath, SavePath + ".bad", true);
                return null;
            }
        }

        private void SaveState(EconomyState state)
        {
            try
            {
                File.WriteAllText(SavePath, JsonUtility.ToJson(state));
            }
            catch (Exception e)
            {
                Debug.LogError($"[Economy] Could not save {SavePath}: {e.Message}");
            }
        }
    }
}
