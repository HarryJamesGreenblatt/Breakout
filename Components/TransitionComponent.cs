using Godot;
using System;
using System.Collections.Generic;
using Breakout.Entities;
using Breakout.Infrastructure;

namespace Breakout.Components
{
    /// <summary>
    /// TransitionComponent — orchestrates transition animations between game states.
    /// 
    /// Responsibilities:
    /// - Coordinate timing and sequencing of entity transitions
    /// - Manage the transition flow: bricks fade → paddle eases → ball blips → game starts
    /// - Emit event when transition completes to signal game can resume
    /// 
    /// Design:
    /// - Component is a Node to access Godot's Tween system and scene tree
    /// - Called by Controller when transitioning between states (game start, restart, level change)
    /// - Emits TransitionComplete event to notify GameStateComponent
    /// </summary>
    public partial class TransitionComponent : Node
    {
        #region Events
        /// <summary>
        /// Emitted when all transition animations complete.
        /// Signals that the game can transition from Transitioning → Playing state.
        /// </summary>
        public event Action TransitionComplete;
        #endregion

        #region Transition Methods
        /// <summary>
        /// Play the game restart transition sequence.
        /// 
        /// Sequence:
        /// 1. Bricks fade in simultaneously (1.5s)
        /// 2. Paddle eases to center with EaseInOut (0.8s) - overlaps with bricks
        /// 3. Ball blips in at 0.85s with "dolg" sound (like "Are you ready?")
        /// 4. Delay (0.5s) for anticipation after blip
        /// 5. Emit TransitionComplete → game launches (GO!)
        /// 
        /// Edge case handling:
        /// - Destroyed bricks fade in around existing (unbroken) bricks
        /// - Paddle smoothly eases to center (even if already close)
        /// - Ball teleports/blips instantly at the end
        /// 
        /// Design:
        /// - TransitionComponent owns all tween creation (respects thin entity pattern)
        /// - Entities just expose properties/positions, Component animates them
        /// </summary>
        /// <param name="paddle">Paddle entity to animate</param>
        /// <param name="ball">Ball entity to blip in</param>
        /// <param name="brickGrid">BrickGrid to fade in</param>
        public void PlayRestartTransition(Paddle paddle, Ball ball, BrickGrid brickGrid)
        {
            GD.Print("=== Starting Restart Transition ===");

            // Phase 1: Bricks fade in (1.5s, starts immediately)
            FadeInBricks(brickGrid, duration: 1.5f);

            // Phase 2: Paddle eases to center (0.8s, starts immediately, overlaps with bricks)
            EasePaddleToCenter(paddle, duration: 0.8f);

            // Phase 3: Ball blips in at 0.85s (after paddle completes) with "dolg" sound - "Are you ready?"
            var ballBlipTimer = GetTree().CreateTimer(0.85f);
            ballBlipTimer.Timeout += () =>
            {
                ball.BlipIn();  // Appears + emits signal for "dolg" sound
                
                // Phase 4: Delay after blip for anticipation before launch - the "ready... GO!" moment
                var completeTimer = GetTree().CreateTimer(2.0f);  // 2 second pause after blip
                completeTimer.Timeout += () =>
                {
                    GD.Print("=== Restart Transition Complete ===");
                    TransitionComplete?.Invoke();
                };
            };
        }

        /// <summary>
        /// Play the game start transition (initial game load).
        /// Same as restart transition - unified behavior.
        /// </summary>
        public void PlayGameStartTransition(Paddle paddle, Ball ball, BrickGrid brickGrid)
        {
            PlayRestartTransition(paddle, ball, brickGrid);
        }

        /// <summary>
        /// Play the level complete transition (advance to next level).
        /// Future implementation - for now, same as restart.
        /// </summary>
        public void PlayLevelCompleteTransition(Paddle paddle, Ball ball, BrickGrid brickGrid)
        {
            PlayRestartTransition(paddle, ball, brickGrid);
        }
        #endregion

        #region Private Animation Methods
        /// <summary>
        /// Fade in all bricks in the grid simultaneously.
        /// TransitionComponent owns tween creation (thin entity pattern).
        /// </summary>
        private void FadeInBricks(BrickGrid brickGrid, float duration)
        {
            // Get destroyed brick IDs (only these should fade in during restart)
            var destroyedIds = new HashSet<int>(brickGrid.GetDestroyedBrickIds());
            
            foreach (var brick in brickGrid.GetAllBricks())
            {
                // Only fade in destroyed bricks
                // Unbroken bricks are already visible (alpha=1) and don't need animation
                if (destroyedIds.Count > 0)
                {
                    // During restart: only animate destroyed bricks
                    // Skip if brick is not in destroyed list (it's unbroken and already visible)
                    var brickIdStr = ((string)brick.Name).Split('_')[1];
                    var brickId = int.Parse(brickIdStr);
                    if (!destroyedIds.Contains(brickId))
                    {
                        continue;  // Skip unbroken bricks
                    }
                }
                
                var tween = CreateTween();
                tween.SetEase(Tween.EaseType.InOut);
                tween.SetTrans(Tween.TransitionType.Quad);
                tween.TweenProperty(brick, "modulate:a", 1f, duration);
            }
            GD.Print($"Fading in bricks over {duration}s");
        }

        /// <summary>
        /// Ease paddle to center position with smooth EaseInOut curve.
        /// TransitionComponent owns tween creation (thin entity pattern).
        /// </summary>
        private void EasePaddleToCenter(Paddle paddle, float duration)
        {
            Vector2 targetPosition = paddle.GetCenterPosition();
            
            var tween = CreateTween();
            tween.SetEase(Tween.EaseType.InOut);
            tween.SetTrans(Tween.TransitionType.Quad);
            tween.TweenProperty(paddle, "position", targetPosition, duration);
            
            GD.Print($"Paddle easing to center: {targetPosition} over {duration}s");
        }
        #endregion
    }
}
