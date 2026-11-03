using System;
using System.Collections.Generic;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace BentleyPlayz.FastFoodUsefulStuff.Smapi
{
    /// <summary>
    /// SMAPI component of "Bentley Playz Fast Food &amp; Useful Stuff". It owns: the restaurant's player door rules,
    /// the Windex attack, and the first-time update announcement. Maps, items, shops, NPCs and art stay in the
    /// Content Patcher pack (UniqueID BentleyPlayz.FastFoodUsefulStuff).
    /// </summary>
    internal sealed class ModEntry : Mod
    {
        /// <summary>UniqueID of the Content Patcher pack. Its {{ModId}} prefixes every item, location and NPC id.</summary>
        public const string ContentPackId = "BentleyPlayz.FastFoodUsefulStuff";

        public const string RestaurantLocation = ContentPackId + "_BentleysRestaurant";
        public const string BedroomLocation = ContentPackId + "_BentleysBedroom";
        public const string BentleyNpcName = ContentPackId + "_Bentley";

        /// <summary>
        /// Identifier of the announcement. Change this (and the text in AnnouncementContent) for a future update
        /// and everyone sees the new announcement exactly once.
        /// </summary>
        public const string AnnouncementVersion = "3.8.1";

        private const string StateFile = "announcement-state";

        private ModConfig config = new();
        private DoorController doors = null!;
        private WindexController windex = null!;
        private AnnouncementState state = new();
        private bool announcementPending;

        public override void Entry(IModHelper helper)
        {
            this.config = helper.ReadConfig<ModConfig>();
            this.doors = new DoorController(helper, this.Monitor, () => this.config.WalkIntoDoors);
            this.windex = new WindexController(helper, this.Monitor, () => this.config);

            helper.Events.GameLoop.GameLaunched += this.OnGameLaunched;
            helper.Events.GameLoop.SaveLoaded += this.OnSaveLoaded;
            helper.Events.GameLoop.ReturnedToTitle += this.OnReturnedToTitle;
            helper.Events.GameLoop.UpdateTicked += this.OnUpdateTicked;
            helper.Events.Input.ButtonPressed += this.OnButtonPressed;

            helper.ConsoleCommands.Add(
                "bentley_announcement",
                "Bentley Playz: 'bentley_announcement show' opens the update announcement now; 'bentley_announcement reset' makes it appear again next time this save is loaded.",
                this.OnAnnouncementCommand
            );
        }

        private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
        {
            if (!this.Helper.ModRegistry.IsLoaded(ContentPackId))
                this.Monitor.Log($"The Content Patcher pack '{ContentPackId}' is not loaded. The doors, Windex and announcement need it; install both folders.", LogLevel.Warn);
        }

        private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
        {
            this.doors.Reset();
            this.state = this.Helper.Data.ReadGlobalData<AnnouncementState>(StateFile) ?? new AnnouncementState();
            this.announcementPending = !this.HasSeenAnnouncement();
            this.Monitor.Log(this.announcementPending ? "Update announcement will be shown once the world is ready." : "Update announcement already seen for this save.", LogLevel.Trace);
        }

        private void OnReturnedToTitle(object? sender, ReturnedToTitleEventArgs e)
        {
            this.announcementPending = false;
            this.doors.Reset();
        }

        private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
        {
            this.doors.OnUpdateTicked();

            if (this.announcementPending && e.IsMultipleOf(30))
                this.TryShowAnnouncement();
        }

        private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
        {
            // Doors first: a click on a door tile must not also swing a tool / spray Windex.
            this.doors.OnButtonPressed(e);
            this.windex.OnButtonPressed(e);
        }

        // ---------------------------------------------------------------- announcement

        private void TryShowAnnouncement()
        {
            // Wait until the save is fully playable and nothing else (menu, event, festival, cutscene) is using the screen.
            if (!Context.IsWorldReady || !Context.IsPlayerFree)
                return;
            if (Game1.activeClickableMenu != null || Game1.CurrentEvent != null || Game1.isFestival())
                return;

            this.announcementPending = false;
            this.ShowAnnouncement();
        }

        private void ShowAnnouncement()
        {
            string saveKey = this.GetSaveKey();
            Game1.activeClickableMenu = new AnnouncementMenu(Game1.player.Name, () => this.MarkAnnouncementSeen(saveKey));
        }

        private bool HasSeenAnnouncement()
        {
            return this.state.SeenBySave.TryGetValue(this.GetSaveKey(), out string? seen) && seen == AnnouncementVersion;
        }

        private void MarkAnnouncementSeen(string saveKey)
        {
            // Written to disk immediately (not at the next in-game save), so quitting without sleeping cannot bring it back.
            this.state.SeenBySave[saveKey] = AnnouncementVersion;
            this.Helper.Data.WriteGlobalData(StateFile, this.state);
            this.Monitor.Log($"Announcement {AnnouncementVersion} dismissed for '{saveKey}'.", LogLevel.Trace);
        }

        /// <summary>One entry per save slot and per player, so multiplayer farmhands each see it once.</summary>
        private string GetSaveKey()
        {
            return $"{Constants.SaveFolderName}/{Game1.player.UniqueMultiplayerID}";
        }

        private void OnAnnouncementCommand(string command, string[] args)
        {
            if (!Context.IsWorldReady)
            {
                this.Monitor.Log("Load a save first.", LogLevel.Warn);
                return;
            }

            string action = args.Length > 0 ? args[0].ToLowerInvariant() : "show";
            if (action == "reset")
            {
                this.state.SeenBySave.Remove(this.GetSaveKey());
                this.Helper.Data.WriteGlobalData(StateFile, this.state);
                this.Monitor.Log("Announcement reset: it will appear the next time this save is loaded.", LogLevel.Info);
            }
            else if (action == "show")
            {
                this.ShowAnnouncement();
            }
            else
            {
                this.Monitor.Log("Usage: bentley_announcement show | reset", LogLevel.Info);
            }
        }

        /// <summary>Which announcement version each save/player has already dismissed.</summary>
        private sealed class AnnouncementState
        {
            public Dictionary<string, string> SeenBySave { get; set; } = new();
        }
    }
}
