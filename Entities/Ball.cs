using Breakout.Game;
using Breakout.Components;
using Godot;

namespace Breakout.Entities
{
    /// <summary>
    /// Ball entity — thin Node2D container that delegates physics to PhysicsComponent.
    /// 
    /// Following Nystrom's Component pattern:
    /// - Ball owns the node lifecycle (creation, rendering, scene hierarchy)
    /// - PhysicsComponent owns all physics state and behavior (velocity, collisions, bounces)
    /// - Ball connects signals and delegates work to the component
    /// - This separation allows physics to be tested and modified independently
    /// 
    /// Signals:
    /// - BallHitPaddle: emitted when paddle bounce occurs (from PhysicsComponent)
    /// - BallOutOfBounds: emitted when ball goes out of bounds (from PhysicsComponent)
    /// </summary>
    public partial class Ball : Area2D
    {
        #region Signals
        /// <summary>
        /// Triggered when the ball hits the paddle.
        /// Forwarded from PhysicsComponent.BounceOccurred.
        /// </summary>
        [Signal]
        public delegate void BallHitPaddleEventHandler();

        /// <summary>
        /// Triggered when the ball goes out of bounds (below the paddle).
        /// Forwarded from PhysicsComponent.OutOfBounds.
        /// </summary>
        [Signal]
        public delegate void BallOutOfBoundsEventHandler();

        /// <summary>
        /// Triggered when the ball hits the ceiling (upper wall).
        /// Forwarded from PhysicsComponent.CeilingHit.
        /// </summary>
        [Signal]
        public delegate void BallHitCeilingEventHandler();
        #endregion

        #region State
        /// <summary>
        /// Physics component owns all physics state (velocity, collisions, bounces).
        /// </summary>
        private PhysicsComponent physics;
        #endregion

        #region Constructor
        /// <summary>
        /// Constructs a ball entity at the given position.
        /// Initializes PhysicsComponent with velocity and ball size.
        /// Creates visual and collision shape.
        /// </summary>
        /// <param name="position">Top-left corner position</param>
        /// <param name="size">Width and height</param>
        /// <param name="initialVelocity">Starting velocity</param>
        /// <param name="color">Visual color</param>
        public Ball(Vector2 position, Vector2 size, Vector2 initialVelocity, Color color)
        {
            Position = position; // Top-left corner, like everything else

            // Initialize physics component (owns velocity, position updates, collision logic)
            physics = new PhysicsComponent(position, size, initialVelocity);

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
        /// Sets up the ball entity:
        /// - Connects physics component events to ball signals
        /// - Connects Area2D signals for collision detection
        /// </summary>
        public override void _Ready()
        {
            // Forward physics component events to Godot signals
            physics.BounceOccurred += (bounceType) =>
            {
                if (bounceType == "paddle")
                {
                    EmitSignal(SignalName.BallHitPaddle);
                }
            };
            physics.OutOfBounds += () =>
            {
                EmitSignal(SignalName.BallOutOfBounds);
            };
            physics.CeilingHit += () =>
            {
                EmitSignal(SignalName.BallHitCeiling);
            };

            // Connect area enter/exit signals to physics component
            AreaEntered += (area) => physics.HandleCollisionEnter((Area2D)area);
            AreaExited += (area) => physics.HandleCollisionExit((Area2D)area);
        }

        /// <summary>
        /// Updates the ball each frame:
        /// - Delegates physics update to PhysicsComponent
        /// - Updates node position based on component's calculated position
        /// 
        /// PhysicsComponent owns all logic:
        /// - Position updates from velocity
        /// - Wall collisions
        /// - Out of bounds detection
        /// - Event emission
        /// </summary>
        public override void _Process(double delta)
        {
            // Delegate all physics to component
            // Component updates position, handles walls, detects out-of-bounds, emits events
            Position = physics.Update((float)delta);
        }

        /// <summary>
        /// Exposes the physics component for direct wiring by Controller.
        /// Allows game rules to modify physics behavior without going through Ball.
        /// </summary>
        public PhysicsComponent GetPhysicsComponent()
        {
            return physics;
        }

        #endregion
    }
}
