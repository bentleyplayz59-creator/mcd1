using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace BentleyPlayz.FastFoodUsefulStuff.Smapi
{
    /// <summary>
    /// Decides whether the player may use the restaurant's two player doors, and performs the entry.
    /// This is the same behaviour the 3.7.3 Content Patcher pack implemented with map patches; the CP patches
    /// for these two doors were removed in 3.8.0 so this class is the only implementation.
    ///
    /// Scope: the front door (Forest 68-69,26 on the normal and both Ice Festival maps) and the staff door
    /// (restaurant 16-17,3). Exits (restaurant -> Forest, bedroom -> restaurant) and Bentley's own NPC warps stay
    /// as map warps in the Content Patcher pack: they were never restricted, and NPC pathfinding needs real map warps.
    /// </summary>
    internal sealed class DoorController
    {
        private const string MainEntrance = "MainEntrance";
        private const string BedroomEntrance = "BedroomEntrance";

        private readonly IModHelper helper;
        private readonly IMonitor monitor;
        private readonly List<DoorTile> doorTiles = new();
        private readonly Func<bool> walkIntoDoors;

        /// <summary>The door the player was standing in last tick (so a door only fires once per visit to its tiles).</summary>
        private string? lastDoorId;

        public DoorController(IModHelper helper, IMonitor monitor, Func<bool> walkIntoDoors)
        {
            this.walkIntoDoors = walkIntoDoors;
            this.helper = helper;
            this.monitor = monitor;

            string restaurant = ModEntry.RestaurantLocation;
            string bedroom = ModEntry.BedroomLocation;

            // Front door: both door-step tiles warp to restaurant 9,12 (as in 3.7.3 forest_restaurant.json).
            this.doorTiles.Add(new DoorTile(MainEntrance, new Point(68, 26), restaurant, new Point(9, 12), IsForestMap));
            this.doorTiles.Add(new DoorTile(MainEntrance, new Point(69, 26), restaurant, new Point(9, 12), IsForestMap));

            // Staff door: 16,3 -> bedroom 6,9 and 17,3 -> bedroom 7,9 (as in 3.7.3 locations.json).
            this.doorTiles.Add(new DoorTile(BedroomEntrance, new Point(16, 3), bedroom, new Point(6, 9), loc => loc.Name == restaurant));
            this.doorTiles.Add(new DoorTile(BedroomEntrance, new Point(17, 3), bedroom, new Point(7, 9), loc => loc.Name == restaurant));
        }

        /// <summary>Forget per-visit state (call when returning to the title screen).</summary>
        public void Reset()
        {
            this.lastDoorId = null;
        }

        /// <summary>Walking into a door: runs once when the player steps onto a door tile.</summary>
        public void OnUpdateTicked()
        {
            if (!this.walkIntoDoors())
            {
                this.lastDoorId = null; // walking onto a door does nothing; only click / tap / action button open it
                return;
            }

            if (!Context.IsWorldReady || !Context.IsPlayerFree)
                return;

            Farmer who = Game1.player;
            GameLocation? location = who.currentLocation;
            if (location == null)
            {
                this.lastDoorId = null;
                return;
            }

            DoorTile? door = this.FindDoorTile(location, who.TilePoint);
            string? doorId = door?.DoorId;

            if (doorId == this.lastDoorId)
                return;

            this.lastDoorId = doorId;
            if (door != null)
                this.UseDoor(door, who);
        }

        /// <summary>
        /// Interacting with a door through the game's normal input: the action button (E by default, honours custom
        /// bindings, controller A) and the use-tool button (mouse click / mobile touch tap) on a door tile within reach.
        /// </summary>
        public void OnButtonPressed(ButtonPressedEventArgs e)
        {
            if (!Context.IsWorldReady || !Context.IsPlayerFree)
                return;
            if (!e.Button.IsActionButton() && !e.Button.IsUseToolButton())
                return;

            Farmer who = Game1.player;
            GameLocation? location = who.currentLocation;
            if (location == null)
                return;

            Point target = e.Cursor.GrabTile.ToPoint();
            DoorTile? door = this.FindDoorTile(location, target);
            if (door == null)
                return;

            Point me = who.TilePoint;
            if (Math.Abs(me.X - target.X) > 2 || Math.Abs(me.Y - target.Y) > 2)
                return;

            this.helper.Input.Suppress(e.Button);
            this.lastDoorId = door.DoorId; // standing on the door tiles must not fire a second time
            this.UseDoor(door, who);
        }

        /// <summary>Whether the given door is currently open for the given farmer.</summary>
        private bool IsOpen(string doorId, Farmer who)
        {
            switch (doorId)
            {
                case MainEntrance:
                    return DoorRules.IsRestaurantOpen(Game1.timeOfDay, who.mailReceived.Contains(DoorRules.TownPassFlag));

                case BedroomEntrance:
                    return DoorRules.IsBedroomOpen(who.getFriendshipHeartLevelForNPC(ModEntry.BentleyNpcName));

                default:
                    return false; // unknown door: stay locked
            }
        }

        private void UseDoor(DoorTile door, Farmer who)
        {
            try
            {
                if (!this.IsOpen(door.DoorId, who))
                {
                    string message = door.DoorId == MainEntrance ? DoorRules.RestaurantClosedMessage : DoorRules.BedroomLockedMessage;
                    Game1.drawObjectDialogue(message);
                    return;
                }

                if (Game1.getLocationFromName(door.TargetLocation) == null)
                {
                    this.monitor.Log($"Door '{door.DoorId}' could not open: location '{door.TargetLocation}' does not exist. Is the Content Patcher pack installed?", LogLevel.Error);
                    Game1.drawObjectDialogue("The door will not open.");
                    return;
                }

                this.monitor.Log($"Door '{door.DoorId}' open; warping to {door.TargetLocation} ({door.TargetTile.X}, {door.TargetTile.Y}).", LogLevel.Trace);
                Game1.warpFarmer(door.TargetLocation, door.TargetTile.X, door.TargetTile.Y, who.FacingDirection);
            }
            catch (Exception ex)
            {
                this.monitor.Log($"Door '{door.DoorId}' failed: {ex}", LogLevel.Error);
            }
        }

        private DoorTile? FindDoorTile(GameLocation location, Point tile)
        {
            foreach (DoorTile door in this.doorTiles)
            {
                if (door.Tile == tile && door.AppliesTo(location))
                    return door;
            }
            return null;
        }

        /// <summary>The three Forest maps the restaurant is patched into (normal, Ice Festival, Ice Festival 2).</summary>
        private static bool IsForestMap(GameLocation location)
        {
            string? path = location.mapPath.Value;
            if (string.IsNullOrEmpty(path))
                return false;

            string file = Path.GetFileName(path.Replace('\\', '/'));
            return file == "Forest" || file == "Forest-IceFestival" || file == "Forest-IceFestival2";
        }

        private sealed class DoorTile
        {
            public string DoorId { get; }
            public Point Tile { get; }
            public string TargetLocation { get; }
            public Point TargetTile { get; }
            public Func<GameLocation, bool> AppliesTo { get; }

            public DoorTile(string doorId, Point tile, string targetLocation, Point targetTile, Func<GameLocation, bool> appliesTo)
            {
                this.DoorId = doorId;
                this.Tile = tile;
                this.TargetLocation = targetLocation;
                this.TargetTile = targetTile;
                this.AppliesTo = appliesTo;
            }
        }
    }
}
