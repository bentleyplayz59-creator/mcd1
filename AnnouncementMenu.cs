using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewValley;
using StardewValley.BellsAndWhistles;
using StardewValley.Menus;

namespace BentleyPlayz.FastFoodUsefulStuff.Smapi
{
    /// <summary>
    /// The first-time update announcement. A normal Stardew menu (IClickableMenu), so it works with mouse,
    /// keyboard, controller and the mobile touch UI. Scrolls with the mouse wheel, the arrow buttons, the arrow
    /// keys, the controller stick/d-pad, or by dragging with a finger. Only the Continue button (or Enter / Space /
    /// the action button / controller A) closes it.
    /// </summary>
    internal sealed class AnnouncementMenu : IClickableMenu
    {
        private readonly string farmerName;
        private readonly Action onContinue;
        private readonly List<Texture2D> brandIcons = new();

        private readonly List<DrawLine> lines = new();
        private Rectangle textArea;
        private ClickableComponent continueButton = null!;
        private ClickableTextureComponent upButton = null!;
        private ClickableTextureComponent downButton = null!;

        private int scrollY;
        private int maxScroll;
        private bool continueHover;
        private bool dragging;
        private int dragLastY;
        private bool dismissed;

        public AnnouncementMenu(string farmerName, Action onContinue)
            : base(0, 0, 0, 0, showUpperRightCloseButton: false)
        {
            this.farmerName = string.IsNullOrWhiteSpace(farmerName) ? "Farmer" : farmerName;
            this.onContinue = onContinue;

            this.LoadBrandIcons();
            this.Layout();

            if (Game1.options.SnappyMenus)
            {
                this.populateClickableComponentList();
                this.snapToDefaultClickableComponent();
            }
        }

        public override bool readyToClose()
        {
            return false; // the player must press Continue
        }

        public override void snapToDefaultClickableComponent()
        {
            this.currentlySnappedComponent = this.continueButton;
            this.snapCursorToCurrentSnappedComponent();
        }

        public override void gameWindowSizeChanged(Rectangle oldBounds, Rectangle newBounds)
        {
            base.gameWindowSizeChanged(oldBounds, newBounds);
            int keep = this.scrollY;
            this.Layout();
            this.scrollY = Math.Min(keep, this.maxScroll);
        }

        public override void receiveLeftClick(int x, int y, bool playSound = true)
        {
            if (this.continueButton.containsPoint(x, y))
            {
                this.Dismiss();
                return;
            }

            if (this.upButton.containsPoint(x, y))
            {
                this.Scroll(-this.LineStep() * 3);
                Game1.playSound("shiny4");
                return;
            }

            if (this.downButton.containsPoint(x, y))
            {
                this.Scroll(this.LineStep() * 3);
                Game1.playSound("shiny4");
                return;
            }

            if (this.textArea.Contains(x, y))
            {
                this.dragging = true;
                this.dragLastY = y;
            }
        }

        public override void leftClickHeld(int x, int y)
        {
            if (!this.dragging)
                return;

            this.Scroll(this.dragLastY - y);
            this.dragLastY = y;
        }

        public override void releaseLeftClick(int x, int y)
        {
            this.dragging = false;
        }

        public override void receiveRightClick(int x, int y, bool playSound = true)
        {
        }

        public override void receiveScrollWheelAction(int direction)
        {
            this.Scroll(direction > 0 ? -this.LineStep() * 2 : this.LineStep() * 2);
        }

        public override void performHoverAction(int x, int y)
        {
            this.continueHover = this.continueButton.containsPoint(x, y);
        }

        public override void receiveKeyPress(Keys key)
        {
            if (key == Keys.Enter || key == Keys.Space || Game1.options.doesInputListContain(Game1.options.actionButton, key))
            {
                this.Dismiss();
                return;
            }

            if (key == Keys.Up || Game1.options.doesInputListContain(Game1.options.moveUpButton, key))
                this.Scroll(-this.LineStep());
            else if (key == Keys.Down || Game1.options.doesInputListContain(Game1.options.moveDownButton, key))
                this.Scroll(this.LineStep());
            else if (key == Keys.PageUp)
                this.Scroll(-this.textArea.Height);
            else if (key == Keys.PageDown)
                this.Scroll(this.textArea.Height);

            // Escape / menu button deliberately do nothing: use Continue.
        }

        public override void receiveGamePadButton(Buttons b)
        {
            if (b == Buttons.A)
                this.Dismiss();
            else if (b == Buttons.DPadUp || b == Buttons.LeftThumbstickUp)
                this.Scroll(-this.LineStep());
            else if (b == Buttons.DPadDown || b == Buttons.LeftThumbstickDown)
                this.Scroll(this.LineStep());
        }

        public override void draw(SpriteBatch b)
        {
            // dim the world behind the window
            b.Draw(Game1.fadeToBlackRect, new Rectangle(0, 0, Game1.uiViewport.Width, Game1.uiViewport.Height), Color.Black * 0.65f);

            // the Stardew-style panel
            Game1.drawDialogueBox(this.xPositionOnScreen, this.yPositionOnScreen, this.width, this.height, false, true);

            this.DrawBranding(b);
            this.DrawText(b);
            this.DrawScrollControls(b);
            this.DrawContinue(b);

            this.drawMouse(b);
        }

        // ---------------------------------------------------------------- layout

        private void Layout()
        {
            int viewW = Game1.uiViewport.Width;
            int viewH = Game1.uiViewport.Height;

            this.width = Math.Min(viewW - 32, 960);
            this.height = Math.Min(viewH - 32, 720);
            this.xPositionOnScreen = (viewW - this.width) / 2;
            this.yPositionOnScreen = (viewH - this.height) / 2;

            int headerH = 112;
            int footerH = 104;
            int sidePad = 56;
            int barW = 48;

            this.textArea = new Rectangle(
                this.xPositionOnScreen + sidePad,
                this.yPositionOnScreen + headerH,
                this.width - sidePad * 2 - barW,
                Math.Max(120, this.height - headerH - footerH)
            );

            this.continueButton = new ClickableComponent(
                new Rectangle(this.xPositionOnScreen + this.width / 2 - 130, this.yPositionOnScreen + this.height - footerH + 12, 260, 72),
                "Continue"
            )
            {
                myID = 1000
            };

            this.upButton = new ClickableTextureComponent(
                new Rectangle(this.textArea.Right + 6, this.textArea.Y, 44, 48),
                Game1.mouseCursors,
                new Rectangle(421, 459, 11, 12),
                4f
            );
            this.downButton = new ClickableTextureComponent(
                new Rectangle(this.textArea.Right + 6, this.textArea.Bottom - 48, 44, 48),
                Game1.mouseCursors,
                new Rectangle(421, 472, 11, 12),
                4f
            );

            this.BuildLines();
        }

        private void BuildLines()
        {
            this.lines.Clear();
            int y = 0;

            foreach (AnnouncementContent.Block block in AnnouncementContent.GetBlocks(this.farmerName))
            {
                SpriteFont font = block.Heading ? Game1.dialogueFont : Game1.smallFont;
                Color color = block.Heading ? new Color(120, 60, 20) : Game1.textColor;
                float lineH = font.MeasureString("Ag").Y + (block.Heading ? 4 : 2);

                if (block.Heading && y > 0)
                    y += 10;

                string wrapped = Game1.parseText(block.Text, font, this.textArea.Width);
                foreach (string line in wrapped.Split('\n'))
                {
                    this.lines.Add(new DrawLine(line, font, color, y, (int)lineH));
                    y += (int)lineH;
                }

                y += block.Heading ? 4 : 12;
            }

            this.maxScroll = Math.Max(0, y - this.textArea.Height);
            this.scrollY = Math.Min(this.scrollY, this.maxScroll);
        }

        private void LoadBrandIcons()
        {
            // Item sprites loaded by the Content Patcher pack. If one is missing the window simply has fewer icons.
            foreach (string name in new[] { "Burger", "Fries", "Coke", "Windex", "SnackPack" })
            {
                try
                {
                    this.brandIcons.Add(Game1.content.Load<Texture2D>($"Mods/{ModEntry.ContentPackId}/{name}"));
                }
                catch
                {
                    // ignore: decoration only
                }
            }
        }

        // ---------------------------------------------------------------- drawing

        private void DrawBranding(SpriteBatch b)
        {
            int centerX = this.xPositionOnScreen + this.width / 2;
            int top = this.yPositionOnScreen + 36;

            SpriteText.drawStringHorizontallyCenteredAt(b, "BENTLEY PLAYZ", centerX, top);

            int iconSize = 16 * 3;
            int count = this.brandIcons.Count;
            if (count > 0)
            {
                int total = count * iconSize + (count - 1) * 8;
                int ix = centerX - total / 2;
                int iy = top + 44;
                foreach (Texture2D icon in this.brandIcons)
                {
                    b.Draw(icon, new Vector2(ix, iy), new Rectangle(0, 0, 16, 16), Color.White, 0f, Vector2.Zero, 3f, SpriteEffects.None, 0.9f);
                    ix += iconSize + 8;
                }
            }
        }

        private void DrawText(SpriteBatch b)
        {
            foreach (DrawLine line in this.lines)
            {
                int top = line.Y - this.scrollY;
                if (top < 0 || top + line.Height > this.textArea.Height)
                    continue; // only draw lines that fit completely inside the window

                if (line.Text.Length == 0)
                    continue;

                Utility.drawTextWithShadow(
                    b,
                    line.Text,
                    line.Font,
                    new Vector2(this.textArea.X, this.textArea.Y + top),
                    line.Color
                );
            }
        }

        private void DrawScrollControls(SpriteBatch b)
        {
            if (this.maxScroll <= 0)
                return;

            this.upButton.draw(b, this.scrollY > 0 ? Color.White : Color.White * 0.4f, 0.9f);
            this.downButton.draw(b, this.scrollY < this.maxScroll ? Color.White : Color.White * 0.4f, 0.9f);

            // thumb
            int trackTop = this.upButton.bounds.Bottom + 6;
            int trackBottom = this.downButton.bounds.Y - 6;
            int trackH = Math.Max(10, trackBottom - trackTop);
            int thumbH = Math.Max(24, trackH * this.textArea.Height / (this.textArea.Height + this.maxScroll));
            int thumbY = trackTop + (trackH - thumbH) * this.scrollY / Math.Max(1, this.maxScroll);
            int thumbX = this.upButton.bounds.X + this.upButton.bounds.Width / 2 - 4;

            b.Draw(Game1.staminaRect, new Rectangle(thumbX, trackTop, 8, trackH), new Color(120, 60, 20) * 0.25f);
            b.Draw(Game1.staminaRect, new Rectangle(thumbX, thumbY, 8, thumbH), new Color(120, 60, 20));
        }

        private void DrawContinue(SpriteBatch b)
        {
            Rectangle r = this.continueButton.bounds;
            IClickableMenu.drawTextureBox(
                b,
                Game1.menuTexture,
                new Rectangle(0, 256, 60, 60),
                r.X, r.Y, r.Width, r.Height,
                this.continueHover ? Color.Wheat : Color.White,
                4f,
                false
            );

            const string label = "Continue";
            Vector2 size = Game1.dialogueFont.MeasureString(label);
            Utility.drawTextWithShadow(
                b,
                label,
                Game1.dialogueFont,
                new Vector2(r.Center.X - size.X / 2f, r.Center.Y - size.Y / 2f + 2),
                Game1.textColor
            );
        }

        // ---------------------------------------------------------------- helpers

        private int LineStep()
        {
            return (int)(Game1.smallFont.MeasureString("Ag").Y + 2);
        }

        private void Scroll(int delta)
        {
            this.scrollY = Math.Max(0, Math.Min(this.maxScroll, this.scrollY + delta));
        }

        private void Dismiss()
        {
            if (this.dismissed)
                return;

            this.dismissed = true;
            Game1.playSound("bigDeSelect");
            this.exitThisMenu(playSound: false);
            this.onContinue();
        }

        private readonly struct DrawLine
        {
            public string Text { get; }
            public SpriteFont Font { get; }
            public Color Color { get; }
            public int Y { get; }
            public int Height { get; }

            public DrawLine(string text, SpriteFont font, Color color, int y, int height)
            {
                this.Text = text;
                this.Font = font;
                this.Color = color;
                this.Y = y;
                this.Height = height;
            }
        }
    }
}
