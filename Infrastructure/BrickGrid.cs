using Godot;
using Breakout.Entities;
using Breakout.Utilities;
using System;
using System.Collections.Generic;

namespace Breakout.Infrastructure
{
    /// <summary>
    /// BrickGrid — infrastructure component managing the brick grid.
    /// 
    /// Classified as Infrastructure because:
    /// - Manages a concrete construct of entities (brick grid)
    /// - Similar to Walls (both are entity collections forming world structure)
    /// - Distinct from arbitrary business logic components (Physics, GameState, Sound, Rendering)
    /// 
    /// Responsibilities:
    /// - Create and manage brick grid
    /// - Track active bricks
    /// - Handle brick destruction (remove from grid, compute brick color)
    /// - Emit BrickDestroyed event with color for scoring/rules
    /// </summary>
    public partial class BrickGrid
    {
        #region State
        /// <summary>
        /// Dictionary mapping brick ID to brick entity (currently alive bricks).
        /// </summary>
        private Dictionary<int, Brick> brickGrid = new();

        /// <summary>
        /// Track which brick IDs were destroyed in the previous game.
        /// Used during restart to fade in only the destroyed bricks.
        /// </summary>
        private List<int> destroyedBrickIds = new();
        #endregion

        #region Events
        /// <summary>
        /// Emitted when a brick is destroyed.
        /// Passes the brick color for game rules (speed increases, scoring).
        /// </summary>
        public event Action<Utilities.BrickColor> BrickDestroyedWithColor;

        /// <summary>
        /// Emitted when all bricks are destroyed (grid becomes empty).
        /// </summary>
        public event Action AllBricksDestroyed;

        /// <summary>
        /// Emitted when grid is instantiated (for UI/debug).
        /// </summary>
        public event Action<int> GridInstantiated;
        #endregion

        #region Public API
        /// <summary>
        /// Instantiates the brick grid.
        /// Called by factory during setup and on game restart.
        /// 
        /// Parameters:
        /// - parentNode: Parent node to add bricks to
        /// - startInvisible: If true, all bricks start invisible (for fade-in transitions)
        /// - destroyedBrickIds: Optional collection of brick IDs that were destroyed.
        ///   When provided, only these bricks are created invisible; others are created visible.
        ///   This allows destroyed bricks to fade in while unbroken bricks stay visible on restart.
        /// </summary>
        public void InstantiateGrid(Godot.Node parentNode, bool startInvisible = false, IEnumerable<int> destroyedBrickIds = null)
        {
            // Convert destroyed list to hashset for O(1) lookup
            var destroyedSet = destroyedBrickIds != null ? new HashSet<int>(destroyedBrickIds) : new HashSet<int>();
            
            int brickId = 0;
            Vector2 gridStart = Breakout.Game.Config.BrickGrid.GridStartPosition;

            for (int row = 0; row < Breakout.Game.Config.BrickGrid.GridRows; row++)
            {
                for (int col = 0; col < Breakout.Game.Config.BrickGrid.GridColumns; col++)
                {
                    // Calculate brick position
                    Vector2 position = gridStart + new Vector2(
                        col * Breakout.Game.Config.BrickGrid.GridSpacingX,
                        row * Breakout.Game.Config.BrickGrid.GridSpacingY
                    );

                    // Get brick color for this row and fetch its config
                    BrickColor brickColorEnum = BrickColorUtility.GetColorForRow(row);
                    BrickColorConfig colorConfig = BrickColorUtility.GetConfig(brickColorEnum);

                    // Create and add brick to the scene
                    var brick = new Brick(brickId, position, Breakout.Game.Config.Brick.Size, colorConfig.VisualColor);
                    
                    // Determine if this brick should start invisible
                    bool isBrickInvisible = false;
                    
                    if (startInvisible)
                    {
                        // Initial game start: all bricks invisible
                        isBrickInvisible = true;
                    }
                    else if (destroyedSet.Count > 0 && destroyedSet.Contains(brickId))
                    {
                        // Game restart: only destroyed bricks are invisible (for fade-in)
                        // Unbroken bricks stay visible
                        isBrickInvisible = true;
                    }
                    
                    if (isBrickInvisible)
                    {
                        brick.SetInvisible();
                    }
                    
                    parentNode.AddChild(brick);

                    // Store brick in the dictionary
                    brickGrid[brickId] = brick;

                    // Connect brick destruction signal
                    brick.BrickDestroyed += (id) => OnBrickDestroyed(id);

                    brickId++;
                }
            }

            GridInstantiated?.Invoke(brickId);
            GD.Print($"Brick grid instantiated: {brickId} bricks (startInvisible: {startInvisible})");
        }

        /// <summary>
        /// Query remaining brick count.
        /// </summary>
        public int GetRemainingBrickCount() => brickGrid.Count;

        /// <summary>
        /// Get all bricks currently in the grid.
        /// Exposes brick collection for TransitionComponent to animate.
        /// </summary>
        public IEnumerable<Brick> GetAllBricks() => brickGrid.Values;

        /// <summary>
        /// Get IDs of bricks that were destroyed in the previous game session.
        /// Used by TransitionComponent to fade in only destroyed bricks on restart.
        /// Unbroken bricks stay visible without transition.
        /// </summary>
        public IEnumerable<int> GetDestroyedBrickIds() => destroyedBrickIds;

        /// <summary>
        /// Reset grid for game restart.
        /// Removes all existing bricks from scene, clears the grid, and resets destroyed brick tracking.
        /// Does NOT rebuild the grid (Controller will call InstantiateGrid again).
        /// </summary>
        public void ResetForGameRestart(Godot.Node parentNode)
        {
            // Remove all brick nodes from the scene
            foreach (var brick in brickGrid.Values)
            {
                if (brick != null)
                {
                    brick.QueueFree();
                }
            }

            // Clear the grid and destroyed list
            brickGrid.Clear();
            destroyedBrickIds.Clear();
            GD.Print("BrickGrid cleared for restart");
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Handles brick destruction.
        /// Removes brick from grid, adds ID to destroyed list, and emits event with color.
        /// Destroyed IDs are used on restart to differentiate destroyed vs unbroken bricks.
        /// </summary>
        private void OnBrickDestroyed(int brickId)
        {
            if (brickGrid.ContainsKey(brickId))
            {
                // Compute brick row to determine color
                int gridColumns = Breakout.Game.Config.BrickGrid.GridColumns;
                int brickRow = brickId / gridColumns;
                BrickColor color = BrickColorUtility.GetColorForRow(brickRow);

                // Remove from grid and track as destroyed
                brickGrid.Remove(brickId);
                destroyedBrickIds.Add(brickId);

                GD.Print($"Brick {brickId} destroyed (row {brickRow}). Remaining: {brickGrid.Count}");

                // Emit event with color (for game rules)
                BrickDestroyedWithColor?.Invoke(color);

                // Check if all bricks destroyed (level complete)
                if (brickGrid.Count == 0)
                {
                    AllBricksDestroyed?.Invoke();
                    GD.Print("All bricks destroyed!");
                }
            }
        }
        #endregion
    }
}
