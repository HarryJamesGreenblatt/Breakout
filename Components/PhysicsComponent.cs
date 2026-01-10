using Godot;
using Breakout.Game;
using System.Collections.Generic;

namespace Breakout.Components
{
    /// <summary>
    /// PhysicsComponent encapsulates ball physics simulation.
    /// Follows Nystrom's Component pattern: owns all physics state and behavior.
    /// 
    /// Responsibilities:
    /// - Velocity state management
    /// - Position updates based on velocity
    /// - Collision detection and bounce calculations
    /// - Collision state tracking (active contacts)
    /// - Speed modifiers for game rules
    /// 
    /// This component is owned by Ball entity; Ball delegates physics to it.
    /// Component signals important events (collisions, out of bounds).
    /// </summary>
    public class PhysicsComponent
    {
        #region Events
        /// <summary>
        /// C# event emitted when ball bounces. Allows Ball to respond (e.g., emit BallHitPaddle signal).
        /// Parameters: bounceType ("paddle", "brick", etc)
        /// </summary>
        public event System.Action<string> BounceOccurred;

        /// <summary>
        /// C# event emitted when ball goes out of bounds. Allows Ball to reset and emit BallOutOfBounds signal.
        /// </summary>
        public event System.Action OutOfBounds;

        /// <summary>
        /// C# event emitted when ball hits ceiling (upper wall).
        /// Allows game rules to respond (e.g., paddle shrink after red row contact).
        /// </summary>
        public event System.Action CeilingHit;

        /// <summary>
        /// C# event emitted when ball bounces off left/right walls.
        /// Allows audio to respond with wall bounce sound.
        /// </summary>
        public event System.Action WallHit;
        #endregion

        #region State
        /// <summary>
        /// Current velocity (direction and magnitude per frame).
        /// </summary>
        private Vector2 velocity;

        /// <summary>
        /// Initial velocity (used for reset).
        /// </summary>
        private Vector2 initialVelocity;

        /// <summary>
        /// Cumulative speed multiplier (persists across ball resets).
        /// Starts at 1.0, multiplied by speed increase factors.
        /// When ball resets, velocity = initialVelocity * currentSpeedMultiplier.
        /// This ensures speed increases are persistent and don't reset.
        /// </summary>
        private float currentSpeedMultiplier = 1.0f;

        /// <summary>
        /// Ball size (needed for radius calculations in bounce logic).
        /// </summary>
        private Vector2 ballSize;

        /// <summary>
        /// Current position of the ball.
        /// </summary>
        private Vector2 position;

        /// <summary>
        /// Initial position for reset.
        /// </summary>
        private Vector2 initialPosition;

        /// <summary>
        /// Track active collisions to prevent bouncing multiple times in same contact.
        /// Uses signal-based state: collision tracked when area_entered, removed when area_exited.
        /// </summary>
        private HashSet<Node> activeCollisions = new();
        #endregion

        #region Constructor
        public PhysicsComponent(Vector2 initialPosition, Vector2 ballSize, Vector2 initialVelocity)
        {
            this.position = initialPosition;
            this.initialPosition = initialPosition;
            this.ballSize = ballSize;
            this.velocity = initialVelocity;
            this.initialVelocity = initialVelocity;
        }
        #endregion

        #region Public API - Position & Velocity
        /// <summary>
        /// Gets the current position.
        /// </summary>
        public Vector2 GetPosition() => position;

        /// <summary>
        /// Gets the current velocity.
        /// </summary>
        public Vector2 GetVelocity() => velocity;

        /// <summary>
        /// Sets velocity (used by game rules, e.g., speed multipliers).
        /// </summary>
        public void SetVelocity(Vector2 newVelocity) => velocity = newVelocity;
        #endregion

        #region Public API - Update & Physics
        /// <summary>
        /// Updates position based on current velocity.
        /// Call this from Ball._Process(delta).
        /// </summary>
        /// <param name="delta">Delta time</param>
        /// <returns>New position after velocity is applied</returns>
        public Vector2 Update(float delta)
        {
            // Update position
            position += velocity * delta;

            // Handle wall collisions
            HandleWallBounceX();
            HandleWallBounceY();

            // Check out of bounds
            if (position.Y > Config.Ball.OutOfBoundsY)
            {
                OutOfBounds?.Invoke();
                // NOTE: ResetPhysics() is now called by Ball after OutOfBounds event
                // This allows Controller to check game state before resetting
            }

            return position;
        }

        /// <summary>
        /// Resets position and velocity to initial values.
        /// Preserves cumulative speed multiplier so speed increases persist across ball resets.
        /// velocity = initialVelocity * currentSpeedMultiplier
        /// </summary>
        public void ResetPhysics()
        {
            position = initialPosition;
            velocity = initialVelocity * currentSpeedMultiplier;
            activeCollisions.Clear();
            GD.Print($"Ball reset. Speed multiplier: {currentSpeedMultiplier}x");
        }
        #endregion

        #region Public API - Collision Handling
        /// <summary>
        /// Handles collision enter event. Call from Ball._OnAreaEntered().
        /// </summary>
        public void HandleCollisionEnter(Area2D area)
        {
            // Only process if this is a NEW contact (not already being tracked)
            if (activeCollisions.Contains(area)) return;
            activeCollisions.Add(area);

            if (area is Entities.Paddle paddle)
            {
                HandlePaddleCollision(paddle);
                BounceOccurred?.Invoke("paddle");
            }
            else if (area is Entities.Brick brick)
            {
                HandleBrickCollision(brick);
                BounceOccurred?.Invoke("brick");
            }
        }

        /// <summary>
        /// Handles collision exit event. Call from Ball._OnAreaExited().
        /// </summary>
        public void HandleCollisionExit(Area2D area)
        {
            activeCollisions.Remove(area);
        }
        #endregion

        #region Private - Wall Collisions
        private void HandleWallBounceX()
        {
            float ballRadius = ballSize.X / 2;

            // Bounce off left wall (inner edge at x=0)
            if (position.X + ballRadius < 0)
            {
                velocity.X = -velocity.X;
                WallHit?.Invoke();
            }
            // Bounce off right wall (inner edge at x=ViewportWidth)
            else if (position.X + ballRadius > Config.ViewportWidth)
            {
                velocity.X = -velocity.X;
                WallHit?.Invoke();
            }
        }

        private void HandleWallBounceY()
        {
            float ballRadius = ballSize.Y / 2;

            // Bounce off ceiling (inner edge at y=0)
            if (position.Y + ballRadius < 0)
            {
                velocity.Y = -velocity.Y;
                CeilingHit?.Invoke();  // Emit event for game rules
            }
        }
        #endregion

        #region Private - Paddle Collision
        private void HandlePaddleCollision(Entities.Paddle paddle)
        {
            // Calculate contact point on paddle surface
            Vector2 ballCenter = position + ballSize / 2;
            Vector2 paddleCenter = paddle.Position + paddle.GetSize() / 2;
            Vector2 paddleSize = paddle.GetSize();

            // Normalized contact position on paddle: -1 (left edge) to +1 (right edge)
            Vector2 delta = ballCenter - paddleCenter;
            float normalizedX = delta.X / (paddleSize.X / 2);
            normalizedX = Mathf.Clamp(normalizedX, -1f, 1f);

            // Arcade Breakout physics:
            // - Always bounce upward (Y velocity becomes negative/upward)
            // - Contact point determines horizontal angle (edges vs center)
            // - Maintain or slightly increase speed to feel responsive

            float speedMagnitude = velocity.Length();

            // Ensure strong upward velocity for authentic feel
            // Arcade Breakout: ball leaves paddle at steep angle (~45 degrees is common)
            velocity.Y = -Mathf.Abs(velocity.Y);

            // Angle control: contact point determines X velocity component
            // Edge hits (normalizedX near ±1) give steeper angles
            // Center hits (normalizedX near 0) go mostly straight up
            const float maxAngleFactor = 0.7f;  // Max horizontal velocity relative to speed magnitude
            velocity.X = speedMagnitude * maxAngleFactor * normalizedX;

            // Paddle velocity influence: if paddle is moving, impart some of that momentum
            // This makes catching with moving paddle feel more dynamic
            float paddleVelocityX = paddle.GetVelocityX();
            if (Mathf.Abs(paddleVelocityX) > 0.1f)
            {
                velocity.X += paddleVelocityX * 0.3f;  // 30% of paddle's horizontal momentum
            }

            GD.Print($"Paddle bounce: contact={normalizedX:F2}, paddleVel={paddleVelocityX:F2}, ballVel={velocity}");
        }
        #endregion

        #region Private - Brick Collision
        private void HandleBrickCollision(Entities.Brick brick)
        {
            float ballRadius = ballSize.X / 2;

            // Calculate penetration on each axis
            Vector2 ballCenter = position + ballSize / 2;
            Vector2 brickCenter = brick.Position + brick.GetBrickSize() / 2;
            Vector2 brickSize = brick.GetBrickSize();

            Vector2 delta = ballCenter - brickCenter;
            float overlapLeft = (brickSize.X / 2) + ballRadius + delta.X;
            float overlapRight = (brickSize.X / 2) + ballRadius - delta.X;
            float overlapTop = (brickSize.Y / 2) + ballRadius + delta.Y;
            float overlapBottom = (brickSize.Y / 2) + ballRadius - delta.Y;

            // Find the smallest overlap to determine which edge was hit
            float minOverlap = Mathf.Min(
                Mathf.Min(overlapLeft, overlapRight),
                Mathf.Min(overlapTop, overlapBottom)
            );

            if (minOverlap == overlapTop || minOverlap == overlapBottom)
            {
                // Hit top or bottom edge
                velocity.Y = -velocity.Y;
                GD.Print("Bounce off brick (vertical)");
            }
            else
            {
                // Hit left or right edge
                velocity.X = -velocity.X;
                GD.Print("Bounce off brick (horizontal)");
            }

            // Destroy the brick (emits signal for game rules)
            brick.Destroy();
        }
        #endregion

        #region Public API - Speed Modifiers
        /// <summary>
        /// Applies a speed multiplier to the current velocity.
        /// Used by game rules (e.g., speed increases after N hits).
        /// </summary>
        public void ApplySpeedMultiplier(float factor)
        {
            currentSpeedMultiplier *= factor;
            velocity = initialVelocity * currentSpeedMultiplier;
            GD.Print($"Speed multiplier applied: {factor}x, cumulative: {currentSpeedMultiplier}x, new velocity={velocity}");
        }
        #endregion
    }
}

