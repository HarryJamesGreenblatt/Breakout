using Godot;

namespace Breakout.Game
{
    /// <summary>
    /// Enumeration of brick colors matching canonical Breakout design.
    /// Ordered top-to-bottom as they appear in the grid (Red at top, Yellow at bottom).
    /// </summary>
    public enum BrickColor
    {
        Red,
        Orange,
        Green,
        Yellow
    }

    /// <summary>
    /// Configuration for a brick color: visual appearance and game properties.
    /// </summary>
    public readonly struct BrickColorConfig
    {
        public BrickColor Color { get; }
        public Godot.Color VisualColor { get; }
        public int Points { get; }

        public BrickColorConfig(BrickColor color, Godot.Color visualColor, int points)
        {
            Color = color;
            VisualColor = visualColor;
            Points = points;
        }
    }

    /// <summary>
    /// Brick color definitions and lookup.
    /// </summary>
    public static class BrickColorDefinitions
    {
        /// <summary>
        /// Get configuration for a given brick color.
        /// </summary>
        public static BrickColorConfig GetConfig(BrickColor color)
        {
            return color switch
            {
                BrickColor.Red => new BrickColorConfig(
                    BrickColor.Red,
                    new Godot.Color(1, 0, 0, 1),      // Red
                    7                                   // Original Breakout: Red = 7 points
                ),
                BrickColor.Orange => new BrickColorConfig(
                    BrickColor.Orange,
                    new Godot.Color(1, 0.65f, 0, 1),  // Orange
                    5                                   // Original Breakout: Orange = 5 points
                ),
                BrickColor.Green => new BrickColorConfig(
                    BrickColor.Green,
                    new Godot.Color(0, 1, 0, 1),      // Green
                    3                                   // Original Breakout: Green = 3 points
                ),
                BrickColor.Yellow => new BrickColorConfig(
                    BrickColor.Yellow,
                    new Godot.Color(1, 1, 0, 1),      // Yellow
                    1                                   // Original Breakout: Yellow = 1 point
                ),
                _ => throw new System.ArgumentException($"Unknown brick color: {color}")
            };
        }

        /// <summary>
        /// Get the brick color for a given grid row (top-to-bottom).
        /// Grid has 8 rows: 2 rows Red, 2 Orange, 2 Green, 2 Yellow.
        /// </summary>
        public static BrickColor GetColorForRow(int rowIndex)
        {
            return rowIndex switch
            {
                0 or 1 => BrickColor.Red,
                2 or 3 => BrickColor.Orange,
                4 or 5 => BrickColor.Green,
                6 or 7 => BrickColor.Yellow,
                _ => throw new System.ArgumentException($"Invalid row index: {rowIndex}")
            };
        }
    }
}
