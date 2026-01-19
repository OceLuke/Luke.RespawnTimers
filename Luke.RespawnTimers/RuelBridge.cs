using System;
using System.Linq;
using System.Reflection;

using LabApi.Features.Wrappers;

namespace Luke.RespawnTimers
{
    internal static class RuelBridge
    {
        private static bool _initialized;
        private static Assembly _ruelAsm;

        private static MethodInfo _showMethod;     // best guess method to show
        private static MethodInfo _hideAllMethod;  // optional

        public static bool TryShow(Player player, string channelId, string text, float durationSeconds)
        {
            try
            {
                EnsureInitialized();
                if (_ruelAsm == null || _showMethod == null)
                    return false;

                // We try to invoke "Show" with a flexible signature.
                // Common argument combos:
                //   (ReferenceHub hub, string id, string text, float duration)
                //   (Player player, string id, string text, float duration)
                //   (ReferenceHub hub, string text, float duration)
                // etc.
                object hub = player.ReferenceHub;

                var pars = _showMethod.GetParameters();
                object[] args = BuildArgs(pars, player, hub, channelId, text, durationSeconds);
                if (args == null)
                    return false;

                _showMethod.Invoke(null, args);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static void TryHideAll(string channelId)
        {
            try
            {
                EnsureInitialized();
                if (_hideAllMethod == null) return;

                var pars = _hideAllMethod.GetParameters();
                if (pars.Length == 1 && pars[0].ParameterType == typeof(string))
                    _hideAllMethod.Invoke(null, new object[] { channelId });
            }
            catch { }
        }

        private static void EnsureInitialized()
        {
            if (_initialized) return;
            _initialized = true;

            _ruelAsm = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(a => a.GetName().Name.Equals("Ruel", StringComparison.OrdinalIgnoreCase));

            if (_ruelAsm == null)
                return;

            // Find a static method named like "Show" that likely renders hints.
            // We search for types containing "Hint" or "Hud" or "Overlay".
            var types = SafeGetTypes(_ruelAsm);

            _showMethod =
                FindBestStaticMethod(types, "Show") ??
                FindBestStaticMethod(types, "ShowHint") ??
                FindBestStaticMethod(types, "Display") ??
                FindBestStaticMethod(types, "Render");

            // Optional: hide/clear API if available
            _hideAllMethod =
                FindBestStaticMethod(types, "HideAll") ??
                FindBestStaticMethod(types, "ClearAll") ??
                FindBestStaticMethod(types, "RemoveAll");
        }

        private static Type[] SafeGetTypes(Assembly asm)
        {
            try { return asm.GetTypes(); }
            catch { return new Type[0]; }
        }

        private static MethodInfo FindBestStaticMethod(Type[] types, string methodName)
        {
            for (int i = 0; i < types.Length; i++)
            {
                var t = types[i];
                if (t == null) continue;

                string tn = t.FullName ?? "";
                string low = tn.ToLowerInvariant();
                if (!(low.Contains("hint") || low.Contains("hud") || low.Contains("overlay") || low.Contains("ui")))
                    continue;

                var m = t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
                    .FirstOrDefault(x => x.Name.Equals(methodName, StringComparison.OrdinalIgnoreCase));

                if (m != null)
                    return m;
            }
            return null;
        }

        private static object[] BuildArgs(ParameterInfo[] pars, Player player, object hub, string id, string text, float duration)
        {
            // Try to map parameters by type/name
            object[] args = new object[pars.Length];

            for (int i = 0; i < pars.Length; i++)
            {
                var p = pars[i];
                var pt = p.ParameterType;

                if (pt.IsInstanceOfType(player)) { args[i] = player; continue; }
                if (hub != null && pt.IsInstanceOfType(hub)) { args[i] = hub; continue; }

                if (pt == typeof(string))
                {
                    // Heuristic: first string param tends to be id/channel, second string param tends to be text
                    // Use parameter name if possible
                    string pn = (p.Name ?? "").ToLowerInvariant();
                    if (pn.Contains("id") || pn.Contains("key") || pn.Contains("name") || pn.Contains("channel"))
                        args[i] = id;
                    else
                        args[i] = text;

                    continue;
                }

                if (pt == typeof(float)) { args[i] = duration; continue; }
                if (pt == typeof(double)) { args[i] = (double)duration; continue; }
                if (pt == typeof(int)) { args[i] = (int)Math.Ceiling(duration); continue; }

                // Unknown parameter type -> we can't safely call this overload
                return null;
            }

            return args;
        }
    }
}
