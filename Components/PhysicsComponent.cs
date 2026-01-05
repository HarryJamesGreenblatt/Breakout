using Godot;
using Breakout.Game;

namespace Breakout.Components
{
    /// <summary>
    /// PhysicsComponent encapsulates ball physics state and bounce logic.
    /// Follows Nystrom's Component pattern: physics state and rules are decoupled from the entity.
    /// 
    /// Responsibilities:
    /// - Velocity state management
    /// - Bounce calculations based on surface type and contact geometry
    /// - Speed modifiers (for game rules like speed increases after N hits)
    /// - Position updates based on velocity
    /// 
    /// This component is owned by Ball; Ball delegates physics to the component.
    /// </summary>
    public class PhysicsComponent
    {
        #region State
        /// <summary>
        /// Current velocity (direction and magnitude per frame).
        /// </summary>
        public Vector2 Velocity { get; set; }

        /// <summary>
        /// Initial velocity (used for reset).
        /// </summary>
        private Vector2 initialVelocity;

        /// <summary>
        /// Ball size (needed for radius calculations in bounce logic).
        /// </summary>
        private Vector2 ballSize;
        #endregion

        #region Constructor
        public PhysicsComponent(Vector2 initialVelocity, Vector2 ballSize)
        {
            this.initialVelocity = initialVelocity;
            this.Velocity = initialVelocity;
            this.ballSize = ballSize;
        }
        #endregion

        #region Position & Velocity Updates
        /// <summary>
        /// Updates position based on current velocity.
        /// </summary>
        /// <param name="currentPosition">Current position of the ball</param>
        /// <param name="delta">Delta time</param>
        /// <returns>New position after velocity is applied</returns>
        public Vector2 UpdatePosition(Vector2 currentPosition, float delta)
        {
            return currentPosition + (Velocity * delta);
        }
        #endregion

        #region Wall Collisions
        /// <summary>
        /// Handles bounce off left/right walls.
        /// Walls are positioned outside viewport at x=-WallThickness and x=ViewportWidth.
        /// </summary>
        /// <param name="ballPosition">Current position of the ball (top-left corner)</param>
        public void HandleWallBounceX(Vector2 ballPosition)
        {
            float ballRadius = ballSize.X / 2;

            // Bounce off left wall (inner edge at x=0)
            if (ballPosition.X + ballRadius < 0)
            {
                var vel = Velocity;
                vel.X = -vel.X;
                Velocity = vel;
                GD.Print("Bounce off left wall");
            }
            // Bounce off right wall (inner edge at x=ViewportWidth)
            else if (ballPosition.X + ballRadius > Config.ViewportWidth)
            {
                var vel = Velocity;
                vel.X = -vel.X;
                Velocity = vel;
                GD.Print("Bounce off right wall");
            }
        }

        /// <summary>
        /// Handles bounce off top wall.
        /// Top wall is positioned outside viewport at y=-WallThickness.
        /// </summary>
        /// <param name="ballPosition">Current position of the ball (top-left corner)</param>
        public void HandleWallBounceY(Vector2 ballPosition)
        {
            float ballRadius = ballSize.Y / 2;

            // Bounce off ceiling (inner edge at y=0)
            if (ballPosition.Y + ballRadius < 0)
            {
                var vel = Velocity;
                vel.Y = -vel.Y;
                Velocity = vel;
                GD.Print("Bounce off top wall");
            }
        }
        #endregion

        #region Paddle Collision
        /// <summary>
        /// Handles bounce off paddle.
        /// Currently simple Y-reversal; future enhancement: angle varies based on paddle position and movement.
        /// </summary>
        public void HandlePaddleBounce()
        {
            var vel = Velocity;
            vel.Y = -vel.Y;
            Velocity = vel;
            GD.Print("Bounce off paddle");
        }

        /// <summary>
        /// Applies angle variation based on where the ball hit the paddle.
        /// Ball hits center of paddle → straight bounce upward.
        /// Ball hits left edge of paddle → bounces left and upward.
        /// Ball hits right edge of paddle → bounces right and upward.
        /// 
        /// This creates directional steering from the paddle.
        /// Boost velocity to make paddle bounces feel impactful.
        /// </summary>
        /// <param name="ballCenter">Center position of the ball</param>
        /// <param name="paddleCenter">Center position of the paddle</param>
        /// <param name="paddleSize">Dimensions of the paddle</param>
        public void ApplyPaddleAngledBounce(Vector2 ballCenter, Vector2 paddleCenter, Vector2 paddleSize)
        {
            // Delta between ball and paddle centers (normalized to -1...+1)
            Vector2 delta = ballCenter - paddleCenter;
            float normalizedX = delta.X / (paddleSize.X / 2);
            normalizedX = Mathf.Clamp(normalizedX, -1f, 1f);

            // Direct velocity manipulation for stronger steering effect
            float speedMagnitude = Velocity.Length();

            var vel = Velocity;
            
            // Ensure upward bounce: always make Y negative (upward in Godot)
            vel.Y = -Mathf.Abs(vel.Y);

            // Impart horizontal steering based on paddle contact point
            // Left edge: -maxSteer * speedMagnitude
            // Right edge: +maxSteer * speedMagnitude
            // Center: no additional steering
            const float maxSteerFactor = 0.6f;  // How much to steer (0.6 = 60% speed added horizontally)
            vel.X = speedMagnitude * maxSteerFactor * normalizedX;

            // Boost velocity magnitude for impactful bounce (10% faster)
            const float bounceBoost = 1.1f;
            vel = vel.Normalized() * (speedMagnitude * bounceBoost);

            Velocity = vel;
            GD.Print($"Paddle bounce: contact={normalizedX:F2} (left=-1, center=0, right=+1), vel={Velocity}");
        }
        #endregion

        #region Brick Collision
        /// <summary>
        /// Handles bounce off brick using penetration heuristic.
        /// Determines which edge (top, bottom, left, right) was hit based on smallest overlap.
        /// </summary>
        /// <param name="ballCenter">Center of the ball</param>
        /// <param name="brickCenter">Center of the brick</param>
        /// <param name="brickSize">Size of the brick</param>
        public void HandleBrickBounce(Vector2 ballCenter, Vector2 brickCenter, Vector2 brickSize)
        {
            float ballRadius = ballSize.X / 2;

            // Calculate penetration on each axis
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
                var vel = Velocity;
                vel.Y = -vel.Y;
                Velocity = vel;
                GD.Print("Bounce off brick (vertical)");
            }
            else
            {
                // Hit left or right edge
                var vel = Velocity;
                vel.X = -vel.X;
                Velocity = vel;
                GD.Print("Bounce off brick (horizontal)");
            }
        }
        #endregion

        #region Speed Modifiers
        /// <summary>
        /// Applies a speed multiplier to the current velocity.
        /// Used by game rules (e.g., speed increases after N hits).
        /// </summary>
        /// <param name="factor">Multiplier (1.1 = 10% faster)</param>
        public void ApplySpeedMultiplier(float factor)
        {
            Velocity *= factor;
            GD.Print($"Speed multiplier applied: {factor}x, new velocity={Velocity}");
        }

        /// <summary>
        /// Resets velocity to initial value.
        /// Called when ball resets after out-of-bounds.
        /// </summary>
        public void ResetVelocity()
        {
            Velocity = initialVelocity;
            GD.Print("Velocity reset to initial");
        }
        #endregion
    }
}
