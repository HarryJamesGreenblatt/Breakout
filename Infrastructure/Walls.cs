using Godot;

namespace Breakout.Infrastructure
{
    /// <summary>
    /// Container for immobile boundary walls (left, right, top).
    /// Creates walls programmatically with collision and visual components.
    /// </summary>
    public partial class Walls : Node
    {
        #region Wall Definition
        /// <summary>
        /// A single wall with collision and visual representation rendered as a ColorRect.
        /// </summary>
        private partial class Wall : StaticBody2D
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="Wall"/> class.
            /// </summary>
            /// <param name="name"></param>
            /// <param name="position"></param>
            /// <param name="size"></param>
            /// <param name="collisionOffset"></param>
            /// <param name="color"></param>
            public Wall(string name, Vector2 position, Vector2 size, Vector2 collisionOffset, Color color)
            {
                Name = name;
                Position = position;

                // Collision shape with explicit offset parameter
                var collisionShape = new CollisionShape2D();
                collisionShape.Position = collisionOffset;
                collisionShape.Shape = new RectangleShape2D { Size = size };
                AddChild(collisionShape);

                // Visual representation
                var visual = new ColorRect
                {
                    Position = Vector2.Zero,
                    Size = size,
                    Color = color
                };
                AddChild(visual);

                // Collision setup from config
                CollisionLayer = GameConfig.Walls.CollisionLayer;
                CollisionMask = GameConfig.Walls.CollisionMask;
            }
        }
        #endregion

        #region Game Behavior
        public override void _Ready()
        {
            // Create boundary walls using GameConfig
            // Walls positioned outside viewport; inner edges define play boundary
            var topWallSize = new Vector2(GameConfig.ViewportWidth, GameConfig.WallThickness);
            var verticalWallSize = new Vector2(GameConfig.WallThickness, GameConfig.ViewportHeight);

            // Top wall: positioned above viewport (y = -thickness), inner edge at y=0
            var topWall = new Wall("TopWall", new Vector2(0, -GameConfig.WallThickness), topWallSize, topWallSize / 2, GameConfig.Walls.Color);
            
            // Left wall: positioned left of viewport (x = -thickness), inner edge at x=0
            var leftWall = new Wall("LeftWall", new Vector2(-GameConfig.WallThickness, 0), verticalWallSize, verticalWallSize / 2, GameConfig.Walls.Color);
            
            // Right wall: positioned at viewport right edge, inner edge at x=ViewportWidth
            var rightWall = new Wall("RightWall", new Vector2(GameConfig.ViewportWidth, 0), verticalWallSize, verticalWallSize / 2, GameConfig.Walls.Color);

            AddChild(topWall);
            AddChild(leftWall);
            AddChild(rightWall);
        }
        #endregion
    }
}