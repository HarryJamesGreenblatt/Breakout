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

            // Wall collisions now handled via Area2D collision events
            // (no manual boundary checks needed)

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
        /// Adds slight randomness to launch angle so ball doesn't always go the same direction.
        /// velocity = initialVelocity * currentSpeedMultiplier (with angle variation)
        /// </summary>
        public void ResetPhysics()
        {
            position = initialPosition;
            
            // Get speed magnitude from current multiplier
            float speed = initialVelocity.Length() * currentSpeedMultiplier;
            
            // Launch angle constrained between 60° and 120° (mostly downward toward paddle)
            // In Godot: 0° = right, 90° = down, 180° = left, 270° = up
            float minAngleDegrees = 60f;
            float maxAngleDegrees = 120f;
            float randomAngleDegrees = Mathf.Lerp(minAngleDegrees, maxAngleDegrees, (float)GD.Randf());
            float angleRadians = Mathf.DegToRad(randomAngleDegrees);
            
            // Reconstruct velocity with constrained angle but same speed
            velocity = new Vector2(
                Mathf.Cos(angleRadians) * speed,
                Mathf.Sin(angleRadians) * speed
            );
            
            activeCollisions.Clear();
            GD.Print($"Ball reset. Speed multiplier: {currentSpeedMultiplier}x, launch angle: {randomAngleDegrees:F1}°");
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
            else if (area.Name.ToString().Contains("Wall"))  // Detect walls by name (TopWall, LeftWall, RightWall)
            {
                HandleWallCollision(area);
                WallHit?.Invoke();
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
            const float maxAngleFactor = 0.7f;  // Max horizontal velocity relative to speed magnitude
            
            // Calculate angle components while preserving total speed magnitude
            float horizontalComponent = speedMagnitude * maxAngleFactor * normalizedX;
            float verticalMagnitude = Mathf.Sqrt((speedMagnitude * speedMagnitude) - (horizontalComponent * horizontalComponent));
            
            velocity.X = horizontalComponent;
            velocity.Y = -verticalMagnitude;  // Negative = upward

            GD.Print($"Paddle bounce: contact={normalizedX:F2}, speed={speedMagnitude:F2}, ballVel={velocity}, magnitude={velocity.Length():F2}");
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

        /// <summary>
        /// Handles collision with walls. Bounces ball appropriately based on wall type.
        /// </summary>
        private void HandleWallCollision(Area2D wall)
        {
            float ballRadius = ballSize.X / 2;
            string wallName = wall.Name;

            if (wallName == "TopWall")
            {
                // Bounce off ceiling
                velocity.Y = -velocity.Y;
                CeilingHit?.Invoke();
                GD.Print("Bounce off ceiling");
            }
            else if (wallName == "LeftWall")
            {
                // Bounce off left wall
                velocity.X = -velocity.X;
                GD.Print("Bounce off left wall");
            }
            else if (wallName == "RightWall")
            {
                // Bounce off right wall
                velocity.X = -velocity.X;
                GD.Print("Bounce off right wall");
            }
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

