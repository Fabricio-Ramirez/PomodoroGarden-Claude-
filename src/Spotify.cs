using System;

namespace PomodoroGarden
{
    // Works out what the music button opens. Only Spotify links are accepted, so the
    // settings file can't be used to launch anything else.
    static class Spotify
    {
        public const string Web = "https://open.spotify.com/";
        const string WebPrefix = "https://open.spotify.com/";

        static readonly string[] kinds = { "playlist", "album", "track", "artist", "show", "episode" };

        // Returns the Spotify app link (spotify:playlist:ID) for a saved link, or null if
        // it isn't a link to something on Spotify. Accepts both spotify: and open.spotify.com links.
        public static string ToAppLink(string link)
        {
            if (string.IsNullOrEmpty(link)) return null;
            link = link.Trim();
            string[] parts;
            if (link.StartsWith("spotify:", StringComparison.OrdinalIgnoreCase))
                parts = link.Substring(8).Split(':');
            else if (link.StartsWith(WebPrefix, StringComparison.OrdinalIgnoreCase))
            {
                string path = link.Substring(WebPrefix.Length);
                int cut = path.IndexOfAny(new[] { '?', '#' });
                if (cut >= 0) path = path.Substring(0, cut);
                parts = path.Trim('/').Split('/');
                if (parts.Length > 0 && parts[0].StartsWith("intl-", StringComparison.OrdinalIgnoreCase))
                    parts = Rest(parts); // localised links: open.spotify.com/intl-es/playlist/...
            }
            else return null;

            if (parts.Length != 2) return null;
            string kind = parts[0].ToLowerInvariant(), id = parts[1];
            if (Array.IndexOf(kinds, kind) < 0 || !IsId(id)) return null;
            return "spotify:" + kind + ":" + id;
        }

        // What to open: the app when it's installed, otherwise the web player in the browser.
        public static string Target(string link, bool appInstalled)
        {
            string app = ToAppLink(link);
            if (appInstalled) return app ?? "spotify:";
            return app == null ? Web : WebPrefix + app.Substring(8).Replace(':', '/');
        }

        static string[] Rest(string[] parts)
        {
            var rest = new string[parts.Length - 1];
            Array.Copy(parts, 1, rest, 0, rest.Length);
            return rest;
        }

        static bool IsId(string id)
        {
            if (id.Length == 0 || id.Length > 64) return false;
            foreach (char ch in id)
                if (!(ch >= 'a' && ch <= 'z' || ch >= 'A' && ch <= 'Z' || ch >= '0' && ch <= '9')) return false;
            return true;
        }
    }
}
