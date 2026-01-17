using Breakout.Components;
using Breakout.Entities;
using Breakout.Infrastructure;
using Godot;

namespace Breakout.Utilities
{
    /// <summary>
    /// SignalWiringUtility — pure orchestration of signal connections.
    /// Stateless utility following EntityFactoryUtility pattern.
    /// 
    /// Responsibility: Wire signals between components and entities.
    /// Zero state, zero awareness of broader game context.
    /// Each method is self-contained and handles one concern.
    /// </summary>
    public static class SignalWiringUtility
    {
        /// <summary>
        /// Wire game rule signals: speed increases.
        /// Note: PaddleShrinkRequired is wired by Controller directly (business logic).
        /// </summary>
        public static void WireGameRules(GameStateComponent gameState, PhysicsComponent ballPhysics, Paddle paddle)
        {
            gameState.SpeedIncreaseRequired += ballPhysics.ApplySpeedMultiplier;
            gameState.PaddleSpeedIncreaseRequired += paddle.ApplySpeedMultiplier;
        }

        /// <summary>
        /// Wire brick destruction to game rules and scoring.
        /// </summary>
        public static void WireBrickEvents(BrickGrid brickGrid, GameStateComponent gameState, UIComponent ui, SoundComponent sound)
        {
            brickGrid.BrickDestroyedWithColor += gameState.OnBrickDestroyed;      // Game rules
            brickGrid.BrickDestroyedWithColor += gameState.AddScore;               // Scoring
            brickGrid.BrickDestroyedWithColor += (color) => ui.FlashScoreForColor(color);       // UI feedback
            brickGrid.BrickDestroyedWithColor += (color) => sound.PlayBrickHit(color);         // Sound feedback
            brickGrid.AllBricksDestroyed += () => gameState.SetState(GameStateComponent.GameState.LevelComplete);
        }

        /// <summary>
        /// Wire UI events: score, lives, flashing, game over.
        /// </summary>
        public static void WireUIEvents(GameStateComponent gameState, UIComponent ui)
        {
            gameState.ScoreChanged += ui.OnScoreChanged;
            gameState.LivesChanged += ui.OnLivesChanged;
            
            // Lives flashing feedback
            gameState.LivesChanged += (lives) => {
                if (lives > 0) ui.FlashLivesLost();
                else if (lives <= 0) ui.FlashLivesIndefinitely();
            };

            // Game state transitions
            gameState.StateChanged += ui.OnGameStateChanged;
            gameState.LivesChanged += (lives) => {
                if (lives <= 0) gameState.SetState(GameStateComponent.GameState.GameOver);
            };

            // Game over UI
            gameState.GameOver += ui.ShowGameOverMessage;
            
            // Continue countdown UI
            gameState.ContinueCountdownChanged += ui.OnContinueCountdownChanged;
        }

        /// <summary>
        /// Wire ball physics and collision events to game logic.
        /// </summary>
        public static void WireBallEvents(Ball ball, Paddle paddle, GameStateComponent gameState)
        {
            ball.BallHitCeiling += gameState.OnBallHitCeiling;
            ball.BallOutOfBounds += gameState.DecrementLives;
        }

        /// <summary>
        /// Wire ball collision events to sound effects.
        /// </summary>
        public static void WireBallSoundEvents(Ball ball, PhysicsComponent ballPhysics, SoundComponent sound)
        {
            ball.BallHitPaddle += sound.PlayPaddleHit;
            ball.BallHitCeiling += sound.PlayWallBounce;
            ballPhysics.WallHit += sound.PlayWallBounce;
        }

        /// <summary>
        /// Wire game state changes to sound effects.
        /// </summary>
        public static void WireGameStateSoundEvents(GameStateComponent gameState, SoundComponent sound)
        {
            gameState.SpeedIncreaseRequired += (_) => sound.PlaySpeedIncrease();
            gameState.PaddleShrinkRequired += sound.PlayPaddleShrinkEffect;
            gameState.LivesChanged += (lives) => {
                // Only play sounds if not in Continuing state (restart in progress)
                if (gameState.GetState() != GameStateComponent.GameState.Continuing)
                {
                    if (lives > 0) sound.PlayLivesDecremented();
                    else if (lives <= 0) sound.PlayGameOver();
                }
            };
        }

        /// <summary>
        /// Wire game over state to entity disabling.
        /// </summary>
        public static void WireGameOverState(GameStateComponent gameState, Ball ball, Paddle paddle)
        {
            gameState.GameOver += () => {
                ball.Visible = false;  // Hide ball during game over screen
                ball.ProcessMode = Node.ProcessModeEnum.Disabled;
                paddle.SetInputEnabled(false);
            };
        }

        /// <summary>
        /// Wire state changes to control input during transitions.
        /// Disables paddle input during Transitioning state, re-enables during Playing state.
        /// </summary>
        public static void WireTransitionState(GameStateComponent gameState, Paddle paddle)
        {
            gameState.StateChanged += (state) => {
                if (state == GameStateComponent.GameState.Transitioning)
                {
                    paddle.SetInputEnabled(false);
                }
                else if (state == GameStateComponent.GameState.Playing)
                {
                    paddle.SetInputEnabled(true);
                }
            };
        }
    }
}
