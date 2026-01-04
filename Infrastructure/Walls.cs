using Godot;

namespace Breakout.Infrastructure
{
    /// <summary>
    /// A Walls class that represents the walls of the Breakout game area.
    /// </summary>
    public partial class Walls : Node
    {

        /// <summary>
        /// Defines a Wall as a StaticBody2D representing a wall in the game.
        /// Accepts parameters for nameposition, and size.
        /// </summary>
        public partial class Wall : StaticBody2D
        {
            public Wall(string name, Vector2 position, Vector2 size)
            {
                Name = name;
                Position = position;
                var collisionShape = new CollisionShape2D();
                var rectangleShape = new RectangleShape2D();
                rectangleShape.Size = size;
                collisionShape.Shape = rectangleShape;
                AddChild(collisionShape);
            }

        }

        public override void _Ready()
        {
            // Create walls for the game area
            var leftWall = new Wall("LeftWall", new Vector2(0, 300), new Vector2(20, 600));
            var rightWall = new Wall("RightWall", new Vector2(800, 300), new Vector2(20, 600));
            var topWall = new Wall("TopWall", new Vector2(400, 0), new Vector2(800, 20));

            AddChild(leftWall);
            AddChild(rightWall);
            AddChild(topWall);

        }
    }
}