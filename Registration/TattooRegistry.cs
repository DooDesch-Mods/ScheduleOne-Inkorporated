using System;
using System.Collections.Generic;
using System.Text;
using Inkorporated.Model;
using S1API.Rendering;
using UnityEngine;

namespace Inkorporated.Registration
{
    /// <summary>
    /// Master list of custom tattoos and their lazy registration with the game.
    /// Registration clones a built-in <c>AvatarLayer</c> of the same placement, swaps in the custom texture, and
    /// registers it at a custom Resources path via S1API (which patches Resources.Load). Lazy = done the first
    /// time the tattoo is needed (when the shop opens), guaranteeing the avatar resources are loaded.
    /// </summary>
    internal static class TattooRegistry
    {
        private static readonly List<TattooDef> _all = new List<TattooDef>();
        private static readonly HashSet<string> _keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public static IReadOnlyList<TattooDef> AllDefs => _all;

        /// <summary>Registers a definition (de-duplicated by Source/Id). Returns false if it was a duplicate.</summary>
        public static bool Add(TattooDef def)
        {
            if (def == null || string.IsNullOrWhiteSpace(def.Id)) return false;
            if (!_keys.Add(def.Key)) return false;
            _all.Add(def);
            return true;
        }

        public static int AddRange(IEnumerable<TattooDef> defs)
        {
            int n = 0;
            if (defs == null) return 0;
            foreach (TattooDef d in defs) if (Add(d)) n++;
            return n;
        }

        /// <summary>The path substring used to recognise which placement a built-in shop category hosts.</summary>
        public static string CategoryToken(TattooPlacement p) => p switch
        {
            TattooPlacement.Chest => "/chest/",
            TattooPlacement.LeftArm => "/leftarm/",
            TattooPlacement.RightArm => "/rightarm/",
            TattooPlacement.Face => "/face/",
            _ => "/chest/"
        };

        // A built-in layer of each placement to clone (inherits its CombinedMaterial / Order / UV expectations).
        private static string SourceLayer(TattooPlacement p) => p switch
        {
            TattooPlacement.Chest => "Avatar/Layers/Tattoos/chest/Chest_Bird",
            TattooPlacement.LeftArm => "Avatar/Layers/Tattoos/leftarm/LeftArm_Web",
            TattooPlacement.RightArm => "Avatar/Layers/Tattoos/rightarm/RightArm_Web",
            TattooPlacement.Face => "Avatar/Layers/Tattoos/face/Face_Teardrop",
            _ => "Avatar/Layers/Tattoos/chest/Chest_Bird"
        };

        // Custom target path. Face uses a capital "/Face/" segment so BasicAvatarSettings.GetAvatarSettings()
        // routes it to the face mesh (it checks tattooPath.Contains("/Face/")). Body placements must NOT contain it.
        private static string TargetPath(TattooDef def)
        {
            string seg = def.Placement == TattooPlacement.Face ? "Face" : def.Placement.ToString().ToLowerInvariant();
            return "Avatar/Layers/Tattoos/custom/" + seg + "/" + Sanitize(def.Source) + "_" + Sanitize(def.Id);
        }

        /// <summary>
        /// Ensures the tattoo's avatar layer exists in the registry so Resources.Load resolves it.
        /// Idempotent. Returns true if the tattoo is ready to be applied/shown.
        /// </summary>
        public static bool EnsureRegistered(TattooDef def)
        {
            if (def == null) return false;
            if (def.ResourcePath != null) return true; // already realized

            try
            {
                Texture2D tex = def.Texture;
                if (tex == null)
                {
                    if (string.IsNullOrEmpty(def.PngPath))
                    {
                        Core.Log?.Warning($"Tattoo '{def.Key}': no texture and no PNG path.");
                        return false;
                    }
                    tex = TextureUtils.LoadTextureFromFile(def.PngPath);
                    if (tex == null)
                    {
                        Core.Log?.Warning($"Tattoo '{def.Key}': failed to load PNG '{def.PngPath}'.");
                        return false;
                    }
                }

                string target = TargetPath(def);
                string source = SourceLayer(def.Placement);

                bool ok = AvatarLayerFactory.CreateAndRegisterAvatarLayer(source, target, def.DisplayName ?? def.Id, tex);
                if (!ok)
                {
                    Core.Log?.Warning($"Tattoo '{def.Key}': CreateAndRegisterAvatarLayer failed (source '{source}').");
                    return false;
                }

                def.ResourcePath = target;
                Core.Log?.Msg($"Registered tattoo '{def.Key}' -> {target}");
                return true;
            }
            catch (Exception ex)
            {
                Core.Log?.Warning($"Tattoo '{def.Key}': registration error - {ex.Message}");
                return false;
            }
        }

        private static string Sanitize(string s)
        {
            if (string.IsNullOrEmpty(s)) return "x";
            var sb = new StringBuilder(s.Length);
            foreach (char c in s)
                sb.Append((char.IsLetterOrDigit(c) || c == '-' || c == '_') ? c : '_');
            return sb.ToString();
        }
    }
}
