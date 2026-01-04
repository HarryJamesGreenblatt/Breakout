using Godot;

namespace Breakout
{
    /// <summary>
    /// Centralized game configuration. All magic numbers, constants, and tunable values here.
    /// </summary>
    public static class GameConfig
    {
        #region Viewport Dimensions
        public const float ViewportWidth = 800f;
        public const float ViewportHeight = 600f;
        #endregion

        #region Infrastructure
        public const float WallThickness = 20f;

        public static class Walls
        {
            public static readonly Color Color = new Color(0.5f, 0.5f, 0.5f, 1);
            public const int CollisionLayer = 1;
            public const int CollisionMask = 1;
        }
        #endregion

        #region Game Entities
        public static class Paddle
        {
            public static readonly Vector2 Position = new Vector2(400, 550);
            public static readonly Vector2 Size = new Vector2(100, 20);
            public static readonly Color Color = new Color(0, 1, 0, 1);
            public const float Speed = 600f;
            public const float MinX = 50f;
            public const float MaxX = 750f;
            public const int CollisionLayer = 1;
            public const int CollisionMask = 1;
        }

        public static class Ball
        {
            public static readonly Vector2 Position = new Vector2(400, 300);
            public static readonly Vector2 Size = new Vector2(20, 20);
            public static readonly Vector2 Velocity = new Vector2(200, -200);
            public static readonly Color Color = new Color(1, 1, 0, 1);
            public const float BounceMarginX = 10f;
            public const float BounceMarginTop = 10f;
            public const float OutOfBoundsY = 600f;
            public const int CollisionLayer = 1;
            public const int CollisionMask = 1;
        }
        #endregion
    }
}
