using MelonLoader;

namespace Inkorporated.Config
{
    /// <summary>
    /// MelonPreferences wrapper. The category id is prefixed with the mod name ("Inkorporated_...") so it is
    /// auto-detected by the "Mod Manager &amp; Phone App" settings UI. Inkorporated is primarily a library, so
    /// it has a single user-facing toggle: whether to drop the bundled example pack on disk as a template.
    /// </summary>
    internal static class Preferences
    {
        private const string CategoryId = "Inkorporated_01_Main";

        private static MelonPreferences_Category _category;
        private static MelonPreferences_Entry<bool> _loadExamplePack;

        internal static void Initialize()
        {
            if (_category != null) return;

            _category = MelonPreferences.CreateCategory(CategoryId, "Inkorporated (Custom Tattoos)");

            _loadExamplePack = _category.CreateEntry("LoadExamplePack", false, "Load example tattoo pack",
                "OFF by default. When ON, Inkorporated drops a small bundled example pack into " +
                "UserData/Inkorporated/Packs/Examples on startup (if not already there) so you get a few ready-made " +
                "tattoos plus a working folder/manifest template to copy for your own pack. Requires a game restart.");
        }

        internal static bool LoadExamplePack => _loadExamplePack?.Value ?? false;
    }
}
