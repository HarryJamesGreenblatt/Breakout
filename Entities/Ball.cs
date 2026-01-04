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
            Position = position; // Top-left corner, like everything else
            initialPosition = position;
            this.initialVelocity = initialVelocity;
            velocity = initialVelocity;

            // Collision shape offset to center of the visual rect
            var collisionShape = new CollisionShape2D();
            collisionShape.Position = size / 2;
            collisionShape.Shape = new CircleShape2D { Radius = size.X / 2 };
            AddChild(collisionShape);

            // Visual at node origin (top-left)
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
            // Connect area enter/exit signals for paddle collision detection
            AreaEntered += _OnAreaEntered;
            AreaExited += _OnAreaExited;
        }

        /// <summary>
        /// Updates the ball's position and handles collisions with walls and paddle.
        /// </summary>
        /// <param name="delta"></param>
        public override void _Process(double delta)
        {
            // Update position based on velocity
            Position += velocity * (float)delta;

            float ballRadius = GameConfig.Ball.Size.X / 2;
            
            // Bounce off left/right walls (walls now positioned outside viewport at x=0 and x=ViewportWidth)
            if (Position.X + ballRadius < 0)
            {
                velocity.X = -velocity.X;
            }
            else if (Position.X + ballRadius > GameConfig.ViewportWidth)
            {
                velocity.X = -velocity.X;
            }

            // Bounce off ceiling (wall now positioned outside viewport at y=-WallThickness)
            if (Position.Y + ballRadius < 0)
            {
                velocity.Y = -velocity.Y;
            }

            // Out of bounds (below paddle)
            if (Position.Y > GameConfig.Ball.OutOfBoundsY)
            {
                EmitSignal(SignalName.BallOutOfBounds);
                ResetBall();
            }
        }

        /// <summary>
        /// Handles paddle collision when the ball enters the paddle's area.
        /// Fires only once per contact due to AreaEntered signal semantics.
        /// </summary>
        private void _OnAreaEntered(Area2D area)
        {
            if (area is Paddle)
            {
                velocity.Y = -velocity.Y;
                EmitSignal(SignalName.BallHitPaddle);
            }
        }

        /// <summary>
        /// Handles paddle exit—used to reset collision state (for future multi-paddle scenarios).
        /// </summary>
        private void _OnAreaExited(Area2D area)
        {
            // Currently unused, but signals clean separation from paddle
            if (area is Paddle)
            {
                // Ball has left paddle contact
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