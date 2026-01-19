using System;
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
        public override string Description { get; } = "Spectator respawn timer overlay (Ruel-first, SendHint fallback).";
        public override string Author { get; } = "OceLuke";
        public override Version Version { get; } = new Version(1, 0, 4, 0);
        public override Version RequiredApiVersion { get; } = new Version(LabApiProperties.CompiledVersion);

        private Timer _timer;

        private const int UpdateIntervalMs = 1000;
        private const string RuelChannelId = "luke_respawn_timer";

        public override void Enable()
        {
            _timer = new Timer(Tick, null, 0, UpdateIntervalMs);
        }

        public override void Disable()
        {
            if (_timer != null)
            {
                _timer.Dispose();
                _timer = null;
            }

            RuelBridge.TryHideAll(RuelChannelId);
        }

        private void Tick(object state)
        {
            int seconds = RespawnTimeReader.GetSecondsToNextRespawn();
            if (seconds < 0) seconds = 0;

            string text = "<align=\"center\"><size=18><b>RESPAWNING IN " + FormatMmSs(seconds) + "</b></size></align>";

            foreach (Player p in Player.List)
            {
                if (p == null || p.ReferenceHub == null)
                    continue;

                RoleTypeId role = p.ReferenceHub.roleManager.CurrentRole.RoleTypeId;
                if (role != RoleTypeId.Spectator)
                    continue;

                // Prefer Ruel so we don't clobber other vanilla hints
                if (!RuelBridge.TryShow(p, RuelChannelId, text, 1.2f))
                {
                    // Fallback (will overwrite other SendHint calls)
                    p.SendHint(text, 1.2f);
                }
            }
        }

        private static string FormatMmSs(int totalSeconds)
        {
            int minutes = totalSeconds / 60;
            int seconds = totalSeconds % 60;
            return string.Format("{0:00}:{1:00}", minutes, seconds);
        }
    }
}
