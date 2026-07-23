#if DEBUG
using System;
using System.IO;
using Il2CppScheduleOne.UI.CharacterCustomization;
using MelonLoader.Utils;
using UnityEngine;

namespace Inkorporated.Dev
{
    /// <summary>
    /// DEBUG-only, file-driven shop opener so the MCP dev loop can verify shop rows without walking the player
    /// to the tattoo parlour. Drop <c>UserData/Inkorporated/selftest.txt</c> containing "openshop" into place
    /// while a save is loaded: the tattoo-shop UI opens, the face category is activated and every injected
    /// row's price label is logged with a [selftest] prefix.
    /// </summary>
    internal static class ShopSelfTest
    {
        private static float _nextPoll;
        private static int _step;
        private static float _stepAt;
        private static TattooShopUI _shop;

        internal static void Tick()
        {
            if (_step > 0) { RunStep(); return; }
            if (Time.unscaledTime < _nextPoll) return;
            _nextPoll = Time.unscaledTime + 2f;

            string path = Path.Combine(MelonEnvironment.UserDataDirectory, "Inkorporated", "selftest.txt");
            try
            {
                if (!File.Exists(path)) return;
                string cmd = File.ReadAllText(path).Trim();
                File.Delete(path);
                if (cmd != "openshop") { Core.Log?.Warning("[selftest] unknown command: " + cmd); return; }
            }
            catch { return; }
            _step = 1; _stepAt = Time.unscaledTime;
            Core.Log?.Msg("[selftest] openshop starting");
        }

        private static void RunStep()
        {
            if (Time.unscaledTime < _stepAt) return;
            try
            {
                switch (_step)
                {
                    case 1:
                        _shop = UnityEngine.Object.FindObjectOfType<TattooShopUI>(true);
                        if (_shop == null) { Core.Log?.Warning("[selftest] no TattooShopUI in this scene (load a save first)"); _step = 0; return; }
                        _shop.Open();
                        _step = 2; _stepAt = Time.unscaledTime + 2f;   // Open fades through a 0.6s black overlay
                        return;

                    case 2:
                    {
                        CharacterCustomizationCategory face = null;
                        foreach (var cat in _shop.Categories)
                        {
                            if (cat == null) continue;
                            foreach (var opt in cat.GetComponentsInChildren<CharacterCustomizationOption>(true))
                                if (opt != null && opt.Label != null && opt.Label.Contains("/face/", StringComparison.OrdinalIgnoreCase))
                                { face = cat; break; }
                            if (face != null) break;
                        }
                        if (face == null) { Core.Log?.Warning("[selftest] no face tattoo category found"); _step = 0; return; }
                        _shop.SetActiveCategory(face);
                        foreach (var opt in face.GetComponentsInChildren<CharacterCustomizationOption>(true))
                            if (opt != null && opt.name.StartsWith("Inkorporated_", StringComparison.Ordinal))
                                Core.Log?.Msg($"[selftest] row '{opt.Name}' price label '{(opt.PriceLabel != null ? opt.PriceLabel.text : "(none)")}'");
                        _step = 0; return;
                    }
                }
            }
            catch (Exception e) { Core.Log?.Error("[selftest] step " + _step + " threw: " + e); _step = 0; }
        }
    }
}
#endif
