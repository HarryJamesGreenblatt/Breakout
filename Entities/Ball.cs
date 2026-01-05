using Breakout.Game;
using Breakout.Components;
using Godot;
using System.Collections.Generic;

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

        #region Physics
        /// <summary>
        /// Physics component handles all velocity state and bounce logic.
        /// </summary>
        private PhysicsComponent physics;

        /// <summary>
        /// Initial position for reset.
        /// </summary>
        private Vector2 initialPosition;

        /// <summary>
        /// Track active collisions to prevent bouncing multiple times in same contact.
        /// Uses signal-based state: collision tracked when AreaEntered, removed when AreaExited.
        /// Only bounce on transition to "new contact" (not already in contact list).
        /// </summary>
        private HashSet<Node> activeCollisions = new();
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

            // Initialize physics component with velocity and ball size
            physics = new PhysicsComponent(initialVelocity, size);

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
            CollisionLayer = Config.Ball.CollisionLayer;
            CollisionMask = Config.Ball.CollisionMask;
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
            // Update position based on physics component's velocity
            Position = physics.UpdatePosition(Position, (float)delta);

            // Handle wall collisions via physics component
            physics.HandleWallBounceX(Position);
            physics.HandleWallBounceY(Position);

            // Out of bounds (below paddle)
            if (Position.Y > Config.Ball.OutOfBoundsY)
            {
                EmitSignal(SignalName.BallOutOfBounds);
                ResetBall();
            }
        }

        /// <summary>
        /// Handles paddle collision when the ball enters the paddle's area.
        /// Uses signal-based state tracking: only bounce on NEW contact (transition from no-contact → contact).
        /// Contact is tracked in activeCollisions set; cleared on area_exited.
        /// </summary>
        private void _OnAreaEntered(Area2D area)
        {
            // Only process bounce if this is a NEW contact (not already being tracked)
            if (activeCollisions.Contains(area)) return;
            activeCollisions.Add(area);

            if (area is Paddle paddle)
            {
                // Apply angled bounce based on paddle contact point
                // (ApplyPaddleAngledBounce handles Y reversal internally)
                Vector2 ballCenter = Position + Config.Ball.Size / 2;
                Vector2 paddleCenter = paddle.Position + paddle.GetSize() / 2;
                physics.ApplyPaddleAngledBounce(ballCenter, paddleCenter, paddle.GetSize());
                
                EmitSignal(SignalName.BallHitPaddle);
            }
            else if (area is Brick brick)
            {
                GD.Print("[Ball] BRICK BOUNCE TRIGGERED");
                // Delegate brick bounce to physics component
                Vector2 ballCenter = Position + Config.Ball.Size / 2;
                Vector2 brickCenter = brick.Position + brick.GetBrickSize() / 2;
                physics.HandleBrickBounce(ballCenter, brickCenter, brick.GetBrickSize());
            }
        }

        /// <summary>
        /// Handles collision exit—clears contact tracking to allow bounces on re-entry.
        /// Following Godot's signal-based pattern: use area_exited to track state.
        /// </summary>
        private void _OnAreaExited(Area2D area)
        {
            activeCollisions.Remove(area);
        }

        /// <summary>
        /// Resets the ball to its initial position and velocity.
        /// </summary>
        /// <remarks>Call this method to return the ball to its starting state, typically after a point is
        /// scored or to restart play.</remarks>
        private void ResetBall()
        {
            Position = initialPosition;
            physics.ResetVelocity();
        }
        #endregion
    }
}