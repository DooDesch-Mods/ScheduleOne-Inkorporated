using System;
using System.Reflection;
using Inkorporated.Config;
using Inkorporated.Content;
using Inkorporated.Registration;
using Inkorporated.Shop;
using MelonLoader;

[assembly: MelonInfo(typeof(Inkorporated.Core), "Inkorporated", "1.1.0", "DooDesch", "https://github.com/DooDesch-Mods/ScheduleOne-Inkorporated")]
[assembly: MelonGame("TVGS", "Schedule I")]
[assembly: MelonOptionalDependencies("ModManager&PhoneApp")]

namespace Inkorporated
{
    /// <summary>
    /// MelonLoader entry point for Inkorporated. On init it loads user tattoo packs (metadata only) and arms the
    /// Harmony patch that injects shop buttons when the tattoo-shop UI awakes. Custom tattoo textures are realized
    /// lazily (the first time the shop opens), so the avatar resources are guaranteed loaded. Other mods can add
    /// tattoos via <see cref="API"/>.
    /// </summary>
    public sealed class Core : MelonMod
    {
        public static Core Instance { get; private set; }
        public static MelonLogger.Instance Log { get; private set; }

        public override void OnInitializeMelon()
        {
            Instance = this;
            Log = LoggerInstance;

            Preferences.Initialize();
            ExamplePack.ExtractIfEnabled();

            int packDefs = TattooRegistry.AddRange(PackLoader.LoadAll());

            try
            {
                HarmonyInstance.PatchAll(Assembly.GetExecutingAssembly());
            }
            catch (Exception e)
            {
                Log.Warning("Harmony patch failed: " + e.Message);
            }

            Log.Msg($"Inkorporated 1.1.0 - {packDefs} pack tattoo(s) loaded ({TattooRegistry.AllDefs.Count} total). Shop injection armed.");
            Log.Msg($"Drop packs in: {PackLoader.PacksRoot}");
        }

        public override void OnSceneWasUnloaded(int buildIndex, string sceneName)
        {
            // The customization UI is rebuilt with the scene; allow our prefix to re-inject next time.
            ShopInjector.Reset();
        }
    }
}
