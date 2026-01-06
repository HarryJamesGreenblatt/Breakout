using Godot;
using Breakout.Entities;
using Breakout.Infrastructure;
using Breakout.Models;
using Breakout.Services;
using Breakout.Components;
using System.Collections.Generic;

namespace Breakout.Game
{
    /// <summary>
    /// Game orchestrator: coordinates entities, signals, and game loop.
    /// Responsible for instantiation and signal binding only.
    /// Game state is owned by GameStateComponent (following Nystrom's pattern).
    /// </summary>
    public partial class Orchestrator : Node2D
    {
        #region Components & Entities

        /// <summary>
        /// Game state component owns all mutable state (score, lives, rules).
        /// </summary>
        private GameStateComponent gameState;

        /// <summary>
        /// A dictionary to hold the brick grid.
        /// </summary>
        private Dictionary<int, Brick> brickGrid = new();

        /// <summary>
        /// Reference to the ball for speed control.
        /// </summary>
        private Ball ball;

        /// <summary>
        /// Reference to the paddle for shrinking.
        /// </summary>
        private Paddle paddle;

        #region Brick Grid Management
        /// <summary>
        /// Creates the brick grid based on Config settings.
        /// Bricks are stored in a dictionary keyed by unique ID for easy removal.
        /// </summary>
        private void InstantiateBrickGrid()
        {
            int brickId = 0;
            Vector2 gridStart = Config.Brick.GridStartPosition;

            // iteratting through each row in the grid
            for (int row = 0; row < Config.Brick.GridRows; row++)
            {
                // iterating through each column in the grid
                for (int col = 0; col < Config.Brick.GridColumns; col++)
                {
                    // Calculate brick position
                    Vector2 position = gridStart + new Vector2(
                        col * Config.Brick.GridSpacingX,
                        row * Config.Brick.GridSpacingY
                    );

                    // Get brick color for this row and fetch its config
                    BrickColor brickColorEnum = BrickColorService.GetColorForRow(row);
                    BrickColorConfig colorConfig = BrickColorService.GetConfig(brickColorEnum);

                    // Create and add brick to the scene
                    var brick = new Brick(brickId, position, Config.Brick.Size, colorConfig.VisualColor);
                    AddChild(brick);

                    // Store brick in the dictionary
                    brickGrid[brickId] = brick;

                    // Increment brick ID for next brick
                    brickId++;
                }
            }

            GD.Print($"Brick grid instantiated: {brickId} bricks");
        }
        #endregion

        #endregion

        #region Game Loop
        public override void _Ready()
        {
            // Instantiate game state component
            gameState = new GameStateComponent();

            // Instantiate entities using GameConfig
            paddle = new Paddle(
                Config.Paddle.Position,
                Config.Paddle.Size,
                Config.Paddle.Color
            );
            AddChild(paddle);

            ball = new Ball(
                Config.Ball.Position,
                Config.Ball.Size,
                Config.Ball.Velocity,
                Config.Ball.Color
            );
            AddChild(ball);

            // Instantiate infrastructure
            var walls = new Walls();
            AddChild(walls);

            // Instantiate brick grid
            InstantiateBrickGrid();

            // Connect game state component events to action handlers
            gameState.SpeedIncreaseRequired += ApplySpeedIncrease;
            gameState.PaddleShrinkRequired += paddle.Shrink;

            // Connect ball signals to game state
            ball.BallHitPaddle += OnBallHitPaddle;
            ball.BallOutOfBounds += OnBallOutOfBounds;
            ball.BallHitCeiling += () => gameState.OnBallHitCeiling();

            // Connect brick signals to game state
            foreach (var brick in brickGrid.Values)
            {
                brick.BrickDestroyed += (brickId) => OnBrickDestroyed(brickId);
            }
        }

        public override void _Process(double delta)
        {
            // Game loop (state transitions, rule checks, etc.)
            // All event-driven now; no polling
        }
        #endregion

        #region Signals
        private void OnBallHitPaddle()
        {
            GD.Print("Ball hit paddle!");
        }

        private void OnBallOutOfBounds()
        {
            GD.Print("Ball out of bounds!");
            gameState.LoseLive();
        }

        /// <summary>
        /// Handles brick destruction by delegating to game state component.
        /// </summary>
        private void OnBrickDestroyed(int brickId)
        {
            if (brickGrid.ContainsKey(brickId))
            {
                // Get brick's row to determine color
                int gridColumns = Config.Brick.GridColumns;
                int brickRow = brickId / gridColumns;
                BrickColor color = BrickColorService.GetColorForRow(brickRow);

                brickGrid.Remove(brickId);

                GD.Print($"Brick {brickId} destroyed (row {brickRow}). Remaining: {brickGrid.Count}");

                // Delegate to game state component (which owns all rule logic)
                gameState.OnBrickDestroyed(color);
            }
        }

        /// <summary>
        /// Applies a speed multiplier to the ball.
        /// Called when gameState emits SpeedIncreaseRequired event.
        /// </summary>
        private void ApplySpeedIncrease(float multiplier)
        {
            if (ball != null)
            {                ball.ApplySpeedMultiplier(multiplier);
            }
        }
        #endregion
    }
}