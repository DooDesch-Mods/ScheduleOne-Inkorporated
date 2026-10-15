#if DEBUG
using System;
using System.IO;
using Il2CppScheduleOne.UI.CharacterCustomization;
using Inkorporated.Model;
using Inkorporated.Registration;
using MelonLoader.Utils;
using UnityEngine;

namespace Inkorporated.Dev
{
    /// <summary>
    /// DEBUG-only, file-driven shop opener so the MCP dev loop can verify shop rows without walking the player
    /// to the tattoo parlour. Drop <c>UserData/Inkorporated/selftest.txt</c> containing "openshop [placement]"
    /// (chest, leftarm, rightarm, face; default face) into place while a save is loaded: the tattoo-shop UI opens,
    /// the category hosting that placement is activated, every injected row's price label is logged with a
    /// [selftest] prefix, and the first injected tattoo is selected so the preview avatar wears it. With a third
    /// word "buy" the tattoo is also bought and the shop closed, which puts it on the player.
    /// </summary>
    internal static class ShopSelfTest
    {
        private static float _nextPoll;
        private static int _step;
        private static float _stepAt;
        private static TattooShopUI _shop;
        private static TattooPlacement _placement;
        private static bool _buy;

        internal static void Tick()
        {
            if (_step > 0) { RunStep(); return; }
            if (Time.unscaledTime < _nextPoll) return;
            _nextPoll = Time.unscaledTime + 2f;

            string path = Path.Combine(MelonEnvironment.UserDataDirectory, "Inkorporated", "selftest.txt");
            try
            {
                if (!File.Exists(path)) return;
                string[] cmd = File.ReadAllText(path).Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                File.Delete(path);
                if (cmd.Length == 0 || cmd[0] != "openshop") { Core.Log?.Warning("[selftest] unknown command: " + string.Join(" ", cmd)); return; }
                _placement = TattooPlacement.Face;
                if (cmd.Length > 1 && !Enum.TryParse(cmd[1], true, out _placement))
                { Core.Log?.Warning("[selftest] unknown placement: " + cmd[1]); return; }
                _buy = cmd.Length > 2 && cmd[2] == "buy";
            }
            catch (Exception e) { Core.Log?.Warning("[selftest] reading " + path + " failed: " + e.Message); return; }
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
                        string src = TattooRegistry.SourceObjectId(_placement);
                        if (src == null) { Core.Log?.Warning("[selftest] no built-in source tattoo for " + _placement); _step = 0; return; }
                        CharacterCustomizationCategory hit = null;
                        foreach (var cat in _shop.Categories)
                        {
                            if (cat == null) continue;
                            foreach (var opt in cat.GetComponentsInChildren<CharacterCustomizationOption>(true))
                                if (opt != null && opt.AvatarObject != null && string.Equals(opt.AvatarObject.Id, src, StringComparison.OrdinalIgnoreCase))
                                { hit = cat; break; }
                            if (hit != null) break;
                        }
                        if (hit == null) { Core.Log?.Warning("[selftest] no tattoo category lists " + src); _step = 0; return; }
                        _shop.SetActiveCategory(hit);
                        CharacterCustomizationOption first = null;
                        foreach (var opt in hit.GetComponentsInChildren<CharacterCustomizationOption>(true))
                            if (opt != null && opt.name.StartsWith("Inkorporated_", StringComparison.Ordinal))
                            {
                                first ??= opt;
                                Core.Log?.Msg($"[selftest] row '{opt.Name}' id '{opt.AvatarObject?.Id}' price label '{(opt.PriceLabel != null ? opt.PriceLabel.text : "(none)")}'");
                            }
                        if (first == null) { Core.Log?.Warning($"[selftest] category '{hit.CategoryName}' has no injected row"); _step = 0; return; }
                        first.SetSelected(true);
                        Core.Log?.Msg($"[selftest] selected '{first.Name}' in '{hit.CategoryName}'");
                        if (!_buy) { _step = 0; return; }
                        first.BuyButton.onClick.Invoke();
                        Core.Log?.Msg($"[selftest] bought '{first.Name}' (purchased={first.purchased})");
                        _step = 3; _stepAt = Time.unscaledTime + 1f;
                        return;
                    }

                    case 3:
                    {
                        // Leaving the shop is what writes the appearance to the player (CharacterCustomizationUI.OnClose).
                        _shop.SetActiveCategory(null);
                        _shop.State.PopFromDefaultParent();
                        Core.Log?.Msg("[selftest] shop closed");
                        _step = 0; return;
                    }
                }
            }
            catch (Exception e) { Core.Log?.Error("[selftest] step " + _step + " threw: " + e); _step = 0; }
        }
    }
}
#endif
