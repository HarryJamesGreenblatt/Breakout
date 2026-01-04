using Godot;

namespace Breakout.Entities
{
    /// <summary>
    /// Ball entity with physics simulation, collision detection, and signals.
    /// Parametrized for position, size, velocity, and color.
    /// </summary>
    public partial class Ball : Area2D
    {
        #region Signals
        /// <summary>
        /// Triggered when the ball hits the paddle.
        /// </summary>
        [Signal]
        public delegate void BallHitPaddleEventHandler();

        /// <summary>
        /// Triggered when the ball goes out of bounds (below the paddle).
        /// </summary>
        [Signal]
        public delegate void BallOutOfBoundsEventHandler();
        #endregion

        #region Physics Properties
        private Vector2 velocity;
        private Vector2 initialVelocity;
        private Vector2 initialPosition;
        #endregion

        #region Constructor
        /// <summary>
        /// Default constructor for Ball.
        /// </summary>
        /// <param name="position"></param>
        /// <param name="size"></param>
        /// <param name="initialVelocity"></param>
        /// <param name="color"></param>
        public Ball(Vector2 position, Vector2 size, Vector2 initialVelocity, Color color)
        {
            Position = position;
            initialPosition = position;
            this.initialVelocity = initialVelocity;
            velocity = initialVelocity;

            // Collision shape
            var collisionShape = new CollisionShape2D();
            collisionShape.Shape = new CircleShape2D { Radius = size.X / 2 };
            AddChild(collisionShape);

            // Visual representation
            var visual = new ColorRect
            {
                Size = size,
                Color = color
            };
            AddChild(visual);

            // Collision setup from config
            CollisionLayer = GameConfig.Ball.CollisionLayer;
            CollisionMask = GameConfig.Ball.CollisionMask;
        }
        #endregion

        #region Game Behavior
        /// <summary>
        /// Sets up the ball entity by connecting collision signals.
        /// </summary>
        public override void _Ready()
        {
            // Connect collision signal
            AreaEntered += _OnAreaEntered;
        }

        /// <summary>
        /// Updates the ball's position and handles collisions with walls and paddle.
        /// </summary>
        /// <param name="delta"></param>
        public override void _Process(double delta)
        {
            // Update position based on velocity
            Position += velocity * (float)delta;

            // Bounce off left/right walls (check if ball center crosses wall boundaries)
            if (Position.X + GameConfig.Ball.BounceMarginX < GameConfig.WallThickness)
            {
                velocity.X = -velocity.X;
            }
            else if (Position.X + GameConfig.Ball.BounceMarginX > GameConfig.ViewportWidth - GameConfig.WallThickness)
            {
                velocity.X = -velocity.X;
            }

            // Bounce off ceiling
            if (Position.Y + GameConfig.Ball.BounceMarginTop < GameConfig.WallThickness)
            {
                velocity.Y = -velocity.Y;
            }

            // Check for continuous paddle collision (prevents tunneling)
            var overlappingAreas = GetOverlappingAreas();
            foreach (var area in overlappingAreas)
            {
                if (area is Paddle)
                {
                    velocity.Y = -velocity.Y;
                    EmitSignal(SignalName.BallHitPaddle);
                    break;
                }
            }

            // Out of bounds (below paddle)
            if (Position.Y > GameConfig.Ball.OutOfBoundsY)
            {
                EmitSignal(SignalName.BallOutOfBounds);
                ResetBall();
            }
        }

        /// <summary>
        /// Defines behavior when the ball collides with another area (e.g., paddle).
        /// </summary>
        /// <param name="area"></param>
        private void _OnAreaEntered(Area2D area)
        {
            if (area is Paddle)
            {
                velocity.Y = -velocity.Y;
                EmitSignal(SignalName.BallHitPaddle);
            }
        }

        /// <summary>
        /// Resets the ball to its initial position and velocity.
        /// </summary>
        /// <remarks>Call this method to return the ball to its starting state, typically after a point is
        /// scored or to restart play.</remarks>
        private void ResetBall()
        {
            Position = initialPosition;
            velocity = initialVelocity;
        }
        #endregion
    }
}