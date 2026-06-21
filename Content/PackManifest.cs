using System.Collections.Generic;

namespace Inkorporated.Content
{
    /// <summary>
    /// Deserialization shape for a pack's <c>manifest.json</c> (parsed with managed Newtonsoft.Json).
    /// Field names are intentionally lowercase to match the JSON authors write.
    /// </summary>
    public sealed class PackManifest
    {
        public string name;
        public string author;
        public List<ManifestEntry> tattoos;
    }

    /// <summary>One tattoo entry inside a pack manifest.</summary>
    public sealed class ManifestEntry
    {
        /// <summary>Unique id within the pack.</summary>
        public string id;

        /// <summary>Display name on the shop button (falls back to id).</summary>
        public string name;

        /// <summary>chest | leftarm | rightarm | face (case-insensitive).</summary>
        public string placement;

        /// <summary>PNG filename relative to the pack folder.</summary>
        public string file;

        /// <summary>Shop price; omit or 0 for "Free".</summary>
        public float price;
    }
}
