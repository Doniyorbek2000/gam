using System.Collections.Generic;
using System.IO;
using System.Linq;
using BilliardGame.UI;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace BilliardGame.Editor.Art
{
    /// <summary>Generate Credits / Validate All Art, and the same two steps before every player build.</summary>
    internal sealed class CreditsGenerator : IPreprocessBuildWithReport
    {
        public const string CreditsPath = "Assets/_Project/Resources/credits.json";

        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            List<string> problems = Validate();
            if (problems.Count > 0) throw new BuildFailedException("Art license check failed:\n" + string.Join("\n", problems));
            Generate();
        }

        [MenuItem("BilliardGame/Art/Generate Credits")]
        public static void Generate()
        {
            var data = new CreditsData
            {
                entries = LicenseRegistry.Load().Values
                    .Where(e => LicenseRegistry.Reject(e) == null)
                    .OrderBy(e => e.Type).ThenBy(e => e.Slug)
                    .Select(e => new CreditsEntry { type = e.Type, attribution = e["attribution"], licenseUrl = e["license_url"] })
                    .ToArray(),
            };
            BallMaterialFactory.EnsureFolder(Path.GetDirectoryName(CreditsPath).Replace('\\', '/'));
            File.WriteAllText(CreditsPath, JsonUtility.ToJson(data, true));
            AssetDatabase.ImportAsset(CreditsPath);
            Debug.Log($"[Art] Credits: {data.entries.Length} entries → {CreditsPath}");
        }

        [MenuItem("BilliardGame/Art/Validate All Art")]
        public static void ValidateMenu()
        {
            List<string> problems = Validate();
            if (problems.Count == 0) Debug.Log("[Art] Validate All Art: every imported asset folder has an allowed license entry.");
            foreach (string problem in problems) Debug.LogError("[Art] " + problem);
        }

        /// <summary>Every Art/&lt;TypeFolder&gt;/&lt;slug&gt;/ folder needs an allowed LICENSES.md block of the same type.</summary>
        public static List<string> Validate()
        {
            Dictionary<string, LicenseEntry> registry = LicenseRegistry.Load();
            var problems = new List<string>();
            foreach (KeyValuePair<string, string> type in LicenseRegistry.TypeFolders)
            {
                string root = $"{ArtImporter.ArtRoot}/{type.Value}";
                if (!Directory.Exists(root)) continue;
                foreach (string dir in Directory.GetDirectories(root))
                {
                    string slug = Path.GetFileName(dir);
                    registry.TryGetValue(slug, out LicenseEntry entry);
                    string rejected = LicenseRegistry.Reject(entry);
                    if (rejected != null) problems.Add($"{root}/{slug}: {rejected}");
                    else if (!string.Equals(entry.Type, type.Key, System.StringComparison.OrdinalIgnoreCase)) problems.Add($"{root}/{slug}: registry type is '{entry.Type}'");
                }
            }

            return problems;
        }
    }
}
