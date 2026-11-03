using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Monsters;

namespace BentleyPlayz.FastFoodUsefulStuff.Smapi
{
    /// <summary>
    /// Gives the Windex joke item a real attack. Holding Windex and pressing the use-tool button (left click, or a
    /// tap on mobile) sprays the area in front of the farmer and damages enemies through the game's own
    /// GameLocation.damageMonster, so damage, knockback, kill credit and drops follow the normal combat rules.
    /// Nothing here touches the farmer's health. Drinking Windex (the action button) is the unchanged vanilla path.
    /// </summary>
    internal sealed class WindexController
    {
        public const string WindexQualifiedId = "(O)" + ModEntry.ContentPackId + "_Windex";

        private readonly IModHelper helper;
        private readonly IMonitor monitor;
        private readonly Func<ModConfig> getConfig;
        private double lastSprayMs = double.MinValue;

        public WindexController(IModHelper helper, IMonitor monitor, Func<ModConfig> getConfig)
        {
            this.helper = helper;
            this.monitor = monitor;
            this.getConfig = getConfig;
        }

        public void OnButtonPressed(ButtonPressedEventArgs e)
        {
            if (!Context.IsWorldReady || !Context.IsPlayerFree)
                return;
            if (!e.Button.IsUseToolButton() || this.helper.Input.IsSuppressed(e.Button))
                return;

            Farmer who = Game1.player;
            if (who.CurrentItem?.QualifiedItemId != WindexQualifiedId)
                return;

            // Windex is "used": stop the game from also treating this press as something else (placing / eating prompts).
            this.helper.Input.Suppress(e.Button);

            ModConfig config = this.getConfig();
            double now = Game1.currentGameTime?.TotalGameTime.TotalMilliseconds ?? 0;
            if (now - this.lastSprayMs < config.WindexCooldownMs)
                return;
            this.lastSprayMs = now;

            this.Spray(who, config);
        }

        private void Spray(Farmer who, ModConfig config)
        {
            GameLocation? location = who.currentLocation;
            if (location == null)
                return;

            try
            {
                int min = Math.Max(1, Math.Min(config.WindexMinDamage, config.WindexMaxDamage));
                int max = Math.Max(min, config.WindexMaxDamage);
                Rectangle area = this.GetSprayArea(who, config.WindexRangeTiles);

                // Remember each enemy's health so we can tell, afterwards, which ones were really hurt.
                Dictionary<Monster, int> before = location.characters
                    .OfType<Monster>()
                    .Where(m => m.Health > 0)
                    .ToDictionary(m => m, m => m.Health);

                location.playSound("dwop");
                location.damageMonster(area, min, max, false, who);

                int hurt = before.Count(pair => pair.Key.Health < pair.Value);
                this.monitor.Log(
                    $"Windex sprayed in {location.NameOrUniqueName}: {hurt} of {before.Count} enemies lost health (damage {min}-{max}).",
                    LogLevel.Trace
                );

                if (hurt > 0)
                {
                    location.playSound("hitEnemy");
                    if (config.WindexConsumedOnHit)
                        who.reduceActiveItemByOne();
                }
            }
            catch (Exception ex)
            {
                // Never let a combat problem crash the game; just report it.
                this.monitor.Log($"Windex spray failed: {ex}", LogLevel.Error);
            }
        }

        /// <summary>The box in front of the farmer (pixels) that the spray covers.</summary>
        private Rectangle GetSprayArea(Farmer who, float rangeTiles)
        {
            Rectangle box = who.GetBoundingBox();
            int reach = (int)(Math.Max(0.5f, rangeTiles) * Game1.tileSize);
            const int side = 32;

            switch (who.FacingDirection)
            {
                case 0: // up
                    return new Rectangle(box.X - side, box.Y - reach, box.Width + side * 2, reach);
                case 1: // right
                    return new Rectangle(box.Right, box.Y - side, reach, box.Height + side * 2);
                case 2: // down
                    return new Rectangle(box.X - side, box.Bottom, box.Width + side * 2, reach);
                default: // left
                    return new Rectangle(box.X - reach, box.Y - side, reach, box.Height + side * 2);
            }
        }
    }
}
