using System;
using System.Linq;
using System.Reflection;
using System.Threading;

using LabApi.Features;
using LabApi.Features.Wrappers;
using LabApi.Loader.Features.Plugins;

using PlayerRoles;

namespace Luke.RespawnTimers
{
    public sealed class RespawnTimersPlugin : Plugin
    {
        public override string Name { get; } = "Luke.RespawnTimers";
        public override string Description { get; } = "Shows spectators a MM:SS timer until the next respawn wave.";
        public override string Author { get; } = "OceLuke";
        public override Version Version { get; } = new Version(1, 0, 0, 0);
        public override Version RequiredApiVersion { get; } = new Version(LabApiProperties.CompiledVersion);

        private Timer _timer;

        public override void Enable()
        {
            // Update once per second (exact mm:ss)
            _timer = new Timer(Tick, null, 0, 1000);
        }

        public override void Disable()
        {
            if (_timer != null)
            {
                _timer.Dispose();
                _timer = null;
            }
        }

        private void Tick(object state)
        {
            try
            {
                int seconds = GetSecondsToNextRespawn();
                if (seconds < 0) seconds = 0;

                string mmss = FormatMmSs(seconds);
                string text = "<align=\"center\"><size=24><b>Respawning in " + mmss + "</b></size></align>";

                foreach (Player p in Player.List)
                {
                    if (p == null || p.ReferenceHub == null)
                        continue;

                    // Spectators only
                    RoleTypeId role = p.ReferenceHub.roleManager.CurrentRole.RoleTypeId;
                    if (role != RoleTypeId.Spectator)
                        continue;

                    // LabAPI hint (works without Ruel/RueI)
                    p.SendHint(text, 1.2f);
                }
            }
            catch
            {
                // Keep the timer loop alive
            }
        }

        private static string FormatMmSs(int totalSeconds)
        {
            int minutes = totalSeconds / 60;
            int seconds = totalSeconds % 60;
            return string.Format("{0:00}:{1:00}", minutes, seconds);
        }

        // Reflection so it works across slight internal API differences
        private static int GetSecondsToNextRespawn()
        {
            Assembly asm = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(a => a.GetName().Name == "Assembly-CSharp");

            if (asm == null)
                return 0;

            // Try Respawning.RespawnManager.Singleton.TimeTillRespawn
            Type rmType = asm.GetType("Respawning.RespawnManager");
            if (rmType != null)
            {
                object singleton = GetStaticMember(rmType, "Singleton");
                if (singleton != null)
                {
                    int v;
                    if (TryGetIntMember(singleton, "TimeTillRespawn", out v)) return v;
                    if (TryGetIntMember(singleton, "SecondsToNextRespawn", out v)) return v;
                }
            }

            // Fallback scan: any Respawning.* singleton with TimeTillRespawn
            Type[] types;
            try { types = asm.GetTypes(); }
            catch { return 0; }

            for (int i = 0; i < types.Length; i++)
            {
                Type t = types[i];
                if (t == null || t.Namespace == null) continue;
                if (!t.Namespace.StartsWith("Respawning")) continue;

                object singleton = GetStaticMember(t, "Singleton");
                if (singleton == null) continue;

                int v;
                if (TryGetIntMember(singleton, "TimeTillRespawn", out v)) return v;
                if (TryGetIntMember(singleton, "SecondsToNextRespawn", out v)) return v;
            }

            return 0;
        }

        private static object GetStaticMember(Type t, string name)
        {
            try
            {
                PropertyInfo p = t.GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                if (p != null) return p.GetValue(null, null);

                FieldInfo f = t.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                if (f != null) return f.GetValue(null);
            }
            catch { }
            return null;
        }

        private static bool TryGetIntMember(object obj, string name, out int value)
        {
            value = 0;
            if (obj == null) return false;

            Type t = obj.GetType();
            try
            {
                PropertyInfo p = t.GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (p != null && p.PropertyType == typeof(int))
                {
                    value = (int)p.GetValue(obj, null);
                    return true;
                }

                FieldInfo f = t.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (f != null && f.FieldType == typeof(int))
                {
                    value = (int)f.GetValue(obj);
                    return true;
                }
            }
            catch { }
            return false;
        }
    }
}
