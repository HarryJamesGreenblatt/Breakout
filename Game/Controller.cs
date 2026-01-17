using Breakout.Components;
using Breakout.Utilities;
using Breakout.Entities;
using Breakout.Infrastructure;
using Godot;
using System.Linq;

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
        private TransitionComponent transitionComponent;
        private BrickGrid brickGrid;
        private Paddle paddle;
        private Ball ball;
        private PhysicsComponent ballPhysics;
        private SoundComponent soundComponent;
        #endregion

        #region Game Loop
        public override void _Ready()
        {
            // Instantiate all components and entities
            var entityFactory = new EntityFactoryUtility();
            gameState = entityFactory.CreateGameState();
            transitionComponent = new TransitionComponent();
            AddChild(transitionComponent);
            brickGrid = entityFactory.CreateBrickGrid(this, startInvisible: true);  // Start invisible for initial transition
            paddle = entityFactory.CreatePaddle(this);
            (ball, ballPhysics) = entityFactory.CreateBallWithPhysics(this);
            entityFactory.CreateWalls(this);

            var uiComponent = new UIComponent();
            AddChild(uiComponent);

            soundComponent = new SoundComponent();
            AddChild(soundComponent);

            // Wire all signals via utility (clean separation)
            SignalWiringUtility.WireGameRules(gameState, ballPhysics, paddle);
            SignalWiringUtility.WireBrickEvents(brickGrid, gameState, uiComponent, soundComponent);
            SignalWiringUtility.WireUIEvents(gameState, uiComponent);
            SignalWiringUtility.WireBallEvents(ball, paddle, gameState);
            SignalWiringUtility.WireBallSoundEvents(ball, ballPhysics, soundComponent);
            SignalWiringUtility.WireGameStateSoundEvents(gameState, soundComponent);
            SignalWiringUtility.WireGameOverState(gameState, ball, paddle);
            SignalWiringUtility.WireTransitionState(gameState, paddle);

            // Wire paddle shrinking (PhysicsComponent handles deferred execution)
            gameState.PaddleShrinkRequired += () => ballPhysics.ShrinkPaddle(paddle);

            // Wire auto-play toggle (business logic: PhysicsComponent implements paddle resize)
            gameState.AutoPlayToggled += (enabled) => ballPhysics.UpdateAutoPlayPaddle(paddle, enabled);

            // Wire transition events
            transitionComponent.TransitionComplete += () => 
            {
                ball.ProcessMode = Node.ProcessModeEnum.Inherit;  // Re-enable ball physics
                soundComponent.PlayBallLaunch();  // Play launch sound when ball starts moving
                gameState.EnterPlayingState();
            };
            ball.BallBlip += () => soundComponent.PlayBallBlip();

            // When continue countdown expires, quit the game (same as ESC)
            gameState.ContinueCountdownExpired += () => GetTree().Quit();

            // Start game with transition animation
            // Hide ball and disable physics until it blips in during transition
            ball.Visible = false;
            ball.ProcessMode = Node.ProcessModeEnum.Disabled;
            gameState.EnterTransitionState();
            transitionComponent.PlayGameStartTransition(paddle, ball, brickGrid);

            GD.Print("Controller initialized: entities created, signals wired, game start transition playing");
        }

        public override void _Process(double delta)
        {
            // Handle spacebar to toggle auto-play mode (test feature)
            if (Input.IsActionJustPressed("ui_select"))
            {
                gameState.ToggleAutoPlay();
            }

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
        /// Restart the game with smooth transitions.
        /// Sequence:
        /// 1. Enter Continuing state (blocks sound effects during reset)
        /// 2. Reset game state and physics immediately
        /// 3. Reset entities to initial positions (instant for ball/paddle state)
        /// 4. Rebuild brick grid (invisible initially)
        /// 5. Enter Transitioning state and play animations (bricks fade, paddle eases, ball blips)
        /// 6. Transition completes → enter Playing state
        /// </summary>
        private void RestartGame()
        {
            GD.Print("=== RESTARTING GAME ===");

            // Enter continuing state (blocks sound effects during reset)
            gameState.EnterContinuingState();

            // Reset game state (score, lives, hit counts, flags)
            // StateChanged event will automatically trigger UIComponent to hide game over message
            gameState.Reset();

            // Reset physics (clears speed multiplier and velocity)
            ballPhysics.ResetForGameRestart();

            // Reset ball to initial position (instant, will blip in later)
            // Keep ball disabled during transition - will re-enable when transition completes
            ball.ResetForGameRestart();
            ball.Visible = false;  // Hide until blip
            ball.ProcessMode = Node.ProcessModeEnum.Disabled;

            // Reset paddle state (will ease to center during transition)
            paddle.ResetForGameRestart();

            // Reset and rebuild brick grid
            // Save destroyed IDs before reset clears them
            var destroyedBrickIds = brickGrid.GetDestroyedBrickIds().ToList();
            
            // Reset clears destroyed list and removes all bricks
            brickGrid.ResetForGameRestart(this);
            
            // Instantiate grid: unbroken bricks visible, destroyed bricks invisible for fade-in
            brickGrid.InstantiateGrid(this, startInvisible: false, destroyedBrickIds: destroyedBrickIds);

            // Enter transitioning state and play animation sequence
            gameState.EnterTransitionState();
            transitionComponent.PlayRestartTransition(paddle, ball, brickGrid);

            GD.Print("Restart transition started");
        }
        #endregion
    }
}
