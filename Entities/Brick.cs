using Breakout.Game;
using Godot;

namespace Breakout.Entities
{
    /// <summary>
    /// Brick entity — destructible on ball collision.
    /// Canonical Breakout: all bricks destroyed in one hit.
    /// No health component needed (follows original ruleset exactly).
    /// </summary>
    public partial class Brick : Area2D
    {
        #region Signals
        /// <summary>
        /// Triggered when the brick is destroyed.
        /// Passes the brick's unique ID for orchestration.
        /// </summary>
        [Signal]
        public delegate void BrickDestroyedEventHandler(int brickId);
        #endregion

        #region State
        /// <summary>
        /// Unique identifier for this brick in the grid.
        /// </summary>
        private int brickId;

        /// <summary>
        /// Brick size (needed for collision calculations).
        /// </summary>
        private Vector2 size;
        #endregion

        #region Constructor
        /// <summary>
        /// Creates a brick at the given position with the given size and color.
        /// </summary>
        /// <param name="id">Unique identifier for this brick in the grid</param>
        /// <param name="position">Top-left corner position</param>
        /// <param name="size">Width and height of the brick</param>
        /// <param name="color">Visual color of the brick</param>
        public Brick(int id, Vector2 position, Vector2 size, Color color)
        {
            brickId = id;
            this.size = size;
            Name = $"Brick_{id}";
            Position = position;

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
            CollisionLayer = Config.Brick.CollisionLayer;
            CollisionMask = Config.Brick.CollisionMask;
        }
        #endregion

    }
}
