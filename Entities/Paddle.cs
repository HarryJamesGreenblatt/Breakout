using Godot;

namespace Breakout.Entities
{
    /// <summary>
    /// Paddle entity controlled by player input (left/right arrow keys).
    /// Uses GameConfig for physics constants and bounds.
    /// </summary>
    public partial class Paddle : Area2D
    {
        public Paddle(Vector2 position, Vector2 size, Color color)
        {
            Name = "Paddle";
            Position = position;

            // Collision shape
            var collisionShape = new CollisionShape2D();
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
            CollisionLayer = GameConfig.Paddle.CollisionLayer;
            CollisionMask = GameConfig.Paddle.CollisionMask;
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
            Position += new Vector2((float)(GameConfig.Paddle.Speed * input * delta), 0);
            
            // Constrain to bounds
            Position = new Vector2(
                Mathf.Clamp(Position.X, GameConfig.Paddle.MinX, GameConfig.Paddle.MaxX),
                Position.Y
            );
        }
    }
}
