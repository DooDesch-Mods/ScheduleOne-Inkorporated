using System;
using System.Collections.Generic;
using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppScheduleOne.UI.CharacterCustomization;
using Inkorporated.Model;
using Inkorporated.Registration;
using UnityEngine;
#if DEBUG
using UnityEngine.UI;
#endif

namespace Inkorporated.Shop
{
    /// <summary>
    /// Harmony prefix on the tattoo-shop category's Awake. We clone an existing option button (one per matching
    /// custom tattoo) and parent it under the category BEFORE the original Awake gathers + wires its options - so
    /// the game's own Awake discovers our buttons and wires their select/deselect/purchase events for us.
    /// </summary>
    [HarmonyPatch(typeof(CharacterCustomizationCategory), nameof(CharacterCustomizationCategory.Awake))]
    internal static class CategoryAwakePatch
    {
        private static void Prefix(CharacterCustomizationCategory __instance) => ShopInjector.TryInject(__instance);
    }

    internal static class ShopInjector
    {
        // Guard against double-injecting the same category instance.
        private static readonly HashSet<IntPtr> _processed = new HashSet<IntPtr>();

        public static void Reset() => _processed.Clear();

        public static void TryInject(CharacterCustomizationCategory category)
        {
            try
            {
                if (category == null) return;
                if (!_processed.Add(category.Pointer)) return;

                // Only inject into the tattoo shop's categories (not clothing/face customization etc.).
                TattooShopUI shop = category.GetComponentInParent<TattooShopUI>();
                if (shop == null) return;

                Il2CppArrayBase<CharacterCustomizationOption> existing =
                    category.GetComponentsInChildren<CharacterCustomizationOption>(true);
                if (existing == null || existing.Length == 0) return;

                CharacterCustomizationOption template = PickTemplate(existing);
                if (template == null) return;

                // Which placement(s) does this category host? Built-in option labels carry the placement folder
                // (e.g. ".../chest/...", ".../face/..."), so we only add tattoos whose placement matches.
                var labels = new List<string>();
                for (int i = 0; i < existing.Length; i++)
                {
                    string lbl = existing[i] != null ? existing[i].Label : null;
                    if (!string.IsNullOrEmpty(lbl)) labels.Add(lbl);
                }

                int injected = 0;
                foreach (TattooDef def in TattooRegistry.AllDefs)
                {
                    if (!LabelsContain(labels, TattooRegistry.CategoryToken(def.Placement))) continue;
                    if (!TattooRegistry.EnsureRegistered(def)) continue;
                    if (HasLabel(existing, def.ResourcePath)) continue;
                    if (CloneOption(category, template, def)) injected++;
                }

                if (injected > 0)
                {
                    Transform content = template.transform.parent;
                    // The vanilla list ends with a tall 'Spacer' child (bottom scroll padding). We appended our
                    // options after it, which split the list and broke the game's price-sort (it assumes options
                    // are contiguous). Move every non-option child (the Spacer) to the end so all options are
                    // contiguous; the game's own Awake sort then orders them and the spacer pads the bottom.
                    if (content != null)
                    {
                        var nonOptions = new List<Transform>();
                        for (int i = 0; i < content.childCount; i++)
                        {
                            Transform ch = content.GetChild(i);
                            if (ch.GetComponent<CharacterCustomizationOption>() == null) nonOptions.Add(ch);
                        }
                        foreach (Transform ch in nonOptions) ch.SetAsLastSibling();
                    }
                    Core.Log?.Msg($"Injected {injected} custom tattoo(s) into category '{category.CategoryName}'.");
#if DEBUG
                    LogLayout(category.CategoryName, content);
#endif
                }
            }
            catch (Exception ex)
            {
                Core.Log?.Warning($"Shop injection error: {ex.Message}");
            }
        }

#if DEBUG
        // Dumps the option container's layout components + each child's rect so we can see exactly why a gap forms.
        private static void LogLayout(string cat, Transform content)
        {
            try
            {
                if (content == null) { Core.Log?.Msg($"[layout] {cat}: content null"); return; }
                RectTransform rt = content.TryCast<RectTransform>();
                bool vlg = content.GetComponent<VerticalLayoutGroup>() != null;
                bool csf = content.GetComponent<ContentSizeFitter>() != null;
                Core.Log?.Msg($"[layout] {cat}: container='{content.name}' children={content.childCount} VLG={vlg} CSF={csf} size={(rt != null ? rt.sizeDelta.ToString() : "?")}");
                int n = content.childCount;
                for (int i = 0; i < n; i++)
                {
                    Transform ch = content.GetChild(i);
                    RectTransform crt = ch.TryCast<RectTransform>();
                    LayoutElement le = ch.GetComponent<LayoutElement>();
                    string les = le != null ? $"LE(min={le.minHeight},pref={le.preferredHeight},ignore={le.ignoreLayout})" : "noLE";
                    string opt = ch.GetComponent<CharacterCustomizationOption>() != null ? "OPT" : "---";
                    Core.Log?.Msg($"[layout]   #{i} '{ch.name}' active={ch.gameObject.activeSelf} {opt} size={(crt != null ? crt.sizeDelta.ToString() : "?")} pos={(crt != null ? crt.anchoredPosition.ToString() : "?")} {les}");
                }
            }
            catch (Exception ex) { Core.Log?.Warning($"[layout] diag error: {ex.Message}"); }
        }
#endif

        private static CharacterCustomizationOption PickTemplate(Il2CppArrayBase<CharacterCustomizationOption> opts)
        {
            for (int i = 0; i < opts.Length; i++)
                if (opts[i] != null && opts[i].gameObject.activeSelf) return opts[i];
            return opts.Length > 0 ? opts[0] : null;
        }

        private static bool LabelsContain(List<string> labels, string token)
        {
            foreach (string l in labels)
                if (l.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0) return true;
            return false;
        }

        private static bool HasLabel(Il2CppArrayBase<CharacterCustomizationOption> opts, string label)
        {
            if (label == null) return false;
            for (int i = 0; i < opts.Length; i++)
                if (opts[i] != null && string.Equals(opts[i].Label, label, StringComparison.Ordinal)) return true;
            return false;
        }

        private static bool CloneOption(CharacterCustomizationCategory category, CharacterCustomizationOption template, TattooDef def)
        {
            try
            {
                // Parent into the SAME container as the existing options (the ScrollRect content), keeping local
                // space - otherwise the clone lands outside the vertical layout group and the list breaks.
                Transform parent = template.transform.parent;
                GameObject clone = UnityEngine.Object.Instantiate(template.gameObject, parent, false).Cast<GameObject>();
                clone.transform.localScale = Vector3.one;
                clone.name = "Inkorporated_" + def.Source + "_" + def.Id;

                CharacterCustomizationOption opt = clone.GetComponent<CharacterCustomizationOption>();
                if (opt == null) { UnityEngine.Object.Destroy(clone); return false; }

                opt.Name = def.DisplayName;
                opt.Label = def.ResourcePath;   // == registered Resources path; added to Tattoos on select
                opt.Price = def.Price;
                opt.RequireLevel = false;

                if (opt.NameLabel != null) opt.NameLabel.text = def.DisplayName;
                if (opt.PriceLabel != null) opt.PriceLabel.text = def.Price > 0f ? Mathf.RoundToInt(def.Price).ToString() : "Free";

                if (!clone.activeSelf) clone.SetActive(true);
                return true;
            }
            catch (Exception ex)
            {
                Core.Log?.Warning($"Tattoo '{def.Key}': failed to create shop button - {ex.Message}");
                return false;
            }
        }
    }
}
