using Godot;
using Breakout.Entities;
using Breakout.Infrastructure;

namespace Breakout
{
    /// <summary>
    /// Game orchestrator: manages entities, signals, and game loop.
    /// Responsible for instantiation, signal binding, and overall game state.
    /// </summary>
    public partial class GameOrchestrator : Node2D
    {
        #region Game Loop
        public override void _Ready()
        {
            // Instantiate entities using GameConfig
            var paddle = new Paddle(
                GameConfig.Paddle.Position,
                GameConfig.Paddle.Size,
                GameConfig.Paddle.Color
            );
            AddChild(paddle);

            var ball = new Ball(
                GameConfig.Ball.Position,
                GameConfig.Ball.Size,
                GameConfig.Ball.Velocity,
                GameConfig.Ball.Color
            );
            AddChild(ball);

            // Instantiate infrastructure
            var walls = new Walls();
            AddChild(walls);

            // Connect signal listeners
            ball.BallHitPaddle += OnBallHitPaddle;
            ball.BallOutOfBounds += OnBallOutOfBounds;
        }

        public override void _Process(double delta)
        {
            // Main game loop (future: game state, scoring, etc.)
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
        }
        #endregion
    }
}