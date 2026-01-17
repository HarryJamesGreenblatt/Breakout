using Breakout.Game;
using Godot;

namespace Breakout.Entities
{
    /// <summary>
    /// Paddle entity — player-controlled horizontal platform.
    /// 
    /// Following Nystrom's Component pattern:
    /// - Paddle owns input handling, movement constraints, and visual updates
    /// - Exposes velocity for PhysicsComponent to influence ball bounce angles
    /// - Executes action methods (Shrink, ApplySpeedMultiplier) when game rules require
    /// 
    /// Canonical Breakout behavior:
    /// - Left/right arrow key input for horizontal movement
    /// - Constrained within viewport bounds (0 to ViewportWidth - paddleWidth)
    /// - Shrinks to 60% width after red row breakthrough + ceiling hit
    /// - Speed increases 10% to compensate for ball speed increases (maintains fairness)
    /// 
    /// Public API:
    /// - GetVelocityX(): exposes current velocity for physics calculations
    /// - GetSize(): provides current dimensions (changes after shrink)
    /// - ApplySpeedMultiplier(factor): increases movement speed
    /// - Shrink(): reduces width by 40% (deferred execution to avoid physics conflicts)
    /// - SetInputEnabled(enabled): disables input on game over
    /// </summary>
    public partial class Paddle : Area2D
    {
        #region State
        /// <summary>
        /// Current dimensions of the paddle (width × height).
        /// Changes when Shrink() is called.
        /// </summary>
        private Vector2 size;

        /// <summary>
        /// Whether player input is processed.
        /// Disabled on game over to freeze paddle.
        /// </summary>
        private bool inputEnabled = true;

        /// <summary>
        /// Current horizontal velocity in pixels/second.
        /// Exposed to PhysicsComponent for paddle momentum transfer to ball.
        /// </summary>
        private float velocityX = 0f;

        /// <summary>
        /// Cumulative speed multiplier applied to paddle movement.
        /// Increases 10% at each ball speed milestone to maintain fairness.
        /// </summary>
        private float speedMultiplier = 1f;
        #endregion

        #region Constructor
        /// <summary>
        /// Constructs a paddle entity at the given position.
        /// Creates collision shape and visual representation.
        /// </summary>
        /// <param name="position">Top-left corner position</param>
        /// <param name="size">Width and height</param>
        /// <param name="color">Visual color</param>
        public Paddle(Vector2 position, Vector2 size, Color color)
        {
            Name = "Paddle";
            Position = position;
            this.size = size;

            // Collision shape offset to center of the visual rect
            var collisionShape = new CollisionShape2D();
            collisionShape.Position = size / 2;
            collisionShape.Shape = new RectangleShape2D { Size = size };
            AddChild(collisionShape);

            // Visual representation
            var visual = new ColorRect
            {
                Size = size,
                Color = color
            };
            AddChild(visual);

            // Collision setup from config
            CollisionLayer = Config.Paddle.CollisionLayer;
            CollisionMask = Config.Paddle.CollisionMask;
        }
        #endregion

        #region Game Behavior
        /// <summary>
        /// Initialize paddle (currently no setup required).
        /// </summary>
        public override void _Ready()
        {
        }

        /// <summary>
        /// Updates paddle position each frame based on player input.
        /// 
        /// Behavior:
        /// - Polls left/right arrow keys (ui_left, ui_right)
        /// - Calculates velocity with current speed multiplier
        /// - Updates position by velocity × delta
        /// - Constrains position within viewport bounds (0 to ViewportWidth - currentWidth)
        /// 
        /// Note: Uses current size for bounds checking (size changes after Shrink()).
        /// </summary>
        /// <param name="delta">Time since last frame in seconds</param>
        public override void _Process(double delta)
        {
            if (!inputEnabled) return;  // Skip input processing if disabled

            // Poll input axis (-1 for left, +1 for right, 0 for no input)
            var input = Input.GetAxis("ui_left", "ui_right");
            
            // Calculate and store velocity for physics momentum transfer (with speed multiplier)
            velocityX = (float)(Config.Paddle.Speed * speedMultiplier * input);
            
            // Update position based on velocity
            Position += new Vector2(velocityX * (float)delta, 0);
            
            // Constrain to viewport bounds (uses current size, not cached size)
            float minXBound = 0;
            float maxXBound = Config.ViewportWidth - size.X;
            Position = new Vector2(
                Mathf.Clamp(Position.X, minXBound, maxXBound),
                Position.Y
            );
        }
        #endregion

        #region Public API

        /// <summary>
        /// Returns the horizontal velocity of the paddle.
        /// Used by PhysicsComponent to impart paddle momentum to the ball.
        /// </summary>
        public float GetVelocityX() => velocityX;

        /// <summary>
        /// Returns the size of the paddle.
        /// </summary>
        public Vector2 GetSize() => size;

        /// <summary>
        /// Set the paddle size (used by components to resize paddle).
        /// Updates collision shape and visual representation.
        /// </summary>
        public void SetSize(Vector2 newSize)
        {
            size = newSize;
            
            // Update visual
            var visual = GetChild(1) as ColorRect;  // ColorRect is second child (after CollisionShape2D)
            if (visual != null)
            {
                visual.Size = size;
            }

            // Update collision shape - keep position centered
            var collisionShape = GetChild(0) as CollisionShape2D;  // CollisionShape2D is first child
            if (collisionShape != null)
            {
                collisionShape.Shape = new RectangleShape2D { Size = size };
                collisionShape.Position = size / 2;  // Center the collision shape
            }
        }

        /// <summary>
        /// Applies a speed multiplier to paddle movement.
        /// Called when ball speed increases to keep paddle responsive.
        /// </summary>
        public void ApplySpeedMultiplier(float factor)
        {
            speedMultiplier *= factor;
            GD.Print($"Paddle speed multiplier applied: {factor}x, cumulative: {speedMultiplier}x");
        }

        /// <summary>
        /// Enable or disable paddle input handling.
        /// Called when game ends to prevent further paddle movement.
        /// </summary>
        public void SetInputEnabled(bool enabled)
        {
            inputEnabled = enabled;
        }

        /// <summary>
        /// Reset paddle to initial state for game restart.
        /// Restores original size, resets position to center, and clears speed multiplier.
        /// Uses Config values for initial state (config is the source of truth).
        /// </summary>
        public void ResetForGameRestart()
        {
            // Keep paddle at its current position so transition can ease it smoothly to center
            // Don't reposition it here - the paddle will move from wherever it is to center
            // during the transition animation
            
            size = Config.Paddle.Size;
            speedMultiplier = 1f;
            velocityX = 0f;
            inputEnabled = true;

            // Update visual and collision shape
            SetSize(Config.Paddle.Size);

            GD.Print($"Paddle reset to initial state from Config");

        }

        /// <summary>
        /// Get the target center position for transitions.
        /// Exposes Config value for TransitionComponent to use.
        /// </summary>
        public Vector2 GetCenterPosition() => Config.Paddle.Position;

        #endregion
    }
}