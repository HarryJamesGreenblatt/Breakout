using Godot;

namespace Breakout.Entities
{
    /// <summary>
    /// A Brick class that represents a breakable brick in the Breakout game.
    /// </summary>
    public partial class Brick : Area2D
    {
        private int health = 1;

        public Brick(int brickId, Vector2 position)
        {
            Name = $"Brick_{brickId}";
            Position = position;
        }

        public override void _Ready()
        {
            // Create CollisionShape2D for the brick
            var collisionShape = new CollisionShape2D();
            // TODO: Assign shape and add to tree
        }

        public void TakeDamage()
        {
            health--;
            if (health <= 0)
            {
                QueueFree();
            }
        }
    }
}
