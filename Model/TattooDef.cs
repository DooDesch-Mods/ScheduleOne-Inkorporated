using UnityEngine;

namespace Inkorporated.Model
{
    /// <summary>Where a tattoo is applied. Determines the built-in tattoo that is cloned, and with it the mesh
    /// (face or body) the tattoo is drawn on and the shop category that lists it.</summary>
    public enum TattooPlacement
    {
        Chest,
        LeftArm,
        RightArm,
        Face
    }

    /// <summary>
    /// A single custom tattoo, resolved either from a pack manifest entry or the public API.
    /// Holds only metadata + a texture source until it is lazily registered with the game (see TattooRegistry).
    /// </summary>
    public sealed class TattooDef
    {
        /// <summary>Pack-scoped unique id (used in the registered avatar object id and the option GameObject name).</summary>
        public string Id;

        /// <summary>Display name shown on the shop button.</summary>
        public string DisplayName;

        public TattooPlacement Placement;

        /// <summary>Shop price; 0 shows "Free".</summary>
        public float Price;

        /// <summary>Absolute path to a PNG on disk (used when Texture is null).</summary>
        public string PngPath;

        /// <summary>Optional preloaded texture (supplied by API callers instead of a file).</summary>
        public Texture2D Texture;

        /// <summary>Optional embedded-resource source (the modder's assembly) - loaded lazily when first needed.</summary>
        public System.Reflection.Assembly ResourceAssembly;

        /// <summary>Embedded resource name of the PNG inside <see cref="ResourceAssembly"/> (exact, or a suffix).</summary>
        public string ResourceName;

        /// <summary>Originating pack name (or "API"); used for logging and a stable unique key.</summary>
        public string Source;

        /// <summary>
        /// The avatar object id this tattoo is registered under once realized, in the shape of a Resources path
        /// ("Avatar/Layers/Tattoos/custom/..."). Null until registered.
        /// </summary>
        public string ResourcePath;

        /// <summary>Stable global key: "&lt;Source&gt;/&lt;Id&gt;". Used for de-duplication.</summary>
        public string Key => (Source ?? "?") + "/" + (Id ?? "?");
    }
}
