namespace BentleyPlayz.FastFoodUsefulStuff.Smapi
{
    /// <summary>Settings for the SMAPI component (config.json, created on first run). The Content Patcher pack keeps its own config.</summary>
    internal sealed class ModConfig
    {
        /// <summary>
        /// Walking onto a door tile enters it automatically. Off by default: doors open only when you click/tap
        /// them or press the action button. Set to true to bring the old walk-in behaviour back.
        /// </summary>
        public bool WalkIntoDoors { get; set; } = false;

        /// <summary>Lowest damage one Windex spray can deal to each enemy it hits.</summary>
        public int WindexMinDamage { get; set; } = 10;

        /// <summary>Highest damage one Windex spray can deal to each enemy it hits.</summary>
        public int WindexMaxDamage { get; set; } = 18;

        /// <summary>How far in front of the farmer the spray reaches, in tiles.</summary>
        public float WindexRangeTiles { get; set; } = 2f;

        /// <summary>Minimum time between sprays, in milliseconds.</summary>
        public int WindexCooldownMs { get; set; } = 700;

        /// <summary>Use up one Windex each time a spray hits at least one enemy (sprays that miss are free).</summary>
        public bool WindexConsumedOnHit { get; set; } = true;
    }
}
