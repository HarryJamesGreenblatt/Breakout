using Godot;
using Breakout.Entities;
using Breakout.Models;
using Breakout.Utilities;
using System;
using System.Collections.Generic;

namespace Breakout.Components
{
    /// <summary>
    /// BrickGridComponent — owns the brick grid and manages brick lifecycle.
    /// 
    /// Following Nystrom's Component pattern:
    /// - Component owns state (brick grid dictionary)
    /// - Component is a plain C# class (NOT a Node)
    /// - Component emits C# events for state changes
    /// - Orchestrator coordinates by listening to events
    /// 
    /// Responsibilities:
    /// - Create and manage brick grid
    /// - Track active bricks
    /// - Handle brick destruction (remove from grid, compute brick color)
    /// - Emit BrickDestroyed event with color for scoring/rules
    /// </summary>
    public partial class BrickGridComponent
    {
        #region State
        /// <summary>
        /// Dictionary mapping brick ID to brick entity.
        /// </summary>
        private Dictionary<int, Brick> brickGrid = new();
        #endregion

        #region Events
        /// <summary>
        /// Emitted when a brick is destroyed.
        /// Passes the brick color for game rules (speed increases, scoring).
        /// </summary>
        public event Action<Models.BrickColor> BrickDestroyedWithColor;

        /// <summary>
        /// Emitted when grid is instantiated (for UI/debug).
        /// </summary>
        public event Action<int> GridInstantiated;
        #endregion

        #region Public API
        /// <summary>
        /// Instantiates the brick grid.
        /// Called by Orchestrator during setup.
        /// </summary>
        public void InstantiateGrid(Godot.Node parentNode)
        {
            int brickId = 0;
            Vector2 gridStart = Breakout.Game.Config.Brick.GridStartPosition;

            for (int row = 0; row < Breakout.Game.Config.Brick.GridRows; row++)
            {
                for (int col = 0; col < Breakout.Game.Config.Brick.GridColumns; col++)
                {
                    // Calculate brick position
                    Vector2 position = gridStart + new Vector2(
                        col * Breakout.Game.Config.Brick.GridSpacingX,
                        row * Breakout.Game.Config.Brick.GridSpacingY
                    );

                    // Get brick color for this row and fetch its config
                    BrickColor brickColorEnum = BrickColorUtility.GetColorForRow(row);
                    BrickColorConfig colorConfig = BrickColorUtility.GetConfig(brickColorEnum);

                    // Create and add brick to the scene
                    var brick = new Brick(brickId, position, Breakout.Game.Config.Brick.Size, colorConfig.VisualColor);
                    parentNode.AddChild(brick);

                    // Store brick in the dictionary
                    brickGrid[brickId] = brick;

                    // Connect brick destruction signal
                    brick.BrickDestroyed += (id) => OnBrickDestroyed(id);

                    brickId++;
                }
            }

            GridInstantiated?.Invoke(brickId);
            GD.Print($"Brick grid instantiated: {brickId} bricks");
        }

        /// <summary>
        /// Query remaining brick count.
        /// </summary>
        public int GetRemainingBrickCount() => brickGrid.Count;
        #endregion

        #region Private Methods
        /// <summary>
        /// Handles brick destruction.
        /// Removes brick from grid and emits event with color.
        /// </summary>
        private void OnBrickDestroyed(int brickId)
        {
            if (brickGrid.ContainsKey(brickId))
            {
                // Compute brick row to determine color
                int gridColumns = Breakout.Game.Config.Brick.GridColumns;
                int brickRow = brickId / gridColumns;
                BrickColor color = BrickColorUtility.GetColorForRow(brickRow);

                // Remove from grid
                brickGrid.Remove(brickId);

                GD.Print($"Brick {brickId} destroyed (row {brickRow}). Remaining: {brickGrid.Count}");

                // Emit event with color (for game rules)
                BrickDestroyedWithColor?.Invoke(color);
            }
        }
        #endregion
    }
}
