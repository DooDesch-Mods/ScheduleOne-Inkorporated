using System;
using System.Collections.Generic;
using System.IO;
using Inkorporated.Model;
using MelonLoader.Utils;
using Newtonsoft.Json;

namespace Inkorporated.Content
{
    /// <summary>
    /// Discovers user content packs under <c>UserData/Inkorporated/Packs/&lt;pack&gt;/manifest.json</c> and turns each
    /// manifest entry into a <see cref="TattooDef"/> (metadata only - no Unity calls; textures load lazily later).
    /// </summary>
    internal static class PackLoader
    {
        /// <summary>Root the user drops packs into. Created on first run with a short README.</summary>
        public static string PacksRoot => Path.Combine(MelonEnvironment.UserDataDirectory, "Inkorporated", "Packs");

        public static List<TattooDef> LoadAll()
        {
            var defs = new List<TattooDef>();
            string root = PacksRoot;

            try
            {
                Directory.CreateDirectory(root);
                WriteReadmeIfMissing(root);
            }
            catch (Exception ex)
            {
                Core.Log?.Warning($"Could not prepare packs folder '{root}': {ex.Message}");
                return defs;
            }

            foreach (string packDir in Directory.GetDirectories(root))
            {
                string manifestPath = Path.Combine(packDir, "manifest.json");
                if (!File.Exists(manifestPath))
                    continue;

                string packName = new DirectoryInfo(packDir).Name;
                try
                {
                    PackManifest manifest = JsonConvert.DeserializeObject<PackManifest>(File.ReadAllText(manifestPath));
                    if (manifest?.tattoos == null)
                    {
                        Core.Log?.Warning($"Pack '{packName}': manifest has no 'tattoos' array - skipped.");
                        continue;
                    }

                    int added = 0;
                    foreach (ManifestEntry e in manifest.tattoos)
                    {
                        TattooDef def = ToDef(packName, packDir, e);
                        if (def != null) { defs.Add(def); added++; }
                    }
                    Core.Log?.Msg($"Pack '{packName}' ({manifest.name ?? "unnamed"}): {added} tattoo(s).");
                }
                catch (Exception ex)
                {
                    Core.Log?.Warning($"Pack '{packName}': failed to read manifest.json - {ex.Message}");
                }
            }

            return defs;
        }

        private static TattooDef ToDef(string packName, string packDir, ManifestEntry e)
        {
            if (e == null || string.IsNullOrWhiteSpace(e.id))
            {
                Core.Log?.Warning($"Pack '{packName}': an entry is missing 'id' - skipped.");
                return null;
            }
            if (!TryParsePlacement(e.placement, out TattooPlacement placement))
            {
                Core.Log?.Warning($"Pack '{packName}' tattoo '{e.id}': unknown placement '{e.placement}' (expected chest|leftarm|rightarm|face) - skipped.");
                return null;
            }
            string file = string.IsNullOrWhiteSpace(e.file) ? (e.id + ".png") : e.file;
            string png = Path.Combine(packDir, file);
            if (!File.Exists(png))
            {
                Core.Log?.Warning($"Pack '{packName}' tattoo '{e.id}': PNG not found at '{png}' - skipped.");
                return null;
            }

            return new TattooDef
            {
                Id = e.id,
                DisplayName = string.IsNullOrWhiteSpace(e.name) ? e.id : e.name,
                Placement = placement,
                Price = e.price < 0f ? 0f : e.price,
                PngPath = png,
                Source = packName
            };
        }

        private static bool TryParsePlacement(string s, out TattooPlacement placement)
        {
            placement = TattooPlacement.Chest;
            if (string.IsNullOrWhiteSpace(s)) return false;
            switch (s.Trim().ToLowerInvariant())
            {
                case "chest": placement = TattooPlacement.Chest; return true;
                case "leftarm": case "left_arm": case "left": placement = TattooPlacement.LeftArm; return true;
                case "rightarm": case "right_arm": case "right": placement = TattooPlacement.RightArm; return true;
                case "face": placement = TattooPlacement.Face; return true;
                default: return false;
            }
        }

        private static void WriteReadmeIfMissing(string root)
        {
            string readme = Path.Combine(root, "README.txt");
            if (File.Exists(readme)) return;
            File.WriteAllText(readme,
@"Inkorporated - custom tattoo packs
==================================

Drop one folder per pack in this directory. Each pack needs a manifest.json and the PNG files it lists.

Folder layout:
  Packs/
    MyPack/
      manifest.json
      my_chest_tattoo.png
      my_face_tattoo.png

manifest.json:
{
  ""name"": ""My Pack"",
  ""author"": ""you"",
  ""tattoos"": [
    { ""id"": ""skull"",   ""name"": ""Skull"",      ""placement"": ""chest"",    ""file"": ""my_chest_tattoo.png"" },
    { ""id"": ""teardrop"",""name"": ""Teardrop X"", ""placement"": ""face"",     ""file"": ""my_face_tattoo.png"", ""price"": 250 }
  ]
}

placement: chest | leftarm | rightarm | face
price:     optional, omit or 0 for ""Free""

Tip: PNGs must match the game's body/face UV layout to sit correctly on the skin. Use an existing
in-game tattoo texture as a template. Tattoos appear in the in-game tattoo shop.
");
        }
    }
}
