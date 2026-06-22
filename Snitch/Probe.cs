#if SNITCH
using Snitch.Api;
using Inkorporated.Registration;  // TattooRegistry

namespace Inkorporated.Profiling
{
    /// <summary>
    /// DEBUG-only Snitch instrumentation for Inkorporated. This is a load-time library mod with no per-frame
    /// cost, so the profiler value is the count of registered tattoos (also catches other mods adding via the
    /// public API) plus a load-time section in Core. No-op when the Snitch host is absent. Compiled only when
    /// SNITCH is defined (Debug + EnableSnitch); excluded from Release. See Workspace/build/Snitch.props.
    /// </summary>
    internal static class SnitchProbe
    {
        public static void Register()
        {
            Profiler.RegisterCounter("Inkorporated.Tattoos", () => TattooRegistry.AllDefs.Count, "tattoos");
        }
    }
}
#endif
