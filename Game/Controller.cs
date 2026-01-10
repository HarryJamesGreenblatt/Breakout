using Breakout.Components;
using Breakout.Utilities;
using Godot;

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
        #region State
        private GameStateComponent gameState;
        #endregion

        #region Game Loop
        public override void _Ready()
        {
            // Instantiate EntityFactoryUtility (responsible for creating entity-component pairs)
            var entityFactory = new EntityFactoryUtility();

            // Instantiate components and entities
            gameState = entityFactory.CreateGameState();
            var brickGrid = entityFactory.CreateBrickGrid(this);
            var paddle = entityFactory.CreatePaddle(this);
            var (ball, ballPhysics) = entityFactory.CreateBallWithPhysics(this);
            var walls = entityFactory.CreateWalls(this);  // For completeness; walls are stateless

            // Instantiate UI component (manages HUD display of score and lives)
            var uiComponent = new UIComponent();
            AddChild(uiComponent);

            // Instantiate sound component (manages all game audio)
            var soundComponent = new SoundComponent();
            AddChild(soundComponent);

            // Wire all signals directly to component behavior owners (zero indirection)
            gameState.SpeedIncreaseRequired += ballPhysics.ApplySpeedMultiplier;  // Wire to physics, not ball
            gameState.PaddleSpeedIncreaseRequired += paddle.ApplySpeedMultiplier;  // Wire paddle speed compensation
            gameState.PaddleShrinkRequired += () => paddle.CallDeferred(nameof(paddle.Shrink));  // Deferred to avoid physics query error
            
            // Wire brick destruction to BOTH game rules (speed/shrink) AND scoring
            brickGrid.BrickDestroyedWithColor += gameState.OnBrickDestroyed;    // Game rules (speed increases, paddle shrink)
            brickGrid.BrickDestroyedWithColor += gameState.AddScore;             // Scoring
            brickGrid.BrickDestroyedWithColor += (color) => uiComponent.FlashScoreForColor(color);     // UI flash by color
            brickGrid.BrickDestroyedWithColor += (color) => soundComponent.PlayBrickHit(color);        // Sound with polyphonic cracking by color

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

            // Wire GameOver event to disable ball, paddle, and show message
            gameState.GameOver += () => {
                ball.ProcessMode = ProcessModeEnum.Disabled;
                paddle.SetInputEnabled(false);
                uiComponent.ShowGameOverMessage();
            };

            // Connect ball signals to game state
            ball.BallHitPaddle += () => OnBallHitPaddle();
            ball.BallOutOfBounds += () => {
                OnBallOutOfBounds();
                gameState.DecrementLives();
            };
            ball.BallHitCeiling += () => {
                gameState.OnBallHitCeiling();
                soundComponent.PlayWallBounce();
            };

            // Wire Sound events
            ball.BallHitPaddle += soundComponent.PlayPaddleHit;
            ballPhysics.WallHit += soundComponent.PlayWallBounce;  // Side wall bounces
            gameState.SpeedIncreaseRequired += (_) => soundComponent.PlaySpeedIncrease();
            gameState.PaddleShrinkRequired += soundComponent.PlayPaddleShrinkEffect;  // "gaw gaw gaw" effect
            gameState.LivesChanged += (lives) => {
                if (lives > 0) soundComponent.PlayLivesDecremented();  // Play sound when lives decrease
                else if (lives <= 0) soundComponent.PlayGameOver();  // Play game over sound when lives reach 0
            };

            // Wire all-bricks-destroyed to level complete
            brickGrid.AllBricksDestroyed += () => {
                gameState.SetState(GameStateComponent.GameState.LevelComplete);
            };

            GD.Print("Controller initialized with Objective 2.1: Scoring & UI");
        }

        public override void _Process(double delta)
        {
            // Handle ESC key to exit on game over (check state, don't manage it)
            if (Input.IsActionJustPressed("ui_cancel") && gameState.GetState() == GameStateComponent.GameState.GameOver)
            {
                GetTree().Quit();
            }
        }
        #endregion

        #region Signal Handlers
        /// <summary>
        /// Called when ball hits the paddle.
        /// </summary>
        private void OnBallHitPaddle()
        {
            GD.Print("Ball hit paddle!");
        }

        /// <summary>
        ///  Called when ball goes out of bounds (missed by paddle).
        /// </summary>
        private void OnBallOutOfBounds()
        {
            GD.Print("Ball out of bounds!");
        }
        #endregion
    }
}
