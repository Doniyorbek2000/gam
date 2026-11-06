using System.IO;
using System.Text;
using BilliardGame.Content;
using BilliardGame.Meta;
using UnityEditor;
using UnityEngine;

namespace BilliardGame.Editor.Economy
{
    /// <summary>
    /// Writes the economy view of the skin catalog (id, type, rarity, default) to catalog.json next to crates.json.
    /// The server seeds its <c>config/catalog</c> from it (Server/nakama/modules/scripts/gen-seed.js), so crate pools
    /// and prices come from the same list, in the same order, as the client and the mock.
    /// </summary>
    internal static class CatalogExport
    {
        public const string OutputPath = "Assets/_Project/Resources/Economy/catalog.json";

        [MenuItem("BilliardGame/Economy/Export Catalog For Server")]
        public static void Export()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<SkinCatalog>(Content.ContentBuilder.CatalogPath);
            if (catalog == null) throw new FileNotFoundException("Skin catalog missing; run BilliardGame ▸ Build Content", Content.ContentBuilder.CatalogPath);

            var json = new StringBuilder("{\n  \"items\": [\n");
            CatalogItem[] items = catalog.ToCatalogItems();
            for (int i = 0; i < items.Length; i++)
            {
                CatalogItem item = items[i];
                json.Append($"    {{ \"id\": \"{item.Id}\", \"type\": \"{item.Type}\", \"rarity\": {(int)item.Rarity}, \"default\": {(item.OwnedByDefault ? "true" : "false")} }}");
                json.Append(i < items.Length - 1 ? ",\n" : "\n");
            }

            json.Append("  ]\n}\n");
            File.WriteAllText(OutputPath, json.ToString());
            AssetDatabase.ImportAsset(OutputPath);
            Debug.Log($"[Economy] Exported {items.Length} catalog items to {OutputPath}");
        }
    }
}
