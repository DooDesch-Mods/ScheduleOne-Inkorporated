using System;
using System.Collections.Generic;
using System.Text;
using DooDesch.AvatarKit;
using Inkorporated.Model;
using S1API.Rendering;
using UnityEngine;

namespace Inkorporated.Registration
{
    /// <summary>
    /// Master list of custom tattoos and their registration with the game.
    /// Registration clones the avatar object of a built-in tattoo of the same placement, swaps in the custom
    /// texture and enters the clone in the game's AvatarObjectLibrary under the tattoo's own id
    /// (<see cref="AvatarLook.RegisterCustomLayer"/>). The game resolves every applied tattoo by that id - shop
    /// preview, the player's saved appearance and other players' appearances alike - so all tattoos are
    /// registered as soon as the library is loaded (<see cref="RegisterAll"/>), before any save applies one.
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
            // Added after the first scene load (a mod registering late): realise it now rather than at the next
            // shop visit, so an appearance that already carries it resolves.
            if (_registeredAll) EnsureRegistered(def);
            return true;
        }

        private static bool _registeredAll;

        /// <summary>
        /// Registers every known tattoo with the avatar object library. Called on the first scene load: the
        /// library fills itself before the first scene, and a save applies its appearance only later.
        /// </summary>
        public static void RegisterAll()
        {
            if (_registeredAll) return;
            if (!AvatarLook.LibraryReady)
            {
                Core.Log?.Warning("Avatar object library not loaded yet - tattoo registration waits for the next scene load.");
                return;
            }
            _registeredAll = true;
            int ok = 0;
            foreach (TattooDef def in _all)
                if (EnsureRegistered(def)) ok++;
            Core.Log?.Msg($"Registered {ok} of {_all.Count} custom tattoo(s) with the avatar object library.");
        }

        /// <summary>
        /// Id of the built-in tattoo that <paramref name="p"/> is cloned from, or null when the game no longer has
        /// it. The shop category that lists this tattoo is the one that hosts the placement.
        /// </summary>
        public static string SourceObjectId(TattooPlacement p) =>
            AvatarLook.TryResolve(SourceLayer(p), out var obj) && obj != null ? obj.Id : null;

        public static int AddRange(IEnumerable<TattooDef> defs)
        {
            int n = 0;
            if (defs == null) return 0;
            foreach (TattooDef d in defs) if (Add(d)) n++;
            return n;
        }

        // A built-in tattoo of each placement to clone (inherits its layer type, order and UV expectations). A legacy
        // layer path; AvatarLook follows it to the 0.4.7 avatar object.
        private static string SourceLayer(TattooPlacement p) => p switch
        {
            TattooPlacement.Chest => "Avatar/Layers/Tattoos/chest/Chest_Bird",
            TattooPlacement.LeftArm => "Avatar/Layers/Tattoos/leftarm/LeftArm_Web",
            TattooPlacement.RightArm => "Avatar/Layers/Tattoos/rightarm/RightArm_Web",
            TattooPlacement.Face => "Avatar/Layers/Tattoos/face/Face_Teardrop",
            _ => "Avatar/Layers/Tattoos/chest/Chest_Bird"
        };

        // The avatar object id. Kept in the shape of the former Resources path, so ids stay stable across versions;
        // face vs body now follows from the cloned tattoo, not from the "/Face/" segment.
        private static string TargetPath(TattooDef def)
        {
            string seg = def.Placement == TattooPlacement.Face ? "Face" : def.Placement.ToString().ToLowerInvariant();
            return "Avatar/Layers/Tattoos/custom/" + seg + "/" + Sanitize(def.Source) + "_" + Sanitize(def.Id);
        }

        /// <summary>
        /// Ensures the tattoo's avatar object exists in the game's library so an appearance carrying its id resolves.
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
                    if (!string.IsNullOrEmpty(def.PngPath))
                    {
                        tex = TextureUtils.LoadTextureFromFile(def.PngPath);
                    }
                    else if (def.ResourceAssembly != null && !string.IsNullOrEmpty(def.ResourceName))
                    {
                        byte[] bytes = ReadResource(def.ResourceAssembly, def.ResourceName);
                        if (bytes != null) tex = TextureUtils.LoadTextureFromBytes(bytes);
                    }
                    else
                    {
                        Core.Log?.Warning($"Tattoo '{def.Key}': no texture, PNG path or embedded resource.");
                        return false;
                    }

                    if (tex == null)
                    {
                        Core.Log?.Warning($"Tattoo '{def.Key}': failed to load image.");
                        return false;
                    }
                }

                string target = TargetPath(def);
                string source = SourceLayer(def.Placement);

                bool ok = AvatarLook.RegisterCustomLayer(source, target, def.DisplayName ?? def.Id, tex);
                if (!ok)
                {
                    Core.Log?.Error($"Tattoo '{def.Key}': registering the avatar object failed (source '{source}').");
                    return false;
                }

                def.ResourcePath = target;
                Core.Log?.Msg($"Registered tattoo '{def.Key}' -> {target}");
                return true;
            }
            catch (Exception ex)
            {
                Core.Log?.Error($"Tattoo '{def.Key}': registration error - {ex}");
                return false;
            }
        }

        // Reads an embedded PNG out of a mod's assembly. Tries the exact resource name first, then a
        // forgiving suffix match so callers can pass just "file.png" instead of the full "Namespace.file.png".
        private static byte[] ReadResource(System.Reflection.Assembly asm, string name)
        {
            try
            {
                string actual = name;
                if (asm.GetManifestResourceStream(actual) == null)
                {
                    foreach (string n in asm.GetManifestResourceNames())
                    {
                        if (n == name || n.EndsWith("." + name, StringComparison.OrdinalIgnoreCase))
                        {
                            actual = n;
                            break;
                        }
                    }
                }

                using System.IO.Stream s = asm.GetManifestResourceStream(actual);
                if (s == null)
                {
                    Core.Log?.Warning($"Embedded resource not found: '{name}' in {asm.GetName().Name}");
                    return null;
                }
                using var ms = new System.IO.MemoryStream();
                s.CopyTo(ms);
                return ms.ToArray();
            }
            catch (Exception ex)
            {
                Core.Log?.Warning($"Embedded resource read failed '{name}': {ex.Message}");
                return null;
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
