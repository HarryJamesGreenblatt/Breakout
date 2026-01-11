using Breakout.Components;
using Breakout.Utilities;
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
        #endregion

        #region Game Loop
        public override void _Ready()
        {
            // Instantiate all components and entities
            var entityFactory = new EntityFactoryUtility();
            gameState = entityFactory.CreateGameState();
            var brickGrid = entityFactory.CreateBrickGrid(this);
            var paddle = entityFactory.CreatePaddle(this);
            var (ball, ballPhysics) = entityFactory.CreateBallWithPhysics(this);
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

            GD.Print("Controller initialized: entities created, signals wired");
        }

        public override void _Process(double delta)
        {
            // Handle ESC key to exit on game over
            if (Input.IsActionJustPressed("ui_cancel") && gameState.GetState() == GameStateComponent.GameState.GameOver)
            {
                GetTree().Quit();
            }
        }
        #endregion
    }
}
