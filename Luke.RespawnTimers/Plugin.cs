using LabApi.Events.Handlers;
using LabApi.Features;
using LabApi.Features.Wrappers;
using Respawning;
using RueI;
using RueI.API;
using RueI.API.Elements;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Luke.RespawnTimers
{
    public class Plugin : LabApi.Features.Plugin
    {
        public override string Name => "Luke.RespawnTimers";
        public override string Description => "Shows spectators a mm:ss timer until the next respawn wave.";
        public override string Author => "OceLuke";
        public override Version Version => new Version(1, 0, 0);
        public override Version RequiredApiVersion => new Version(14, 2, 5);

        // One tag per player so we can update the same line cleanly
        private readonly Dictionary<int, Tag> _tagsByPlayerId = new Dictionary<int, Tag>();

        private CancellationTokenSource _cts;
        private Task _loopTask;

        public override void Enable()
        {
            // Hook events
            PlayerEvents.Left += OnPlayerLeft;

            // Start update loop
            _cts = new CancellationTokenSource();
            _loopTask = Task.Run(() => Loop(_cts.Token));

            base.Enable();
        }

        public override void Disable()
        {
            PlayerEvents.Left -= OnPlayerLeft;

            try
            {
                _cts?.Cancel();
            }
            catch { /* ignored */ }

            _tagsByPlayerId.Clear();

            base.Disable();
        }

        private void OnPlayerLeft(LabApi.Events.Arguments.PlayerEvents.PlayerLeftEventArgs ev)
        {
            // Clean up their tag so we don’t leak memory
            if (ev?.Player != null)
                _tagsByPlayerId.Remove(ev.Player.PlayerId);
        }

        private async Task Loop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    // TimeTillRespawn is in seconds (int)
                    int seconds = RespawnManager.Singleton != null
                        ? RespawnManager.Singleton.TimeTillRespawn
                        : 0;

                    if (seconds < 0) seconds = 0;

                    TimeSpan ts = TimeSpan.FromSeconds(seconds);
                    string timeText = string.Format("{0:00}:{1:00}", (int)ts.TotalMinutes, ts.Seconds);

                    // Match the “Respawning in: 00:00” style
                    string text = "<b><color=#DADADA>Respawning in:</color> <color=#00FF6A>" + timeText + "</color></b>";

                    foreach (Player p in Player.List)
                    {
                        if (p == null) continue;

                        // ONLY spectators
                        if (!p.IsSpectator)
                            continue;

                        RueDisplay display = RueDisplay.Get(p);

                        Tag tag;
                        if (!_tagsByPlayerId.TryGetValue(p.PlayerId, out tag))
                        {
                            tag = new Tag();
                            _tagsByPlayerId[p.PlayerId] = tag;
                        }

                        // Update the same tag every tick (no Hide() needed)
                        // Priority 800 is similar to RueI examples; adjust if you want it higher/lower.
                        display.Show(tag, new BasicElement(800, text));
                    }
                }
                catch
                {
                    // swallow exceptions to keep the loop alive
                }

                // Update rate (4x per second). You can change to 0.5s if you want.
                try { await Task.Delay(250, token); } catch { }
            }
        }
    }
}
