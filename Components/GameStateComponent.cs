using Godot;
using System;
using Breakout.Utilities;

namespace Breakout.Components
{
    /// <summary>
    /// GameStateComponent — owns all mutable game state, rule logic, and state machine.
    /// 
    /// Following Nystrom's Component pattern:
    /// - Component owns state (score, lives, hit count, speed milestones, paddle shrink flag, flow state)
    /// - Component is a plain C# class (NOT a Node), like PhysicsComponent
    /// - Component emits C# events for state changes
    /// - Orchestrator coordinates by listening to events and calling entity methods
    /// 
    /// Encapsulates:
    /// - Canonical Breakout rules (scoring, lives, speed increases, paddle shrink)
    /// - Game flow state machine (Playing, GameOver, LevelComplete)
    /// - State transitions triggered by game events (lives=0, all bricks destroyed)
    /// 
    /// Events are emitted when state changes occur, allowing Orchestrator to react.
    /// </summary>
    public partial class GameStateComponent
    {
        #region Constants
        /// <summary>
        /// Continue countdown duration in seconds (arcade-style).
        /// Player has 10 seconds to press a key to continue, or game over is final.
        /// </summary>
        private const float ContinueCountdownSeconds = 10f;
        #endregion

        #region State Enum
        public enum GameState
        {
            Playing,
            GameOver,
            LevelComplete,
            Paused
        }
        #endregion
        #region State
        /// <summary>
        /// Player score.
        /// </summary>
        private int score = 0;

        /// <summary>
        /// Lives remaining (starts at 3 for canonical Breakout).
        /// </summary>
        private int lives = 3;

        /// <summary>
        /// Cumulative brick destruction count (for speed milestones).
        /// </summary>
        private int totalHits = 0;

        /// <summary>
        /// Track if speed milestone at 4 hits has been applied.
        /// Prevents re-applying.
        /// </summary>
        private bool speedMilestone4Applied = false;

        /// <summary>
        /// Track if speed milestone at 12 hits has been applied.
        /// Prevents re-applying.
        /// </summary>
        private bool speedMilestone12Applied = false;

        /// <summary>
        /// Track if speed increase from orange row contact has been applied.
        /// Prevents applying multiple times.
        /// </summary>
        private bool speedOrangeRowApplied = false;

        /// <summary>
        /// Track if speed increase from red row contact has been applied.
        /// Prevents applying multiple times.
        /// </summary>
        private bool speedRedRowApplied = false;

        /// <summary>
        /// Track if paddle has been shrunk.
        /// Canonical Breakout: paddle shrinks once per game.
        /// </summary>
        private bool paddleHasShrunk = false;

        /// <summary>
        /// Track if red row has been broken through.
        /// Set when first red brick is destroyed, cleared when paddle shrinks.
        /// </summary>
        private bool redRowBroken = false;

        /// <summary>
        /// Current game flow state.
        /// </summary>
        private GameState currentState = GameState.Playing;

        /// <summary>
        /// Continue countdown timer (seconds remaining).
        /// When game reaches GameOver state, this counts down from ContinueCountdownSeconds.
        /// When it reaches 0, no more restart allowed (final game over).
        /// </summary>
        private float continueCountdownRemaining = 0f;
        #endregion

        #region Events
        /// <summary>
        /// Emitted when score changes.
        /// </summary>
        public event Action<int> ScoreChanged;

        /// <summary>
        /// Emitted when lives change.
        /// </summary>
        public event Action<int> LivesChanged;

        /// <summary>
        /// Emitted when a speed increase is required.
        /// </summary>
        public event Action<float> SpeedIncreaseRequired;

        /// <summary>
        /// Emitted when paddle speed should increase to compensate for ball speed.
        /// </summary>
        public event Action<float> PaddleSpeedIncreaseRequired;

        /// <summary>
        /// Emitted when paddle should shrink.
        /// </summary>
        public event Action PaddleShrinkRequired;

        /// <summary>
        /// Emitted when a brick is destroyed (for scoring).
        /// Includes brick color enum to look up point value.
        /// </summary>
        public event Action<BrickColor> BrickDestroyed;

        /// <summary>
        /// Emitted when game state transitions.
        /// </summary>
        public event Action<GameState> StateChanged;

        /// <summary>
        /// Emitted specifically when entering GameOver state.
        /// </summary>
        public event Action GameOver;

        /// <summary>
        /// Emitted specifically when entering LevelComplete state.
        /// </summary>
        public event Action LevelComplete;

        /// <summary>
        /// Emitted when continue countdown changes (every second during game over).
        /// Passes remaining seconds as integer for UI display.
        /// </summary>
        public event Action<int> ContinueCountdownChanged;

        /// <summary>
        /// Emitted when continue countdown expires (reaches 0).
        /// Allows Controller to exit the game gracefully.
        /// </summary>
        public event Action ContinueCountdownExpired;
        #endregion

        #region Constructor
        public GameStateComponent()
        {
            // Initialize with canonical Breakout settings
            score = 0;
            lives = 3;
            totalHits = 0;
        }
        #endregion

        #region Public API
        /// <summary>
        /// Called when a brick is destroyed.
        /// Tracks hit count, applies speed rules, checks paddle shrink condition.
        /// </summary>
        public void OnBrickDestroyed(BrickColor color)
        {
            totalHits++;
            BrickDestroyed?.Invoke(color);

            GD.Print($"Brick destroyed: {color}. Total hits: {totalHits}");

            // Canonical Breakout speed increase rules (applied once, not compounded)
            // Rule 1: Speed up after 4 hits (applied once)
            if (totalHits == 4 && !speedMilestone4Applied)
            {
                SpeedIncreaseRequired?.Invoke(1.15f);
                PaddleSpeedIncreaseRequired?.Invoke(1.15f);
                speedMilestone4Applied = true;
                GD.Print("Speed increase #1 (4-hit milestone): 15% faster");
            }
            // Rule 2: Speed up after 12 hits (applied once)
            else if (totalHits == 12 && !speedMilestone12Applied)
            {
                SpeedIncreaseRequired?.Invoke(1.15f);
                PaddleSpeedIncreaseRequired?.Invoke(1.15f);
                speedMilestone12Applied = true;
                GD.Print("Speed increase #2 (12-hit milestone): 15% faster");
            }

            // Rule 3: Speed up on contact with orange or red rows (once per color)
            if (color == BrickColor.Orange && !speedOrangeRowApplied)
            {
                SpeedIncreaseRequired?.Invoke(1.15f);
                PaddleSpeedIncreaseRequired?.Invoke(1.15f);
                speedOrangeRowApplied = true;
                GD.Print("Speed increase on first orange row contact: 15% faster");
            }
            else if (color == BrickColor.Red && !speedRedRowApplied)
            {
                SpeedIncreaseRequired?.Invoke(1.15f);
                PaddleSpeedIncreaseRequired?.Invoke(1.15f);
                speedRedRowApplied = true;
                GD.Print("Speed increase on first red row contact: 15% faster");
            }

            // Canonical Breakout paddle shrink rule:
            // Set flag when red row is hit (only if paddle hasn't already shrunk)
            if (color == BrickColor.Red && !paddleHasShrunk)
            {
                redRowBroken = true;
                GD.Print("Red row broken. Paddle will shrink on next ceiling hit.");
            }
        }

        /// <summary>
        /// Called when ball hits ceiling.
        /// If red row was broken, emits paddle shrink event (once).
        /// </summary>
        public void OnBallHitCeiling()
        {
            if (redRowBroken && !paddleHasShrunk)
            {
                PaddleShrinkRequired?.Invoke();
                paddleHasShrunk = true;
                redRowBroken = false;
                GD.Print("Paddle shrunk. Will not shrink again.");
            }
        }

        /// <summary>
        /// Called when ball hits a brick (for scoring).
        /// Looks up point value from BrickColorUtility and updates score.
        /// </summary>
        public void AddScore(BrickColor color)
        {
            int points = BrickColorUtility.GetConfig(color).Points;
            score += points;
            ScoreChanged?.Invoke(score);
            GD.Print($"Score +{points}. Total: {score}");
        }

        /// <summary>
        /// Called when ball goes out of bounds.
        /// Decrements lives by one.
        /// </summary>
        public void DecrementLives()
        {
            lives--;
            LivesChanged?.Invoke(lives);
            GD.Print($"Lives: {lives}");

            if (lives <= 0)
            {
                GD.Print("Game Over!");
            }
        }

        /// <summary>
        /// Query current score.
        /// </summary>
        public int GetScore() => score;

        /// <summary>
        /// Query current lives.
        /// </summary>
        public int GetLives() => lives;

        /// <summary>
        /// Query current hit count.
        /// </summary>
        public int GetTotalHits() => totalHits;

        /// <summary>
        /// Query if paddle has been shrunk.
        /// </summary>
        public bool HasPaddleShrunk() => paddleHasShrunk;

        /// <summary>
        /// Transition to a new game state.
        /// Validates transition rules before allowing state change.
        /// When entering GameOver, starts the continue countdown.
        /// </summary>
        public void SetState(GameState newState)
        {
            if (newState == currentState)
                return;  // No-op if state doesn't change

            GD.Print($"GameState: {currentState} → {newState}");

            currentState = newState;
            StateChanged?.Invoke(currentState);

            // Emit state-specific events for specialized handling
            switch (currentState)
            {
                case GameState.GameOver:
                    continueCountdownRemaining = ContinueCountdownSeconds;
                    ContinueCountdownChanged?.Invoke((int)continueCountdownRemaining);
                    GameOver?.Invoke();
                    break;

                case GameState.LevelComplete:
                    LevelComplete?.Invoke();
                    break;
            }
        }

        /// <summary>
        /// Query current state.
        /// </summary>
        public GameState GetState() => currentState;

        /// <summary>
        /// Check if game is actively playing (not paused or over).
        /// </summary>
        public bool IsPlaying() => currentState == GameState.Playing;

        /// <summary>
        /// Get remaining continue countdown seconds.
        /// Returns 0 if not in GameOver state.
        /// </summary>
        public float GetContinueCountdownRemaining() => continueCountdownRemaining;

        /// <summary>
        /// Update continue countdown (called each frame from Controller during GameOver).
        /// Decrements timer and emits event when seconds change.
        /// </summary>
        public void UpdateContinueCountdown(float delta)
        {
            if (currentState != GameState.GameOver || continueCountdownRemaining <= 0)
                return;

            continueCountdownRemaining -= delta;
            if (continueCountdownRemaining < 0)
                continueCountdownRemaining = 0;

            // Emit event when seconds digit changes (once per second)
            int currentSeconds = (int)continueCountdownRemaining;
            ContinueCountdownChanged?.Invoke(currentSeconds);

            // When countdown expires, emit event so Controller can quit
            if (continueCountdownRemaining == 0)
            {
                ContinueCountdownExpired?.Invoke();
            }
        }

        /// <summary>
        /// Reset all game state to initial values.
        /// Called when restarting the game.
        /// </summary>
        public void Reset()
        {
            score = 0;
            lives = 3;
            totalHits = 0;
            speedMilestone4Applied = false;
            speedMilestone12Applied = false;
            speedOrangeRowApplied = false;
            speedRedRowApplied = false;
            paddleHasShrunk = false;
            redRowBroken = false;
            currentState = GameState.Playing;
            continueCountdownRemaining = 0f;

            ScoreChanged?.Invoke(score);
            LivesChanged?.Invoke(lives);
            StateChanged?.Invoke(currentState);  // Notify listeners (e.g., UIComponent) of state transition
            GD.Print("GameState reset to initial values");
        }
        #endregion
    }
}
