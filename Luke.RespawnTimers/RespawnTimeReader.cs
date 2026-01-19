using System;
using System.Linq;
using System.Reflection;

namespace Luke.RespawnTimers
{
    internal static class RespawnTimeReader
    {
        public static int GetSecondsToNextRespawn()
        {
            Assembly asm = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(a => a.GetName().Name == "Assembly-CSharp");

            if (asm == null)
                return 0;

            Type rmType = asm.GetType("Respawning.RespawnManager");
            if (rmType == null)
                return 0;

            object singleton = GetStaticMember(rmType, "Singleton");
            if (singleton == null)
                return 0;

            // Common candidates
            string[] candidates =
            {
                "TimeTillRespawn",
                "TimeTillNextRespawn",
                "TimeToNextRespawn",
                "TimeToNextWave",
                "SecondsToNextRespawn",
                "SecondsToNextWave",
                "_timeToNextRespawn",
                "_timeTillRespawn",
                "_respawnTime",
                "_nextRespawnTime",
                "NextRespawnTime",
            };

            for (int i = 0; i < candidates.Length; i++)
            {
                int seconds;
                if (TryGetNumberMemberAsSeconds(singleton, candidates[i], out seconds))
                    return Clamp(seconds);
            }

            return 0;
        }

        private static int Clamp(int seconds)
        {
            if (seconds < 0) return 0;
            if (seconds > 3600) return 3600;
            return seconds;
        }

        private static bool TryGetNumberMemberAsSeconds(object obj, string name, out int seconds)
        {
            seconds = 0;
            if (obj == null) return false;

            Type t = obj.GetType();

            try
            {
                PropertyInfo p = t.GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (p != null)
                    return TryReadNumericAsSeconds(p.PropertyType, () => p.GetValue(obj, null), out seconds);
            }
            catch { }

            try
            {
                FieldInfo f = t.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (f != null)
                    return TryReadNumericAsSeconds(f.FieldType, () => f.GetValue(obj), out seconds);
            }
            catch { }

            return false;
        }

        private static bool TryReadNumericAsSeconds(Type type, Func<object> getter, out int seconds)
        {
            seconds = 0;

            try
            {
                object raw = getter();
                if (raw == null) return false;

                if (type == typeof(float)) { seconds = (int)Math.Ceiling((float)raw); return true; }
                if (type == typeof(double)) { seconds = (int)Math.Ceiling((double)raw); return true; }
                if (type == typeof(int)) { seconds = (int)raw; return true; }
                if (type == typeof(uint)) { seconds = unchecked((int)(uint)raw); return true; }
                if (type == typeof(short)) { seconds = (short)raw; return true; }
                if (type == typeof(long)) { long v = (long)raw; seconds = (int)Math.Min(v, int.MaxValue); return true; }
            }
            catch { }

            return false;
        }

        private static object GetStaticMember(Type t, string name)
        {
            try
            {
                PropertyInfo p = t.GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                if (p != null) return p.GetValue(null, null);
            }
            catch { }

            try
            {
                FieldInfo f = t.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                if (f != null) return f.GetValue(null);
            }
            catch { }

            return null;
        }
    }
}
