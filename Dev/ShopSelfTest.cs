#if DEBUG
using System;
using System.IO;
using Il2CppScheduleOne.DevUtilities;
using Il2CppScheduleOne.PlayerScripts;
using Il2CppScheduleOne.UI.CharacterCustomization;
using Inkorporated.Model;
using Inkorporated.Registration;
using MelonLoader.Utils;
using UnityEngine;

namespace Inkorporated.Dev
{
    /// <summary>
    /// DEBUG-only, file-driven shop driver so the MCP dev loop can verify shop rows and frame screenshots without
    /// walking the player to the tattoo parlour. Drop <c>UserData/Inkorporated/selftest.txt</c> with one command
    /// while a save is loaded:
    /// <list type="bullet">
    /// <item>"openshop [placement] [buy] [id]" - opens the tattoo shop, activates the category hosting that
    /// placement (chest, leftarm, rightarm, face; default face), logs every injected row and selects the first
    /// one, or the row whose id ends with [id]. "buy" also buys it and closes the shop.</item>
    /// <item>"rotate 0..1" - the preview rig's rotation slider.</item>
    /// <item>"cam up fwd right fov" - moves the shop camera relative to its anchor and sets the FOV.</item>
    /// <item>"ui on|off" - shows or hides the shop canvas.</item>
    /// <item>"close" - leaves the shop.</item>
    /// </list>
    /// </summary>
    internal static class ShopSelfTest
    {
        private static float _nextPoll;
        private static int _step;
        private static float _stepAt;
        private static TattooShopUI _shop;
        private static TattooPlacement _placement;
        private static bool _buy;
        private static string _pick;

        internal static void Tick()
        {
            if (_step > 0) { RunStep(); return; }
            if (Time.unscaledTime < _nextPoll) return;
            _nextPoll = Time.unscaledTime + 1f;

            string path = Path.Combine(MelonEnvironment.UserDataDirectory, "Inkorporated", "selftest.txt");
            string[] cmd;
            try
            {
                if (!File.Exists(path)) return;
                cmd = File.ReadAllText(path).Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                File.Delete(path);
            }
            catch (Exception e) { Core.Log?.Warning("[selftest] reading " + path + " failed: " + e.Message); return; }
            if (cmd.Length == 0) { Core.Log?.Warning("[selftest] empty command"); return; }

            try
            {
                switch (cmd[0])
                {
                    case "openshop":
                        _placement = TattooPlacement.Face;
                        if (cmd.Length > 1 && !Enum.TryParse(cmd[1], true, out _placement))
                        { Core.Log?.Warning("[selftest] unknown placement: " + cmd[1]); return; }
                        _buy = cmd.Length > 2 && cmd[2] == "buy";
                        _pick = cmd.Length > 3 ? cmd[3] : (cmd.Length > 2 && cmd[2] != "buy" ? cmd[2] : null);
                        _step = 1; _stepAt = Time.unscaledTime;
                        Core.Log?.Msg("[selftest] openshop starting");
                        return;
                    case "rotate":
                        if (!RequireShop()) return;
                        _shop.RigRotationSlider.value = float.Parse(cmd[1], System.Globalization.CultureInfo.InvariantCulture);
                        Core.Log?.Msg("[selftest] rotate " + _shop.RigRotationSlider.value);
                        return;
                    case "cam":
                    {
                        if (!RequireShop()) return;
                        var inv = System.Globalization.CultureInfo.InvariantCulture;
                        float up = float.Parse(cmd[1], inv), fwd = float.Parse(cmd[2], inv), right = float.Parse(cmd[3], inv), fov = float.Parse(cmd[4], inv);
                        Transform anchor = _shop.CharacterCustomizationShop.CameraPosition;
                        Vector3 pos = anchor.position + anchor.up * up + anchor.forward * fwd + anchor.right * right;
                        PlayerSingleton<PlayerCamera>.Instance.OverrideTransform(pos, anchor.rotation, 0f);
                        PlayerSingleton<PlayerCamera>.Instance.OverrideFOV(fov, 0f);
                        Core.Log?.Msg($"[selftest] cam up {up} fwd {fwd} right {right} fov {fov}");
                        return;
                    }
                    case "ui":
                        if (!RequireShop()) return;
                        _shop.Canvas.enabled = cmd.Length < 2 || cmd[1] != "off";
                        Core.Log?.Msg("[selftest] ui " + (_shop.Canvas.enabled ? "on" : "off"));
                        return;
                    case "close":
                        if (!RequireShop()) return;
                        _shop.SetActiveCategory(null);
                        _shop.State.PopFromDefaultParent();
                        Core.Log?.Msg("[selftest] shop closed");
                        return;
                    default:
                        Core.Log?.Warning("[selftest] unknown command: " + string.Join(" ", cmd));
                        return;
                }
            }
            catch (Exception e) { Core.Log?.Error("[selftest] '" + string.Join(" ", cmd) + "' threw: " + e); }
        }

        private static bool RequireShop()
        {
            if (_shop != null && _shop.IsOpen) return true;
            Core.Log?.Warning("[selftest] shop is not open (run openshop first)");
            return false;
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
                        if (!_shop.IsOpen) { _shop.Open(); _stepAt = Time.unscaledTime + 2f; }   // Open fades through a 0.6s black overlay
                        _step = 2;
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
                                string id = opt.AvatarObject?.Id ?? "";
                                if (first == null && (_pick == null || id.EndsWith(_pick, StringComparison.OrdinalIgnoreCase))) first = opt;
                                Core.Log?.Msg($"[selftest] row '{opt.Name}' id '{id}' price label '{(opt.PriceLabel != null ? opt.PriceLabel.text : "(none)")}'");
                            }
                        if (first == null) { Core.Log?.Warning($"[selftest] category '{hit.CategoryName}' has no injected row matching '{_pick}'"); _step = 0; return; }
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
