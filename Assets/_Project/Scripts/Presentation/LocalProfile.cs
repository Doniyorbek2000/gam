using System;

namespace BilliardGame.Presentation
{
    /// <summary>
    /// Which local save and online account this process uses, so the editor and dev players on one PC do not share
    /// one: <c>-profile name</c> picks one explicitly, a <c>-autopilot</c> bot gets its own ("bot"), everything else
    /// (the editor, phones) keeps the default save. The editor's online account is told apart separately.
    /// </summary>
    public static class LocalProfile
    {
        public static string Suffix { get; } = Resolve(Environment.GetCommandLineArgs());

        /// <summary>"-name" to append to save file names and device ids, or "" for the default profile.</summary>
        public static string Resolve(string[] args)
        {
            int at = Array.IndexOf(args, "-profile");
            if (at >= 0 && at + 1 < args.Length)
            {
                string name = Clean(args[at + 1]);
                if (name.Length > 0) return "-" + name;
            }

            return Array.IndexOf(args, "-autopilot") >= 0 ? "-bot" : string.Empty;
        }

        /// <summary>File-name-safe: lowercase letters, digits, '_' only, at most 24 characters.</summary>
        private static string Clean(string raw)
        {
            var chars = new System.Text.StringBuilder();
            foreach (char c in raw.ToLowerInvariant())
            {
                if ((c >= 'a' && c <= 'z') || (c >= '0' && c <= '9') || c == '_') chars.Append(c);
                if (chars.Length == 24) break;
            }

            return chars.ToString();
        }

        /// <summary><paramref name="fileName"/> with the profile suffix before its extension ("economy_mock-bot.json").</summary>
        public static string FileName(string fileName)
        {
            if (Suffix.Length == 0) return fileName;
            int dot = fileName.LastIndexOf('.');
            return dot < 0 ? fileName + Suffix : fileName.Substring(0, dot) + Suffix + fileName.Substring(dot);
        }
    }
}
