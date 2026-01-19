namespace Luke.RespawnTimers
{
    public sealed class Config
    {
        public bool Enabled { get; set; } = true;

        // Spectators only (you asked for this)
        public bool SpectatorsOnly { get; set; } = true;

        // Update every second
        public float UpdateIntervalSeconds { get; set; } = 1f;

        // RueI show duration (we refresh it every tick anyway)
        public float HintDurationSeconds { get; set; } = 2f;

        // Hint text (SCP:SL rich text supported)
        public string Format { get; set; } =
            "<align=\"center\"><size=24><b>Respawn in {time}</b></size></align>";
    }
}
