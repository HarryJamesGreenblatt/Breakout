using Breakout.Game;
using Godot;

namespace Breakout.Entities
{
    /// <summary>
    /// Paddle entity controlled by player input (left/right arrow keys).
    /// Uses GameConfig for physics constants and bounds.
    /// </summary>
    public partial class Paddle : Area2D
    {
        private Vector2 size;

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
            // Handle input
            var input = Input.GetAxis("ui_left", "ui_right");
            
            // Update position
            Position += new Vector2((float)(Config.Paddle.Speed * input * delta), 0);
            
            // Constrain to bounds
            Position = new Vector2(
                Mathf.Clamp(Position.X, Config.Paddle.MinX, Config.Paddle.MaxX),
                Position.Y
            );
        }

        /// <summary>
        /// Returns the size of the paddle.
        /// </summary>
        public Vector2 GetSize() => size;
    }
}
