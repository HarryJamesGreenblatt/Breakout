using Breakout.Components;
using Breakout.Utilities;
using Breakout.Entities;
using Breakout.Infrastructure;
using Godot;

namespace Breakout.Game
{
    /// <summary>
    /// Game controller: instantiation and coordination only.
    /// Signal wiring delegated to SignalWiringUtility (separation of concerns).
    /// 
    /// Responsibility: Create entities and components, then wire them via utility.
    /// Zero signal handling logic. Pure orchestration.
    /// 
    /// Following Nystrom's patterns:
    /// - EntityFactoryUtility creates entity-component pairs
    /// - SignalWiringUtility wires all signals (stateless utility)
    /// - Controller is just the orchestrator (thin, clean)
    /// - Components own all state and logic
    /// </summary>
    public partial class Controller : Node2D
    {
        #region State
        private GameStateComponent gameState;
        private BrickGrid brickGrid;
        private Paddle paddle;
        private Ball ball;
        private PhysicsComponent ballPhysics;
        #endregion

        #region Game Loop
        public override void _Ready()
        {
            // Instantiate all components and entities
            var entityFactory = new EntityFactoryUtility();
            gameState = entityFactory.CreateGameState();
            brickGrid = entityFactory.CreateBrickGrid(this);
            paddle = entityFactory.CreatePaddle(this);
            (ball, ballPhysics) = entityFactory.CreateBallWithPhysics(this);
            entityFactory.CreateWalls(this);

            var uiComponent = new UIComponent();
            AddChild(uiComponent);

            var soundComponent = new SoundComponent();
            AddChild(soundComponent);

            // Wire all signals via utility (clean separation)
            SignalWiringUtility.WireGameRules(gameState, ballPhysics, paddle);
            SignalWiringUtility.WireBrickEvents(brickGrid, gameState, uiComponent, soundComponent);
            SignalWiringUtility.WireUIEvents(gameState, uiComponent);
            SignalWiringUtility.WireBallEvents(ball, paddle, gameState);
            SignalWiringUtility.WireBallSoundEvents(ball, ballPhysics, soundComponent);
            SignalWiringUtility.WireGameStateSoundEvents(gameState, soundComponent);
            SignalWiringUtility.WireGameOverState(gameState, ball, paddle);

            // When continue countdown expires, quit the game (same as ESC)
            gameState.ContinueCountdownExpired += () => GetTree().Quit();

            GD.Print("Controller initialized: entities created, signals wired");
        }

        public override void _Process(double delta)
        {
            // Update continue countdown when in game over state
            if (gameState.GetState() == GameStateComponent.GameState.GameOver)
            {
                gameState.UpdateContinueCountdown((float)delta);

                // Handle restart on game over (only if countdown > 0)
                if (Input.IsActionJustPressed("ui_accept") && gameState.GetContinueCountdownRemaining() > 0)
                {
                    RestartGame();
                }
            }

            // Handle ESC key to exit on game over
            if (Input.IsActionJustPressed("ui_cancel") && gameState.GetState() == GameStateComponent.GameState.GameOver)
            {
                GetTree().Quit();
            }
        }
        #endregion

        #region Game Restart
        /// <summary>
        /// Restart the game to initial state.
        /// Resets all game state, clears and rebuilds brick grid, and resets entities.
        /// GameStateComponent.Reset() triggers state transition → UIComponent auto-hides game over message.
        /// </summary>
        private void RestartGame()
        {
            GD.Print("=== RESTARTING GAME ===");

            // Reset game state (score, lives, hit counts, flags, and state machine)
            // StateChanged event will automatically trigger UIComponent to hide game over message
            gameState.Reset();

            // Reset physics (clears speed multiplier and velocity)
            ballPhysics.ResetForGameRestart();

            // Reset ball visual position and re-enable _Process()
            ball.ResetForGameRestart();

            // Reset paddle to initial state (size, position, speed multiplier)
            paddle.ResetForGameRestart();

            // Reset and rebuild brick grid
            brickGrid.ResetForGameRestart(this);
            brickGrid.InstantiateGrid(this);

            GD.Print("Game restart complete");
        }
        #endregion
    }
}
