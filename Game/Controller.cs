using Godot;
using Breakout.Components;
using Breakout.Utilities;
using Breakout.Infrastructure;

namespace Breakout.Game
{
    /// <summary>
    /// Game controller: pure signal wiring and coordination.
    /// Instantiation delegated to EntityFactory (separation of concerns).
    /// All business logic delegated to components (GameStateComponent, etc.).
    /// 
    /// Responsibility: ONLY wire signals between components and entities.
    /// Nothing else. Zero persistent state.
    /// 
    /// Following Nystrom's Component pattern:
    /// - EntityFactory creates and manages entities
    /// - Controller only wires signals
    /// - Components own all state and logic
    /// 
    /// Objective 2.1 Update:
    /// - GameUIComponent wired to ScoreChanged/LivesChanged events
    /// - GameStateComponent now includes state machine (Playing/GameOver/LevelComplete)
    /// - Game-over detection triggers state transition within GameStateComponent
    /// </summary>
    public partial class Controller : Node2D
    {
        #region Game Loop
        public override void _Ready()
        {
            // Instantiate EntityFactoryUtility (responsible for creating entity-component pairs)
            var entityFactory = new EntityFactoryUtility();

            // Instantiate components and entities
            var gameState = entityFactory.CreateGameState();
            var brickGrid = entityFactory.CreateBrickGrid(this);
            var paddle = entityFactory.CreatePaddle(this);
            var (ball, ballPhysics) = entityFactory.CreateBallWithPhysics(this);
            var walls = entityFactory.CreateWalls(this);  // For completeness; walls are stateless

            // Instantiate UI component (manages HUD display of score and lives)
            var uiComponent = new UIComponent();
            AddChild(uiComponent);

            // Wire all signals directly to component behavior owners (zero indirection)
            gameState.SpeedIncreaseRequired += ballPhysics.ApplySpeedMultiplier;  // Wire to physics, not ball
            gameState.PaddleShrinkRequired += paddle.Shrink;
            brickGrid.BrickDestroyedWithColor += gameState.OnBrickDestroyed;

            // Wire UI events
            gameState.ScoreChanged += uiComponent.OnScoreChanged;
            gameState.LivesChanged += uiComponent.OnLivesChanged;

            // Wire state machine (game-over detection)
            gameState.LivesChanged += (lives) => {
                if (lives <= 0)
                {
                    gameState.SetState(GameStateComponent.GameState.GameOver);
                }
            };

            // Connect ball signals to game state
            ball.BallHitPaddle += () => OnBallHitPaddle();
            ball.BallOutOfBounds += () => {
                OnBallOutOfBounds();
                gameState.LoseLive();
            };
            ball.BallHitCeiling += () => gameState.OnBallHitCeiling();

            // All-bricks-destroyed detection
            brickGrid.GridInstantiated += (brickCount) => {
                OnGridInstantiated(brickCount, brickGrid, gameState);
            };

            GD.Print("Controller initialized with Objective 2.1: Scoring & UI");
        }

        public override void _Process(double delta)
        {
            // Game loop (state transitions, rule checks, etc.)
            // All event-driven now; no polling
        }
        #endregion

#region Signal Handlers
        private void OnBallHitPaddle()
        {
            GD.Print("Ball hit paddle!");
        }

        private void OnBallOutOfBounds()
        {
            GD.Print("Ball out of bounds!");
        }

        /// <summary>
        /// Called when brick grid is instantiated.
        /// Stores brick count for level-complete detection.
        /// </summary>
        private int totalBricksInLevel = 0;
        private int bricksDestroyedInLevel = 0;

        private void OnGridInstantiated(int brickCount, Infrastructure.BrickGrid brickGrid, GameStateComponent gameState)
        {
            totalBricksInLevel = brickCount;
            bricksDestroyedInLevel = 0;
            GD.Print($"Level instantiated: {totalBricksInLevel} bricks");

            // Wire brick destruction tracking for level-complete detection
            brickGrid.BrickDestroyedWithColor += (color) => {
                bricksDestroyedInLevel++;
                if (bricksDestroyedInLevel >= totalBricksInLevel)
                {
                    gameState.SetState(GameStateComponent.GameState.LevelComplete);
                    GD.Print("Level complete! All bricks destroyed.");
                }
            };
        }
        #endregion
    }
}
