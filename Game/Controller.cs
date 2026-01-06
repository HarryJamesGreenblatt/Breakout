using Godot;
using Breakout.Components;

namespace Breakout.Game
{
    /// <summary>
    /// Game controller: pure signal wiring and coordination.
    /// Instantiation delegated to EntityComponent (separation of concerns).
    /// All business logic delegated to components (GameStateComponent, etc.).
    /// 
    /// Responsibility: ONLY wire signals between components and entities.
    /// Nothing else. Zero persistent state.
    /// 
    /// Following Nystrom's Component pattern:
    /// - EntityComponent creates and manages entities
    /// - Controller only wires signals
    /// - Components own all state and logic
    /// </summary>
    public partial class Controller : Node2D
    {
        #region Game Loop
        public override void _Ready()
        {
            // Instantiate EntityComponent (responsible for creating entity-component pairs)
            var entityComponent = new EntityComponent();

            // Instantiate components and entities
            var gameState = entityComponent.CreateGameState();
            var brickGrid = entityComponent.CreateBrickGrid(this);
            var paddle = entityComponent.CreatePaddle(this);
            var (ball, ballPhysics) = entityComponent.CreateBallWithPhysics(this);
            var walls = entityComponent.CreateWalls(this);  // For completeness; walls are stateless

            // Wire all signals directly to component behavior owners (zero indirection)
            gameState.SpeedIncreaseRequired += ballPhysics.ApplySpeedMultiplier;  // Wire to physics, not ball
            gameState.PaddleShrinkRequired += paddle.Shrink;
            brickGrid.BrickDestroyedWithColor += gameState.OnBrickDestroyed;

            // Connect ball signals to game state
            ball.BallHitPaddle += () => OnBallHitPaddle();
            ball.BallOutOfBounds += () => {
                OnBallOutOfBounds();
                gameState.LoseLive();
            };
            ball.BallHitCeiling += () => gameState.OnBallHitCeiling();
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
        }
        #endregion
    }
}
