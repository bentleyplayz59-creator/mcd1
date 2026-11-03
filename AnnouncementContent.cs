using System.Collections.Generic;

namespace BentleyPlayz.FastFoodUsefulStuff.Smapi
{
    /// <summary>The announcement text. Change the text and bump ModEntry.AnnouncementVersion for a future update.</summary>
    internal static class AnnouncementContent
    {
        internal readonly struct Block
        {
            public bool Heading { get; }
            public string Text { get; }

            public Block(bool heading, string text)
            {
                this.Heading = heading;
                this.Text = text;
            }
        }

        public static IEnumerable<Block> GetBlocks(string farmerName)
        {
            // Note: plain ASCII punctuation only; Stardew's fonts do not all include the long dash.
            yield return new Block(true, "Bentley Playz Fast Food - New Update Available!");
            yield return new Block(true, $"Welcome, {farmerName}!");
            yield return new Block(false, "Thank you for downloading the latest version of Bentley Playz Fast Food & Useful Stuff!");
            yield return new Block(false, "We're excited to introduce the newest improvements to the restaurant. This update focuses on making the restaurant experience more polished, functional, and enjoyable.");

            yield return new Block(true, "What's new in this update?");

            yield return new Block(true, "Improved Interior Lighting");
            yield return new Block(false, "The restaurant interior has received lighting improvements to make the environment more comfortable and visually consistent.");

            yield return new Block(true, "Improved Restaurant Entrance");
            yield return new Block(false, "The restaurant's existing entrance system has been improved and transferred into a dedicated SMAPI component.");

            yield return new Block(true, "Preserved Door Locking System");
            yield return new Block(false, "The original entrance restrictions and locking behavior have been preserved while moving their functionality into the DLL.");

            yield return new Block(true, "Bedroom Access");
            yield return new Block(false, "Bedroom access continues to follow the existing progression requirement: you need three hearts with Bentley before the staff door opens.");

            yield return new Block(true, "Windex Improvements");
            yield return new Block(false, "Windex has received its intended damage functionality through the game's combat system.");

            yield return new Block(true, "Special Thanks");
            yield return new Block(false, "A special thank-you to Nick and Ed for their support throughout this project's development.");
            yield return new Block(false, "We would also like to thank everyone in the Stardew Valley community who has downloaded the mod, shared feedback, and supported its continued development.");
            yield return new Block(false, "We genuinely appreciate your support.");
            yield return new Block(false, "Thank you for being part of Bentley Playz Fast Food & Useful Stuff!");
            yield return new Block(true, "Enjoy the update!");
            yield return new Block(false, "Creator: Bentley Playz");
        }
    }
}
