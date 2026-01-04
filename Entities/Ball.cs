using Godot;

namespace Breakout.Entities
{
    /// <summary>
    /// A Ball class that represents the ball entity in the Breakout game.
    /// </summary>
    public partial class Ball : Area2D
    {
        private Vector2 velocity = new Vector2(200, -200);
        public Ball()
        {
            Name = "Ball";
            Position = new Vector2(400, 300);
        }
        public override void _Ready()
        {
            // create CollisionShape2D for the ball
            var collisionShape = new CollisionShape2D();

            // connect body_entered signal to detect collisions with walls and paddle


        }
        public override void _Process(double delta)
        {
            // update ball position based on velocity
            Position += velocity * (float)delta;

            // Constrain within bounds
            if (Position.X < 10 || Position.X > 790)
            {
                velocity.X = -velocity.X; // reverse X velocity on wall collision
            }

        }
    }
}