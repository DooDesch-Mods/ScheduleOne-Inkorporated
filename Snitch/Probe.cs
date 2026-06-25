#if SNITCH
using System.Collections.Generic;
using Snitch.Api;
using Inkorporated.Model;          // TattooDef
using Inkorporated.Registration;   // TattooRegistry

namespace Inkorporated.Profiling
{
    /// <summary>
    /// DEBUG-only Snitch instrumentation for Inkorporated. This is a load-time library mod with no per-frame cost,
    /// so the panel mainly reports how much content is registered (pack tattoos plus anything other mods add via the
    /// public API). It also exposes the built-in tattoo UV-template export (formerly an auto-run on world entry) as
    /// an on-demand "Dump Templates" button. No-op when the Snitch host is absent. Compiled only when SNITCH is
    /// defined (Debug + EnableSnitch); excluded from Release. See Workspace/build/Snitch.props.
    /// </summary>
    internal static class SnitchProbe
    {
        public static void Register()
        {
            Panel p = Profiler.RegisterPanel("Inkorporated", "Inkorporated");

            p.Counter("Packs", () => DistinctPackCount(), "packs");
            p.Counter("Tattoos", () => TattooRegistry.AllDefs.Count, "tattoos");

            // Replaces the old timed auto-export on world entry: run the UV-template dump on demand.
            // TemplateDumper is #if DEBUG; SNITCH is only defined in Debug, so it is always present here.
            p.Action("Dump Templates", () =>
            {
                string summary = Dev.TemplateDumper.Dump();
                Profiler.Log("Inkorporated", "Template export: " + summary);
            });

            p.Log();
        }

        // Distinct originating pack names ("API" entries are excluded so this reflects user content packs).
        private static int DistinctPackCount()
        {
            var seen = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
            foreach (TattooDef d in TattooRegistry.AllDefs)
            {
                string src = d?.Source;
                if (!string.IsNullOrEmpty(src) && src != "API") seen.Add(src);
            }
            return seen.Count;
        }
    }
}
#endif
