using System;
using System.IO;
using System.Reflection;
using Inkorporated.Config;

namespace Inkorporated.Content
{
    /// <summary>
    /// Extracts the bundled example pack (embedded PNGs + manifest) to
    /// <c>UserData/Inkorporated/Packs/Examples</c> when the <see cref="Preferences.LoadExamplePack"/> toggle is on,
    /// so users get a few ready-made tattoos and a working folder/manifest template to copy. Never overwrites an
    /// existing Examples folder (so user edits survive).
    /// </summary>
    internal static class ExamplePack
    {
        // Embedded resource name prefix: "<RootNamespace>.<folder path with dots>."
        private const string ResourcePrefix = "Inkorporated.Assets.ExamplePack.";

        public static void ExtractIfEnabled()
        {
            if (!Preferences.LoadExamplePack) return;

            string dir = Path.Combine(PackLoader.PacksRoot, "Examples");
            try
            {
                if (Directory.Exists(dir) && File.Exists(Path.Combine(dir, "manifest.json")))
                {
                    Core.Log?.Msg("Example pack already present - leaving it untouched.");
                    return;
                }

                Directory.CreateDirectory(dir);
                Assembly asm = Assembly.GetExecutingAssembly();
                int n = 0;
                foreach (string res in asm.GetManifestResourceNames())
                {
                    if (!res.StartsWith(ResourcePrefix, StringComparison.Ordinal)) continue;
                    string fileName = res.Substring(ResourcePrefix.Length);
                    using Stream s = asm.GetManifestResourceStream(res);
                    if (s == null) continue;
                    using FileStream fs = File.Create(Path.Combine(dir, fileName));
                    s.CopyTo(fs);
                    n++;
                }
                Core.Log?.Msg($"Extracted bundled example pack ({n} file(s)) -> {dir}");
            }
            catch (Exception ex)
            {
                Core.Log?.Warning($"Example pack extraction failed: {ex.Message}");
            }
        }
    }
}
