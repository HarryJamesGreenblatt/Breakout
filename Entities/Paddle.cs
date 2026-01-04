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
            // create a CollisionShape2D for the paddle
            var collisionShape = new CollisionShape2D();

            // assign a RectangleShape2D to the collision shape
            collisionShape.Shape = new RectangleShape2D { Size = new Vector2(100, 20) };

            // add the paddle's collision shape to the scene tree
            AddChild(collisionShape);

            // Connect signals

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
