using Godot;
using Breakout.Entities;
using Breakout.Infrastructure;
using System.Collections.Generic;

namespace Breakout.Game
{
    /// <summary>
    /// Game orchestrator: manages entities, signals, and game loop.
    /// Responsible for instantiation, signal binding, and overall game state.
    /// </summary>
    public partial class Orchestrator : Node2D
    {
        #region Game State
        private Dictionary<int, Brick> brickGrid = new();
        #endregion

        #region Game Loop
        public override void _Ready()
        {
            // Instantiate entities using GameConfig
            var paddle = new Paddle(
                Config.Paddle.Position,
                Config.Paddle.Size,
                Config.Paddle.Color
            );
            AddChild(paddle);

            var ball = new Ball(
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

            // Connect signal listeners
            ball.BallHitPaddle += OnBallHitPaddle;
            ball.BallOutOfBounds += OnBallOutOfBounds;

            // Connect brick signals
            foreach (var brick in brickGrid.Values)
            {
                brick.BrickDestroyed += OnBrickDestroyed;
            }
        }

        public override void _Process(double delta)
        {
            // Main game loop (future: game state, scoring, etc.)
        }
        #endregion

        #region Brick Grid Management
        /// <summary>
        /// Creates the brick grid based on Config settings.
        /// Bricks are stored in a dictionary keyed by unique ID for easy removal.
        /// </summary>
        private void InstantiateBrickGrid()
        {
            int brickId = 0;
            Vector2 gridStart = Config.Brick.GridStartPosition;

            for (int row = 0; row < Config.Brick.GridRows; row++)
            {
                for (int col = 0; col < Config.Brick.GridColumns; col++)
                {
                    Vector2 position = gridStart + new Vector2(
                        col * Config.Brick.GridSpacingX,
                        row * Config.Brick.GridSpacingY
                    );

                    // Get brick color for this row and fetch its config
                    BrickColor brickColorEnum = BrickColorDefinitions.GetColorForRow(row);
                    BrickColorConfig colorConfig = BrickColorDefinitions.GetConfig(brickColorEnum);

                    var brick = new Brick(brickId, position, Config.Brick.Size, colorConfig.VisualColor);

                    AddChild(brick);
                    brickGrid[brickId] = brick;
                    brickId++;
                }
            }

            GD.Print($"Brick grid instantiated: {brickId} bricks");
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
        }

        private void OnBrickDestroyed(int brickId)
        {
            if (brickGrid.ContainsKey(brickId))
            {
                brickGrid.Remove(brickId);
                GD.Print($"Brick {brickId} destroyed. Remaining: {brickGrid.Count}");
            }
        }
        #endregion
    }
}
