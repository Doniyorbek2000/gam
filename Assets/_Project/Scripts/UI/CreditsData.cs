using System;

namespace BilliardGame.UI
{
    /// <summary>Resources/credits.json, generated from ArtSource/LICENSES.md by BilliardGame ▸ Art ▸ Generate Credits.</summary>
    [Serializable]
    public sealed class CreditsData
    {
        public const string ResourcePath = "credits";

        public CreditsEntry[] entries = new CreditsEntry[0];
    }

    [Serializable]
    public sealed class CreditsEntry
    {
        public string type;
        public string attribution;
        public string licenseUrl;
    }
}
