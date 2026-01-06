using Godot;
using Breakout.Entities;
using Breakout.Infrastructure;
using Breakout.Models;
using Breakout.Services;
using Breakout.Components;

namespace Breakout.Game
{
    /// <summary>
    /// Game orchestrator: coordinates entities, signals, and game loop.
    /// Responsible for instantiation and signal binding only.
    /// Game state is owned by GameStateComponent (following Nystrom's pattern).
    /// </summary>
    public partial class Orchestrator : Node2D
    {
        #region Components & Entities

        /// <summary>
        /// Game state component owns all mutable state (score, lives, rules).
        /// </summary>
        private GameStateComponent gameState;

        /// <summary>
        /// Brick grid component owns and manages the brick grid.
        /// </summary>
        private BrickGridComponent brickGrid;

        /// <summary>
        /// Reference to the ball for speed control.
        /// </summary>
        private Ball ball;

        /// <summary>
        /// Reference to the paddle for shrinking.
        /// </summary>
        private Paddle paddle;
        #endregion

        #region Game Loop
        public override void _Ready()
        {
            // Instantiate components
            gameState = new GameStateComponent();
            brickGrid = new BrickGridComponent();

            // Instantiate entities using GameConfig
            paddle = new Paddle(
                Config.Paddle.Position,
                Config.Paddle.Size,
                Config.Paddle.Color
            );
            AddChild(paddle);

            ball = new Ball(
                Config.Ball.Position,
                Config.Ball.Size,
                Config.Ball.Velocity,
                Config.Ball.Color
            );
            AddChild(ball);

            // Instantiate infrastructure
            var walls = new Walls();
            AddChild(walls);

            // Instantiate brick grid
            brickGrid.InstantiateGrid(this);

            // Connect component events directly to actions (pure signal coordination)
            gameState.SpeedIncreaseRequired += ball.ApplySpeedMultiplier;
            gameState.PaddleShrinkRequired += paddle.Shrink;
            brickGrid.BrickDestroyedWithColor += gameState.OnBrickDestroyed;

            // Connect ball signals to game state
            ball.BallHitPaddle += OnBallHitPaddle;
            ball.BallOutOfBounds += OnBallOutOfBounds;
            ball.BallHitCeiling += () => gameState.OnBallHitCeiling();
        }

        public override void _Process(double delta)
        {
            // Game loop (state transitions, rule checks, etc.)
            // All event-driven now; no polling
        }
        #endregion

        #region Signals
        private void OnBallHitPaddle()
        {
            GD.Print("Ball hit paddle!");
        }

        private void OnBallOutOfBounds()
        {
            GD.Print("Ball out of bounds!");
            gameState.LoseLive();
        }
        #endregion
    }
}
