#if DEBUG
using System;
using System.Collections.Generic;
using System.IO;
using Il2CppInterop.Runtime;
using Il2CppScheduleOne.AvatarFramework;
using MelonLoader.Utils;
using UnityEngine;

namespace Inkorporated.Dev
{
    /// <summary>
    /// DEBUG-only helper: exports the game's built-in tattoo textures to PNG so they can be used as UV-aligned
    /// authoring templates (a custom tattoo must paint into the same UV region as the built-ins to land correctly).
    /// Uses a RenderTexture readback so it works even for non-CPU-readable (GPU-only) textures.
    /// Output: &lt;UserData&gt;/Inkorporated/Templates/&lt;placement&gt;/&lt;name&gt;.png
    /// </summary>
    internal static class TemplateDumper
    {
        private static readonly Dictionary<string, string[]> BuiltIn = new Dictionary<string, string[]>
        {
            ["chest"] = new[] { "Chest_Bird", "Chest_DeadFace", "Chest_Egg", "Chest_LBC", "Chest_Sword" },
            ["leftarm"] = new[] { "LeftArm_Alien", "LeftArm_Heart", "LeftArm_Peace", "LeftArm_Web", "LeftArm_Weed" },
            ["rightarm"] = new[] { "RightArm_Alien", "RightArm_Heart", "RightArm_Peace", "RightArm_Web", "RightArm_Weed" },
            ["face"] = new[] { "Face_ForeheadCross", "Face_Sword", "Face_Teardrop", "Face_Tribal" }
        };

        /// <summary>
        /// Exports all built-in tattoo textures as UV templates. Safe to call repeatedly. Returns a short summary
        /// line (also written to the MelonLogger) so callers can surface it (e.g. in the Snitch panel log).
        /// Must run on the main thread while in-world (Resources.Load needs the avatar resources loaded).
        /// </summary>
        public static string Dump()
        {
            string root = Path.Combine(MelonEnvironment.UserDataDirectory, "Inkorporated", "Templates");
            Directory.CreateDirectory(root);
            WriteReadme(root);

            int ok = 0, fail = 0;
            foreach (KeyValuePair<string, string[]> kv in BuiltIn)
            {
                string placement = kv.Key;
                string dir = Path.Combine(root, placement);
                Directory.CreateDirectory(dir);

                foreach (string name in kv.Value)
                {
                    string resPath = $"Avatar/Layers/Tattoos/{placement}/{name}";
                    try
                    {
                        AvatarLayer layer = Resources.Load<AvatarLayer>(resPath);
                        if (layer == null) { Core.Log?.Warning($"[template] layer not found: {resPath}"); fail++; continue; }

                        if (TryDump(layer.Texture, Path.Combine(dir, name + ".png"))) ok++; else fail++;
                        if (layer.Normal != null) TryDump(layer.Normal, Path.Combine(dir, name + "_normal.png"));
                    }
                    catch (Exception ex)
                    {
                        Core.Log?.Warning($"[template] {resPath}: {ex.Message}");
                        fail++;
                    }
                }
            }

            string summary = $"Dumped {ok} built-in tattoo template(s) ({fail} failed) -> {root}";
            Core.Log?.Msg($"[template] {summary}");
            return summary;
        }

        private static bool TryDump(Texture2D src, string path)
        {
            if (src == null) return false;
            Texture2D readable = null;
            RenderTexture rt = null;
            RenderTexture prev = RenderTexture.active;
            try
            {
                rt = RenderTexture.GetTemporary(src.width, src.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
                Graphics.Blit(src, rt);
                RenderTexture.active = rt;

                readable = new Texture2D(src.width, src.height, TextureFormat.RGBA32, false);
                readable.ReadPixels(new Rect(0, 0, src.width, src.height), 0, 0);
                readable.Apply(false);

                byte[] png = ImageConversion.EncodeToPNG(readable);
                if (png == null || png.Length == 0) return false;
                File.WriteAllBytes(path, png);
                return true;
            }
            catch (Exception ex)
            {
                Core.Log?.Warning($"[template] readback failed for '{path}': {ex.Message}");
                return false;
            }
            finally
            {
                RenderTexture.active = prev;
                if (rt != null) RenderTexture.ReleaseTemporary(rt);
                if (readable != null) UnityEngine.Object.Destroy(readable);
            }
        }

        private static void WriteReadme(string root)
        {
            string readme = Path.Combine(root, "README.txt");
            try
            {
                File.WriteAllText(readme,
@"Inkorporated - UV templates (auto-exported, DEBUG build)
=======================================================

These PNGs are the game's BUILT-IN tattoo textures, exported as-is. They show exactly where to paint
a custom tattoo so it lands in the right place on the avatar's body/face. A custom tattoo PNG should
use the SAME 512-square canvas and keep its opaque ink inside the same UV region you see here.

Per placement (chest / leftarm / rightarm / face), open any file as a reference layer in your editor,
draw your design over the inked area, then export your own transparent PNG at the same size.
");
            }
            catch { /* non-fatal */ }
        }
    }
}
#endif
