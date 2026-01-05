using Godot;

namespace Breakout.Models
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
}
