using Breakout.Game;
using Godot;

namespace Breakout.Entities
{
    /// <summary>
    /// Paddle entity controlled by player input (left/right arrow keys).
    /// Uses GameConfig for physics constants and bounds.
    /// Provides Shrink() action for GameStateComponent to call.
    /// </summary>
    public partial class Paddle : Area2D
    {
        private Vector2 size;
        private bool inputEnabled = true;
        private float velocityX = 0f;  // Track horizontal velocity for paddle momentum transfer
        private float speedMultiplier = 1f;  // Track cumulative speed increases


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

        public override void _Ready()
        {
        }

        /// <summary>
        /// Update paddle position based on player input each frame.
        /// </summary>
        /// <param name="delta"></param>
        public override void _Process(double delta)
        {
            if (!inputEnabled) return;  // Skip input processing if disabled

            // Handle input
            var input = Input.GetAxis("ui_left", "ui_right");
            
            // Calculate and store velocity for physics momentum transfer (with speed multiplier)
            velocityX = (float)(Config.Paddle.Speed * speedMultiplier * input);
            
            // Update position
            Position += new Vector2(velocityX * (float)delta, 0);
            
            // Constrain to bounds based on current paddle size (not cached original size)
            float minX = 0;
            float maxX = Config.ViewportWidth - size.X;  // Use current size, not cached size
            Position = new Vector2(
                Mathf.Clamp(Position.X, minX, maxX),
                Position.Y
            );
        }

        /// <summary>

        /// <summary>
        /// Returns the horizontal velocity of the paddle.
        /// Used by PhysicsComponent to impart paddle momentum to the ball.
        /// </summary>
        public float GetVelocityX() => velocityX;
        /// Returns the size of the paddle.
        /// </summary>
        public Vector2 GetSize() => size;

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
        /// Shrinks the paddle to 50% of its original width.
        /// Called by Orchestrator when GameStateComponent emits PaddleShrinkRequired.
        /// This is the canonical Breakout rule: paddle shrinks after breaking red row and hitting ceiling.
        /// </summary>
        public void Shrink()
        {
            float originalWidth = size.X;
            size = new Vector2(size.X * 0.5f, size.Y);
            float widthDifference = originalWidth - size.X;
            
            // Move paddle right by half the width difference to center the shrink
            Position += new Vector2(widthDifference / 2, 0);
            
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

            GD.Print($"Paddle shrunk to {size.X}x{size.Y}");
        }

        /// <summary>
        /// Enable or disable paddle input handling.
        /// Called when game ends to prevent further paddle movement.
        /// </summary>
        public void SetInputEnabled(bool enabled)
        {
            inputEnabled = enabled;
        }
    }
}