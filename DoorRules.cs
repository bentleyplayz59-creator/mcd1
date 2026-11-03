namespace BentleyPlayz.FastFoodUsefulStuff.Smapi
{
    /// <summary>
    /// The door rules, copied one-for-one from the 3.7.3 Content Patcher pack (content.json dynamic tokens
    /// RestaurantDoorOpen / BedroomDoorOpen). Pure functions with no game types so they are easy to test.
    /// </summary>
    internal static class DoorRules
    {
        /// <summary>Mail flag set by buying the Town Pass (3.7.3: HasFlag {{ModId}}_TownPassOwned).</summary>
        public const string TownPassFlag = ModEntry.ContentPackId + "_TownPassOwned";

        /// <summary>Friendship hearts needed for the bedroom (3.7.3: Hearts:{{ModId}}_Bentley in range 3-14).</summary>
        public const int BedroomHeartsRequired = 3;

        /// <summary>Message shown at the closed front door (3.7.3 text, unchanged).</summary>
        public const string RestaurantClosedMessage = "Bentley's Fast Food is closed right now. Open every day from 9:00 AM to 9:00 PM.";

        /// <summary>Message shown at the locked staff door (3.7.3 text, unchanged).</summary>
        public const string BedroomLockedMessage = "The door is locked. Bentley's bedroom is private. Maybe he'd let you in if you were better friends (3 hearts).";

        /// <summary>
        /// Front door. 3.7.3: open by default; closed 6:00-8:59 and 20:50 onward (Time ranges 600-859 and 2050-2600);
        /// the Town Pass flag keeps it open at all times.
        /// </summary>
        /// <param name="timeOfDay">Game time as 600-2600 (e.g. 2050 = 8:50 PM).</param>
        /// <param name="hasTownPass">Whether the player owns the Town Pass flag.</param>
        public static bool IsRestaurantOpen(int timeOfDay, bool hasTownPass)
        {
            if (hasTownPass)
                return true;

            bool closed = (timeOfDay >= 600 && timeOfDay <= 859) || (timeOfDay >= 2050 && timeOfDay <= 2600);
            return !closed;
        }

        /// <summary>Staff door to the bedroom. 3.7.3: open only at 3 or more hearts with Bentley.</summary>
        /// <param name="heartLevel">Whole hearts (friendship points / 250).</param>
        public static bool IsBedroomOpen(int heartLevel)
        {
            return heartLevel >= BedroomHeartsRequired;
        }
    }
}
