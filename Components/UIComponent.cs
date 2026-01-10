using Godot;
using Breakout.Game;

namespace Breakout.Components
{
    /// <summary>
    /// UIComponent — manages HUD display (score, lives labels).
    /// 
    /// Following Nystrom's Component pattern:
    /// - Component owns the UI rendering responsibility (HUD overlay)
    /// - Listens to GameStateComponent events and updates display
    /// - Responsibility: Display score and lives; react to state changes via events
    /// 
    /// This is a focused UI component for HUD labels only.
    /// </summary>
    public partial class UIComponent : CanvasLayer
    {
        #region UI Elements
        private Label scoreLabel;
        private Label livesLabel;
        private Label gameOverLabel;
        private Tween scoreFlashTween;
        #endregion

        #region Lifecycle
        public override void _Ready()
        {
            // Create bold sans-serif SystemFont for arcade-style text
            var arcadeFont = new SystemFont();
            arcadeFont.FontNames = new[] { "Arial", "Helvetica", "sans-serif" };
            arcadeFont.FontWeight = 700;  // Bold

            // Create score label (top-left with 15px padding)
            scoreLabel = new Label();
            scoreLabel.AnchorLeft = 0;
            scoreLabel.AnchorRight = 0;
            scoreLabel.AnchorTop = 0;
            scoreLabel.AnchorBottom = 0;
            scoreLabel.OffsetLeft = 15;   // 15px padding from left wall
            scoreLabel.OffsetRight = 100; // Width constraint (~85px)
            scoreLabel.OffsetTop = 25;    // 25px from top (below ceiling)
            scoreLabel.OffsetBottom = 65;
            scoreLabel.Text = "000";  // Initialize with 3-digit zero-padding
            scoreLabel.AddThemeFontSizeOverride("font_size", 48);
            scoreLabel.AddThemeColorOverride("font_color", new Color(1, 1, 1, 1));  // White text
            scoreLabel.AddThemeFontOverride("font", arcadeFont);
            AddChild(scoreLabel);

            // Create lives label (top-right with 15px padding)
            livesLabel = new Label();
            livesLabel.AnchorLeft = 1;
            livesLabel.AnchorRight = 1;
            livesLabel.AnchorTop = 0;
            livesLabel.AnchorBottom = 0;
            livesLabel.OffsetLeft = -100; // 100px to the left from right edge
            livesLabel.OffsetRight = -15; // 15px padding from right wall
            livesLabel.OffsetTop = 25;    // 25px from top (below ceiling)
            livesLabel.OffsetBottom = 65;
            livesLabel.Text = "3";
            livesLabel.AddThemeFontSizeOverride("font_size", 48);
            livesLabel.AddThemeColorOverride("font_color", new Color(1, 1, 1, 1));  // White text
            livesLabel.AddThemeFontOverride("font", arcadeFont);
            livesLabel.HorizontalAlignment = HorizontalAlignment.Right;
            AddChild(livesLabel);

            GD.Print("UIComponent initialized (bold sans-serif, symmetric 15px padding)");
        }
        #endregion

        #region Animations
        /// <summary>
        /// Flash the score label based on brick color.
        /// Red=4 flashes, Orange=3, Green=2, Yellow=1 (matches crack count).
        /// </summary>
        public void FlashScoreForColor(Models.BrickColor color)
        {
            int flashCount = color switch
            {
                Models.BrickColor.Red => 4,
                Models.BrickColor.Orange => 3,
                Models.BrickColor.Green => 2,
                Models.BrickColor.Yellow => 1,
                _ => 1
            };

            scoreFlashTween?.Kill();
            scoreFlashTween = CreateTween();
            
            for (int i = 0; i < flashCount; i++)
            {
                scoreFlashTween.TweenProperty(scoreLabel, "modulate:a", 0.3f, 0.15f);
                scoreFlashTween.TweenProperty(scoreLabel, "modulate:a", 1.0f, 0.15f);
            }
        }
        #endregion

        #region Event Handlers
        /// <summary>
        /// Called when score changes.
        /// Updates score label display.
        /// </summary>
        public void OnScoreChanged(int newScore)
        {
            scoreLabel.Text = newScore.ToString().PadLeft(3, '0');
        }

        /// <summary>
        /// Called when lives change.
        /// Updates lives label display.
        /// </summary>
        public void OnLivesChanged(int newLives)
        {
            livesLabel.Text = $"{newLives}";
        }

        /// <summary>
        /// Called when game transitions to GameOver state.
        /// Displays game-over message in center of screen.
        /// </summary>
        public void ShowGameOverMessage()
        {
            // Create bold sans-serif SystemFont for consistency
            var arcadeFont = new SystemFont();
            arcadeFont.FontNames = new[] { "Arial", "Helvetica", "sans-serif" };
            arcadeFont.FontWeight = 700;  // Bold

            gameOverLabel = new Label();
            gameOverLabel.Text = "GAME OVER";
            gameOverLabel.AddThemeFontSizeOverride("font_size", 64);
            gameOverLabel.AddThemeColorOverride("font_color", new Color(1, 1, 1, 1));  // White
            gameOverLabel.AddThemeFontOverride("font", arcadeFont);
            gameOverLabel.HorizontalAlignment = HorizontalAlignment.Center;
            gameOverLabel.VerticalAlignment = VerticalAlignment.Center;
            
            // Stretch horizontally from left wall to right wall, center vertically
            gameOverLabel.AnchorLeft = 0f;
            gameOverLabel.AnchorRight = 1f;
            gameOverLabel.AnchorTop = 0.5f;
            gameOverLabel.AnchorBottom = 0.5f;
            
            // Offset to account for walls: start after left wall, end before right wall
            gameOverLabel.OffsetLeft = Config.WallThickness;
            gameOverLabel.OffsetRight = -Config.WallThickness;
            gameOverLabel.OffsetTop = -75;
            gameOverLabel.OffsetBottom = 75;
            
            AddChild(gameOverLabel);

            GD.Print("Game Over message displayed");
        }
        #endregion
    }
}
