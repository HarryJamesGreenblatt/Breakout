using Godot;
using Breakout.Entities;
using Breakout.Infrastructure;

namespace Breakout
{
    /// <summary>
    /// A Game class that manages the main game loop, entities and infrastructure
    /// representing the 1976 arade game Breakout.
    /// </summary>
    public partial class Game : Node2D
    {
        #region Entities
        private Paddle paddle;
        #endregion

        #region Signals
        private void OnBallHitPaddle()
        {
            GD.Print("Ball hit the paddle!");
        }
        private void OnBallOutOfBounds()
        {
            GD.Print("Ball went out of bounds!");
        }
        #endregion

        #region Game Loop
        public override void _Ready()
        {
            // Initialize game entities
            paddle = new Paddle();
            AddChild(paddle);

            var ball = new Ball();
            AddChild(ball);

            // Initialize game infrastructure
            var walls = new Walls();
            AddChild(walls);

            // Connect signals
            ball.BallHitPaddle += OnBallHitPaddle;
            ball.BallOutOfBounds += OnBallOutOfBounds;


        }
        public override void _Process(double delta)
        {
            // Update game logic
        }
        #endregion
    }
}