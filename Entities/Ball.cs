using Godot;

namespace Breakout.Entities
{
    /// <summary>
    /// A Ball class that represents the ball entity in the Breakout game.
    /// </summary>
    public partial class Ball : Area2D
    {

        #region Signals
        [Signal]
        public delegate void BallHitPaddleEventHandler();

        [Signal]
        public delegate void BallOutOfBoundsEventHandler();
        #endregion


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

            // assign CircleShape2D to the collision shape 
            // with a radius of 10
            collisionShape.Shape = new CircleShape2D {Radius = 10};

            // add the ball's collision shape to the scene tree
            AddChild(collisionShape);

            // Connect Area2D's area_entered signal
            AreaEntered += _OnAreaEntered;

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

            if (Position.Y < 10)
            {
                velocity.Y = -velocity.Y; // reverse Y velocity on ceiling collision
            }

            if (Position.Y > 600)
            {
                // Ball is out of bounds (below the paddle)
                EmitSignal(SignalName.BallOutOfBounds);

                // Reset ball position
                Position = new Vector2(400, 300);
                velocity = new Vector2(200, -200);
            }

        }

        public void _OnAreaEntered(Area2D area)
        {
            if (area is Paddle)
            {
                // Reverse Y velocity on paddle collision
                velocity.Y = -velocity.Y;

                // Emit signal for ball hitting paddle
                EmitSignal(SignalName.BallHitPaddle);
            }
        }
    }
}