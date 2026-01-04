using Godot;


namespace Breakout.Entities
{
    public partial class Paddle : Area2D
    {
        public Paddle()
        {
            Name = "Paddle";
            Position = new Vector2(400, 550);
        }

        public override void _Ready()
        {
            // Create a CollisionShape2D for the paddle
            var collisionShape = new CollisionShape2D();

            // Connect the body_entered signal to detect collisions with the ball

        }

        public override void _Process(double delta) 
        {
            // Handle input for moving the paddle left and right
            var input = Input.GetAxis("ui_left", "ui_right");

            // Update position based on input
            Position += new Vector2((float)(input * 600 * delta), 0);

            // Constrain within bounds
            Position = new Vector2(
                Mathf.Clamp(Position.X, 50, 750),
                Position.Y
            );
        }
    }
}
