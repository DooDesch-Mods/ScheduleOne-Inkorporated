using Inkorporated.Model;
using Inkorporated.Registration;
using UnityEngine;

namespace Inkorporated
{
    /// <summary>
    /// Public, stable entry point for other mods that want to add custom tattoos to the in-game tattoo shop.
    /// Register early (e.g. in your mod's OnInitializeMelon). Tattoos registered after the tattoo-shop UI has
    /// already been built will appear the next time that UI is rebuilt.
    /// </summary>
    public static class API
    {
        /// <summary>Register a tattoo from an already-loaded texture.</summary>
        /// <param name="id">Unique id within your <paramref name="source"/>.</param>
        /// <param name="displayName">Name shown on the shop button.</param>
        /// <param name="placement">Body region the tattoo applies to.</param>
        /// <param name="texture">The tattoo texture (must match the game's body/face UV layout).</param>
        /// <param name="price">Shop price; 0 shows "Free".</param>
        /// <param name="source">A stable namespace for your mod (defaults to "API"); used for de-duplication and paths.</param>
        /// <returns>True if added; false if it duplicated an existing (source, id).</returns>
        public static bool RegisterTattoo(string id, string displayName, TattooPlacement placement, Texture2D texture, float price = 0f, string source = "API")
        {
            return TattooRegistry.Add(new TattooDef
            {
                Id = id,
                DisplayName = string.IsNullOrWhiteSpace(displayName) ? id : displayName,
                Placement = placement,
                Price = price < 0f ? 0f : price,
                Texture = texture,
                Source = string.IsNullOrWhiteSpace(source) ? "API" : source
            });
        }

        /// <summary>Register a tattoo from a PNG file on disk (loaded lazily when first needed).</summary>
        public static bool RegisterTattooFromFile(string id, string displayName, TattooPlacement placement, string pngPath, float price = 0f, string source = "API")
        {
            return TattooRegistry.Add(new TattooDef
            {
                Id = id,
                DisplayName = string.IsNullOrWhiteSpace(displayName) ? id : displayName,
                Placement = placement,
                Price = price < 0f ? 0f : price,
                PngPath = pngPath,
                Source = string.IsNullOrWhiteSpace(source) ? "API" : source
            });
        }

        /// <summary>
        /// Register a tattoo from a PNG embedded in YOUR mod's DLL (the calling assembly), so you don't have to
        /// write the resource-loading boilerplate. <paramref name="resourceName"/> is the embedded resource name
        /// (e.g. "MyMod.Assets.skull.png"); a trailing-suffix match is accepted, so "skull.png" works too.
        /// Loaded lazily when first needed.
        /// </summary>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        public static bool RegisterTattooFromResource(string id, string displayName, TattooPlacement placement, string resourceName, float price = 0f, string source = "API")
        {
            return RegisterTattooFromResource(id, displayName, placement, System.Reflection.Assembly.GetCallingAssembly(), resourceName, price, source);
        }

        /// <summary>
        /// Register a tattoo from a PNG embedded in the given <paramref name="assembly"/> (use this overload if
        /// the PNG lives in a different assembly than the caller). Loaded lazily when first needed.
        /// </summary>
        public static bool RegisterTattooFromResource(string id, string displayName, TattooPlacement placement, System.Reflection.Assembly assembly, string resourceName, float price = 0f, string source = "API")
        {
            return TattooRegistry.Add(new TattooDef
            {
                Id = id,
                DisplayName = string.IsNullOrWhiteSpace(displayName) ? id : displayName,
                Placement = placement,
                Price = price < 0f ? 0f : price,
                ResourceAssembly = assembly,
                ResourceName = resourceName,
                Source = string.IsNullOrWhiteSpace(source) ? "API" : source
            });
        }
    }
}
