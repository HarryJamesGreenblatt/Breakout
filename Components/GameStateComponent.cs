using Godot;
using System;

namespace Breakout.Components
{
    /// <summary>
    /// GameStateComponent — owns all mutable game state and rule logic.
    /// 
    /// Following Nystrom's Component pattern:
    /// - Component owns state (score, lives, hit count, speed milestones, paddle shrink flag)
    /// - Component is a plain C# class (NOT a Node), like PhysicsComponent
    /// - Component emits C# events for state changes
    /// - Orchestrator coordinates by listening to events and calling entity methods
    /// 
    /// Encapsulates canonical Breakout rules:
    /// - Score tracking
    /// - Lives management
    /// - Speed increase milestones (4 hits, 12 hits, orange row, red row)
    /// - Paddle shrink rule (once per game after red row + ceiling hit)
    /// 
    /// Events are emitted when state changes occur, allowing Orchestrator to react.
    /// </summary>
    public partial class GameStateComponent
    {
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
        /// Emitted when paddle should shrink.
        /// </summary>
        public event Action PaddleShrinkRequired;

        /// <summary>
        /// Emitted when a brick is destroyed (for scoring).
        /// Includes brick color enum to look up point value.
        /// </summary>
        public event Action<Models.BrickColor> BrickDestroyed;
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
        public void OnBrickDestroyed(Models.BrickColor color)
        {
            totalHits++;
            BrickDestroyed?.Invoke(color);

            GD.Print($"Brick destroyed: {color}. Total hits: {totalHits}");

            // Canonical Breakout speed increase rules (applied once, not compounded)
            // Rule 1: Speed up after 4 hits (applied once)
            if (totalHits == 4 && !speedMilestone4Applied)
            {
                SpeedIncreaseRequired?.Invoke(1.05f);
                speedMilestone4Applied = true;
                GD.Print("Speed increase #1 (4-hit milestone)");
            }
            // Rule 2: Speed up after 12 hits (applied once)
            else if (totalHits == 12 && !speedMilestone12Applied)
            {
                SpeedIncreaseRequired?.Invoke(1.05f);
                speedMilestone12Applied = true;
                GD.Print("Speed increase #2 (12-hit milestone)");
            }

            // Rule 3: Speed up on contact with orange or red rows (once per color)
            if (color == Models.BrickColor.Orange && !speedOrangeRowApplied)
            {
                SpeedIncreaseRequired?.Invoke(1.05f);
                speedOrangeRowApplied = true;
                GD.Print("Speed increase on first orange row contact");
            }
            else if (color == Models.BrickColor.Red && !speedRedRowApplied)
            {
                SpeedIncreaseRequired?.Invoke(1.05f);
                speedRedRowApplied = true;
                GD.Print("Speed increase on first red row contact");
            }

            // Canonical Breakout paddle shrink rule:
            // Set flag when red row is hit (only if paddle hasn't already shrunk)
            if (color == Models.BrickColor.Red && !paddleHasShrunk)
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
        public void AddScore(Models.BrickColor color)
        {
            int points = Utilities.BrickColorUtility.GetConfig(color).Points;
            score += points;
            ScoreChanged?.Invoke(score);
            GD.Print($"Score +{points}. Total: {score}");
        }

        /// <summary>
        /// Called when ball goes out of bounds.
        /// Decrements lives.
        /// </summary>
        public void LoseLive()
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
        #endregion
    }
}
