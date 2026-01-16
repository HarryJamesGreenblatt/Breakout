using Godot;
using System;
using Breakout.Game;
using Breakout.Utilities;

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
        private Label continueCountdownLabel;
        private Tween scoreFlashTween;
        private Tween livesFlashTween;
        #endregion

        #region Lifecycle
        public override void _Ready()
        {
            // Load arcade font using the official Godot 4.5 runtime font loading method
            Font arcadeFont = LoadArcadeFont();

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
            scoreLabel.AddThemeFontSizeOverride("font_size", Config.UI.ScoreLabelFontSize);
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
            livesLabel.AddThemeFontSizeOverride("font_size", Config.UI.ScoreLabelFontSize);
            livesLabel.AddThemeColorOverride("font_color", new Color(1, 1, 1, 1));  // White text
            livesLabel.AddThemeFontOverride("font", arcadeFont);
            livesLabel.HorizontalAlignment = HorizontalAlignment.Right;
            AddChild(livesLabel);

            GD.Print("UIComponent initialized (arcade font loaded, 15px padding)");
        }

        /// <summary>
        /// Loads arcade font using Godot 4.5 official runtime font loading API.
        /// Based on: https://docs.godotengine.org/en/4.5/tutorials/io/runtime_file_loading_and_saving.html#fonts
        /// 
        /// Note: load_dynamic_font() works with filesystem paths that can be resolved
        /// by Godot, including res:// paths which are automatically converted to project folder paths.
        /// </summary>
        private Font LoadArcadeFont()
        {
            const string fontPath = "res://UI/Fonts/PressStart2P-Regular.ttf";
            
            try
            {
                var fontFile = new FontFile();
                
                // Use load_dynamic_font() as documented in Godot 4.5
                Error error = fontFile.LoadDynamicFont(fontPath);
                
                if (error == Error.Ok)
                {
                    GD.Print($"Successfully loaded arcade font from: {fontPath}");
                    return fontFile;
                }
                else
                {
                    GD.PrintErr($"FontFile.load_dynamic_font() failed with error code: {error}");
                    GD.PrintErr($"Attempted path: {fontPath}");
                }
            }
            catch (Exception ex)
            {
                GD.PrintErr($"Exception in LoadArcadeFont(): {ex.Message}");
                GD.PrintErr($"Stack trace: {ex.StackTrace}");
            }
            
            // Fallback: use bold SystemFont if loading fails
            GD.Print("Falling back to SystemFont");
            var fallbackFont = new SystemFont();
            fallbackFont.FontNames = new[] { "Arial", "Helvetica", "sans-serif" };
            fallbackFont.FontWeight = 800;  // Extra bold
            return fallbackFont;
        }
        #endregion

        #region Animations
        /// <summary>
        /// Flash the score label based on brick color.
        /// Red=4 flashes, Orange=3, Green=2, Yellow=1 (matches crack count).
        /// </summary>
        public void FlashScoreForColor(BrickColor color)
        {
            int flashCount = color switch
            {
                BrickColor.Red => 4,
                BrickColor.Orange => 3,
                BrickColor.Green => 2,
                BrickColor.Yellow => 1,
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

        /// <summary>
        /// Flash the lives label once when a life is lost.
        /// Single brief flash for visual feedback.
        /// </summary>
        public void FlashLivesLost()
        {
            livesFlashTween?.Kill();
            livesFlashTween = CreateTween();
            livesFlashTween.TweenProperty(livesLabel, "modulate:a", 0.3f, 0.15f);
            livesFlashTween.TweenProperty(livesLabel, "modulate:a", 1.0f, 0.15f);
        }

        /// <summary>
        /// Flash the lives label indefinitely when all lives are lost.
        /// Warning indicator until game over screen is shown.
        /// </summary>
        public void FlashLivesIndefinitely()
        {
            livesFlashTween?.Kill();
            livesFlashTween = CreateTween();
            livesFlashTween.SetLoops();  // Repeat forever
            livesFlashTween.TweenProperty(livesLabel, "modulate:a", 0.3f, 0.15f);
            livesFlashTween.TweenProperty(livesLabel, "modulate:a", 1.0f, 0.15f);
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
            // Load arcade font for consistency
            Font arcadeFont = LoadArcadeFont();

            gameOverLabel = new Label();
            gameOverLabel.Text = "GAME OVER";
            gameOverLabel.AddThemeFontSizeOverride("font_size", Config.UI.GameOverFontSize);
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

        /// <summary>
        /// Hides the game over message.
        /// Called from OnGameStateChanged when state transitions to Playing.
        /// </summary>
        private void HideGameOverMessage()
        {
            if (gameOverLabel != null)
            {
                gameOverLabel.QueueFree();
                gameOverLabel = null;
                GD.Print("Game Over message hidden");
            }
        }

        /// <summary>
        /// Called when game state changes.
        /// Hides game over message if transitioning back to Playing.
        /// </summary>
        public void OnGameStateChanged(GameStateComponent.GameState newState)
        {
            if (newState == GameStateComponent.GameState.Playing)
            {
                HideGameOverMessage();
                HideContinueCountdown();
            }
        }

        /// <summary>
        /// Called when continue countdown changes.
        /// Displays or updates the countdown number below game over message.
        /// </summary>
        public void OnContinueCountdownChanged(int secondsRemaining)
        {
            if (secondsRemaining > 0)
            {
                if (continueCountdownLabel == null)
                {
                    // Create countdown label below game over message
                    Font arcadeFont = LoadArcadeFont();
                    
                    continueCountdownLabel = new Label();
                    continueCountdownLabel.Text = $"CONTINUE: {secondsRemaining}";
                    continueCountdownLabel.AddThemeFontSizeOverride("font_size", (int)(Config.UI.GameOverFontSize * 0.7f));  // Smaller subheading
                    continueCountdownLabel.AddThemeColorOverride("font_color", new Color(1, 1, 1, 1));
                    continueCountdownLabel.AddThemeFontOverride("font", arcadeFont);
                    continueCountdownLabel.HorizontalAlignment = HorizontalAlignment.Center;
                    continueCountdownLabel.VerticalAlignment = VerticalAlignment.Center;
                    
                    continueCountdownLabel.AnchorLeft = 0f;
                    continueCountdownLabel.AnchorRight = 1f;
                    continueCountdownLabel.AnchorTop = 0.5f;
                    continueCountdownLabel.AnchorBottom = 0.5f;
                    
                    continueCountdownLabel.OffsetLeft = Config.WallThickness;
                    continueCountdownLabel.OffsetRight = -Config.WallThickness;
                    continueCountdownLabel.OffsetTop = 50;  // Below game over message
                    continueCountdownLabel.OffsetBottom = 150;
                    
                    AddChild(continueCountdownLabel);
                    GD.Print($"Continue countdown displayed: {secondsRemaining}");
                }
                else
                {
                    // Update countdown text
                    continueCountdownLabel.Text = $"CONTINUE: {secondsRemaining}";
                }
            }
            else
            {
                HideContinueCountdown();
            }
        }

        /// <summary>
        /// Hides the continue countdown label.
        /// Called when countdown reaches 0 or game returns to playing state.
        /// </summary>
        private void HideContinueCountdown()
        {
            if (continueCountdownLabel != null)
            {
                continueCountdownLabel.QueueFree();
                continueCountdownLabel = null;
                GD.Print("Continue countdown hidden");
            }
        }
        #endregion
    }
}
