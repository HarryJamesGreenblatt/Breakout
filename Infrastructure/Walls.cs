using Godot;

namespace Breakout.Infrastructure
{
    /// <summary>
    /// Container for immobile boundary walls (left, right, top).
    /// Creates walls programmatically with collision and visual components.
    /// </summary>
    public partial class Walls : Node
    {
        private partial class Wall : StaticBody2D
        {
            public Wall(string name, Vector2 position, Vector2 size, Color color)
            {
                Name = name;
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
                CollisionLayer = GameConfig.Walls.CollisionLayer;
                CollisionMask = GameConfig.Walls.CollisionMask;
            }
        }

        public override void _Ready()
        {
            // Create boundary walls using GameConfig
            var topWall = new Wall("TopWall", new Vector2(0, 0), new Vector2(GameConfig.ViewportWidth, GameConfig.WallThickness), GameConfig.Walls.Color);
            var leftWall = new Wall("LeftWall", new Vector2(0, 0), new Vector2(GameConfig.WallThickness, GameConfig.ViewportHeight), GameConfig.Walls.Color);
            var rightWall = new Wall("RightWall", new Vector2(GameConfig.ViewportWidth - GameConfig.WallThickness, 0), new Vector2(GameConfig.WallThickness, GameConfig.ViewportHeight), GameConfig.Walls.Color);

            AddChild(topWall);
            AddChild(leftWall);
            AddChild(rightWall);
        }
    }
}