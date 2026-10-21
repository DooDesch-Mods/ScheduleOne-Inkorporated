// Copyright (c) 2026 DooDesch. DooDesch API License (see LICENSE.md): you may copy this file unchanged.
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
    /// <remarks>
    /// A tattoo is a full UV-space skin texture, not a sticker. The body shares one UV unwrap (torso, both arms,
    /// legs) and the face has its own. Opaque pixels must sit at the UV coordinates of the body part; the rest stays
    /// transparent. The built-in tattoos use 2048x2048 for body and arms and 512x512 for the face. Size only changes
    /// sharpness, not placement.
    ///
    /// Each tattoo is keyed by <c>source/id</c>, compared case-insensitively. It is registered under the avatar
    /// object id <c>Avatar/Layers/Tattoos/custom/&lt;placement&gt;/&lt;source&gt;_&lt;id&gt;</c>, where placement is
    /// <c>chest</c>, <c>leftarm</c>, <c>rightarm</c> or <c>Face</c>. Characters other than letters, digits, <c>-</c>
    /// and <c>_</c> in source and id become <c>_</c>. Example: <c>Avatar/Layers/Tattoos/custom/Face/InkExample_ring_face</c>.
    /// </remarks>
    /// <example>
    /// Register a face tattoo from a PNG embedded in your own DLL, in <c>OnInitializeMelon</c>:
    /// <code>
    /// using Inkorporated;
    /// using Inkorporated.Model;
    ///
    /// bool added = API.RegisterTattooFromResource(
    ///     id: "ring_face",
    ///     displayName: "Example Ring",
    ///     placement: TattooPlacement.Face,
    ///     resourceName: "InkExample.Assets.ring_face.png",
    ///     price: 0f,
    ///     source: "InkExample");
    /// </code>
    /// </example>
    public static class API
    {
        /// <summary>Register a tattoo from an already-loaded texture.</summary>
        /// <param name="id">Unique id within your <paramref name="source"/>, e.g. "skull". Null or blank registers nothing and returns false.</param>
        /// <param name="displayName">Name shown on the shop button, e.g. "Skull". Null or blank shows the id.</param>
        /// <param name="placement">Body region: <c>Chest</c>, <c>LeftArm</c>, <c>RightArm</c> or <c>Face</c>. Also picks the shop category that lists it.</param>
        /// <param name="texture">The tattoo texture. It must match the game's body or face UV layout; see the remarks on <see cref="API"/>.</param>
        /// <param name="price">Shop price in in-game dollars, shown rounded to whole dollars, e.g. 250 shows "$250". 0 shows "Free". A negative value counts as 0.</param>
        /// <param name="source">A stable namespace for your mod, e.g. "MyMod". Used for de-duplication and the registered avatar object id. Null or blank means "API".</param>
        /// <returns>True if added. False if <paramref name="id"/> is blank or the same (source, id) is already registered.</returns>
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
        /// <param name="id">Unique id within your <paramref name="source"/>, e.g. "skull". Null or blank registers nothing and returns false.</param>
        /// <param name="displayName">Name shown on the shop button, e.g. "Skull". Null or blank shows the id.</param>
        /// <param name="placement">Body region: <c>Chest</c>, <c>LeftArm</c>, <c>RightArm</c> or <c>Face</c>. Also picks the shop category that lists it.</param>
        /// <param name="pngPath">Absolute path to a PNG file, e.g. <c>@"C:\path\to.png"</c>. A missing or unreadable file logs a warning and the tattoo does not show.</param>
        /// <param name="price">Shop price in in-game dollars, shown rounded to whole dollars, e.g. 250 shows "$250". 0 shows "Free". A negative value counts as 0.</param>
        /// <param name="source">A stable namespace for your mod, e.g. "MyMod". Used for de-duplication and the registered avatar object id. Null or blank means "API".</param>
        /// <returns>True if added. False if <paramref name="id"/> is blank or the same (source, id) is already registered. The file is only read later, so a bad path still returns true.</returns>
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
        /// <param name="id">Unique id within your <paramref name="source"/>, e.g. "skull". Null or blank registers nothing and returns false.</param>
        /// <param name="displayName">Name shown on the shop button, e.g. "Skull". Null or blank shows the id.</param>
        /// <param name="placement">Body region: <c>Chest</c>, <c>LeftArm</c>, <c>RightArm</c> or <c>Face</c>. Also picks the shop category that lists it.</param>
        /// <param name="resourceName">Embedded resource name of the PNG, e.g. "MyMod.Assets.skull.png". If no resource has that exact name, the first one ending in "." plus this name is used, so "skull.png" works too.</param>
        /// <param name="price">Shop price in in-game dollars, shown rounded to whole dollars, e.g. 250 shows "$250". 0 shows "Free". A negative value counts as 0.</param>
        /// <param name="source">A stable namespace for your mod, e.g. "MyMod". Used for de-duplication and the registered avatar object id. Null or blank means "API".</param>
        /// <returns>True if added. False if <paramref name="id"/> is blank or the same (source, id) is already registered. The resource is only read later, so a wrong name still returns true.</returns>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        public static bool RegisterTattooFromResource(string id, string displayName, TattooPlacement placement, string resourceName, float price = 0f, string source = "API")
        {
            return RegisterTattooFromResource(id, displayName, placement, System.Reflection.Assembly.GetCallingAssembly(), resourceName, price, source);
        }

        /// <summary>
        /// Register a tattoo from a PNG embedded in the given <paramref name="assembly"/> (use this overload if
        /// the PNG lives in a different assembly than the caller). Loaded lazily when first needed.
        /// </summary>
        /// <param name="id">Unique id within your <paramref name="source"/>, e.g. "skull". Null or blank registers nothing and returns false.</param>
        /// <param name="displayName">Name shown on the shop button, e.g. "Skull". Null or blank shows the id.</param>
        /// <param name="placement">Body region: <c>Chest</c>, <c>LeftArm</c>, <c>RightArm</c> or <c>Face</c>. Also picks the shop category that lists it.</param>
        /// <param name="assembly">The assembly that embeds the PNG, e.g. <c>typeof(MyAssetsType).Assembly</c>.</param>
        /// <param name="resourceName">Embedded resource name of the PNG, e.g. "MyMod.Assets.skull.png". If no resource has that exact name, the first one ending in "." plus this name is used, so "skull.png" works too.</param>
        /// <param name="price">Shop price in in-game dollars, shown rounded to whole dollars, e.g. 250 shows "$250". 0 shows "Free". A negative value counts as 0.</param>
        /// <param name="source">A stable namespace for your mod, e.g. "MyMod". Used for de-duplication and the registered avatar object id. Null or blank means "API".</param>
        /// <returns>True if added. False if <paramref name="id"/> is blank or the same (source, id) is already registered. The resource is only read later, so a wrong name still returns true.</returns>
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
