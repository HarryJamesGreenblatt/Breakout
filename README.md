# Breakout

![Breakout Poster](https://flyers.arcade-museum.com/videogame-flyers/0/breakout-00145-01.jpg)

A faithful recreation of Atari's classic *Breakout* (1976) in **Godot 4.5 with C#**, built as a hands-on learning exercise in 2D game development fundamentals and architectural patterns.

**Current Status:** Objective 1.1++ complete with polish features (MVP fully playable with arcade-authentic physics, UI, and smooth transitions) — Destroyed brick tracking, fade-in transitions, auto-play test mode with full-width paddle, proper deferred physics operations, and critical bug fixes for lives reset and UI timing (January 16, 2026).

---

## Overview

This project follows a **pattern-driven learning approach** grounded in:
- **Godot 4.5 official documentation** (source of truth for engine behavior)
- **Robert Nystrom's *Game Programming Patterns*** (https://gameprogrammingpatterns.com/) — architectural guidance
- **2D Game Development Curriculum** (Branch A: Arcade Foundations) — design and progression

### Philosophy

Rather than providing complete code, this project **implements features iteratively**, discovering problems that make design patterns valuable. The goal is to understand *why* patterns exist, not just apply them blindly.

---

## What's Implemented

### MVP (Objective 1.1++ Complete ✅)

- **Entities:** Paddle (player-controlled with speed compensation), Ball (physics-driven with magnitude preservation), Brick Grid (8×8, destructible with color-based feedback), Walls (boundary)
- **Gameplay:** Ball bounces off walls, paddle, and bricks preserving speed magnitude; bricks destroyed on contact; speed increases (4, 12 hit milestones + orange/red row contact) at 15%; paddle speed increases at 10% to compensate; paddle shrinks on red row breakthrough (deferred execution)
- **Collision Detection:** Signal-based (`AreaEntered`/`AreaExited` events) instead of polling
- **Components:** PhysicsComponent (ball physics with proper magnitude preservation across bounces), GameStateComponent (canonical Breakout rules), UIComponent (HUD with color-based score flashing), SoundComponent (8-bit synthesis with color-based polyphonic cracks), BrickGrid (Infrastructure/), EntityFactoryUtility (Utilities/)
- **UI Layer:** Zero-padded 3-digit score display, lives counter, color-synchronized score flashing (red=4, orange=3, green=2, yellow=1), centered game over message accounting for wall offset, bold sans-serif arcade font
- **Physics:** Magnitude preservation across all bounces (paddle reconstructs velocity to maintain speed), randomized launch angle (60°-120° downward), speed multiplier persistence across ball resets
- **Audio:** Color-based polyphonic cracking (1-4 cracks matching brick value), 8-bit square wave synthesis
- **Brick Destruction:** Ball collision → PhysicsComponent.HandleBrickCollision() → brick.Destroy() → BrickDestroyed signal → BrickGrid listener → color passed to GameStateComponent for rules/scoring/events
- **Destroyed Brick Tracking:** BrickGrid tracks destroyed brick IDs; TransitionComponent fades only destroyed bricks on restart while unbroken bricks remain visible
- **Auto-Play Test Mode:** Press spacebar to toggle full-width paddle (spans entire viewport) for testing without manual control; GameStateComponent owns auto-play flag, PhysicsComponent implements logic
- **Configuration:** Centralized in `Config.cs` with `Config.Brick`, `Config.BrickGrid`, `Config.Paddle` (entity properties and infrastructure layout); dynamic brick grid spacing
- **Brick Colors:** Type-safe enum with scoring metadata (Red=7pts, Orange=5pts, Green=3pts, Yellow=1pt)
- **Architecture:** Nystrom's Component Pattern — plain C# components own state & logic; thin entities forward events; pure signal wiring; direct component access; event-driven communication (no state drilling); PhysicsComponent owns collision-affecting operations

### Not Yet Implemented

- Level progression and level complete state
- Ball speed cap (may need for extremely long games)
- Replay/restart functionality
- Proper game initialization flow (wait for player input before launch)

---

## Architecture Notes

### Current Design: Nystrom's Component Pattern

**Pattern:** Component (plain C# classes owning state + logic) + Observer (Godot Signals + C# Events)

**Architecture Layers (Organized by Role):**

1. **Components/** (Business Logic) — Own state and logic, emit C# events:
   - `PhysicsComponent` — Ball velocity, position, collision tracking, speed multipliers, bounce logic
   - `GameStateComponent` — Score, lives, hit count, speed/shrink decision logic
   - `UIComponent` — HUD display (score, lives labels); listens to ScoreChanged/LivesChanged events

2. **Infrastructure/** (World Structure) — Entity collections forming environment:
   - `BrickGrid` — Brick grid management and destruction tracking
   - `Walls` — Container that instantiates and manages Wall entities

3. **Utilities/** (Helpers) — Factory and lookup functions:
   - `EntityFactoryUtility` — Factory for entity-component pair instantiation
   - `BrickColorUtility` — Color-to-config lookup

4. **Entities/** (Node2D Containers) — Forward component events to Godot signals:
   - `Ball` — Delegates to PhysicsComponent; emits `BallHitPaddle`, `BallOutOfBounds`, `BallHitCeiling` signals
   - `Paddle` — Input handling, movement bounds, `Shrink()` action method
   - `Brick` — Has `Destroy()` method that emits `BrickDestroyed` signal and removes entity
   - `Wall` — Immobile boundary wall segment with collision and visual representation

5. **Game/** (Orchestration + Config):
   - `Controller` — Pure instantiation and orchestration; delegates all signal wiring to SignalWiringUtility; zero business logic
   - `Config` — Centralized constants; split into `Config.Brick` (entity) and `Config.BrickGrid` (infrastructure)

**Signal Flow Example:**
```
Ball hits brick → PhysicsComponent.HandleBrickCollision()
                  ├─ bounces ball (velocity.Y = -velocity.Y)
                  └─ calls brick.Destroy() → emits BrickDestroyed signal
                     ↓
                  BrickGrid listens → OnBrickDestroyed()
                  ├─ removes brick from dictionary
                  └─ emits BrickDestroyedWithColor(color)
                     ↓
                  GameStateComponent.OnBrickDestroyed() checks milestones
                  ├─ Emits SpeedIncreaseRequired → Controller wires DIRECTLY to PhysicsComponent.ApplySpeedMultiplier()
                  │  (no indirection through Ball!)
                  └─ Emits PaddleShrinkRequired → Controller wires to Paddle.Shrink()
```

**Rationale:** Per Nystrom's Component pattern, components own state + behavior. Entities are thin containers. Controller is mechanical wiring. Zero state duplication. Each concern has exactly one owner.

**Config Structure (Brick vs. BrickGrid):**
```csharp
// Config.Brick — entity-level properties
public static class Brick
{
    public static readonly Vector2 Size = ComputeBrickSize();
    public const int CollisionLayer = 1;
    public const int CollisionMask = 1;
}

// Config.BrickGrid — infrastructure layout properties
public static class BrickGrid
{
    public const int GridRows = 8;
    public const int GridColumns = 8;
    public const float HorizontalGap = 3f;
    public static readonly Vector2 GridStartPosition = new Vector2(20, 65);  // Y=65 provides clearance below UI
    public static readonly float GridSpacingX = Brick.Size.X + HorizontalGap;
    public static readonly float GridSpacingY = 20f;
}
```

- `Config.Brick` is owned by the entity and infrastructure (BrickGrid uses Brick.Size for layout)
- `Config.BrickGrid` is owned by BrickGrid infrastructure (controls grid layout and positioning)
- One-way dependency: Brick.ComputeBrickSize() references BrickGrid dimensions

---

## Development Workflow

### Build & Run

```bash
# Build the project
dotnet build

# Run in Godot editor
# Scene: main.tscn (auto-loaded)
```

### Key Files (Organized by Folder)

**Game/**
- **Controller.cs** — Pure signal wiring and instantiation via EntityFactoryUtility; zero business logic
- **Config.cs** — All constants; dynamic layout logic

**Components/**
- **PhysicsComponent.cs** — Ball physics, collision tracking, speed multipliers; calls brick.Destroy() on collision
- **GameStateComponent.cs** — Game rules, score, lives, speed/shrink logic; emits events for Controller to wire

**Infrastructure/**
- **BrickGrid.cs** — Brick grid management and destruction tracking; listens to Brick.BrickDestroyed signals
- **Walls.cs** — Boundary walls (positioned outside viewport)

**Utilities/**
- **EntityFactoryUtility.cs** — Factory for entity-component pair instantiation; creates all game entities
- **SignalWiringUtility.cs** — Stateless utility for all signal orchestration; 7 focused methods (one per concern domain)
- **BrickColorUtility.cs** — Color-to-config lookup (Red/Orange/Green/Yellow)

**Entities/**
- **Ball.cs** — Thin entity; delegates physics to PhysicsComponent; exposes component via GetPhysicsComponent()
- **Paddle.cs** — Input handling, movement, shrink action
- **Brick.cs** — Has Destroy() method that emits BrickDestroyed signal and removes entity from scene

### Git Workflow

Commits follow conventional format:
```
feat: new feature (e.g., "implement brick grid collision")
fix: bug fix
refactor: structural improvement
docs: documentation
```

---

## Objectives Roadmap

### Objective 1.1: Scene Structure & Component Architecture ✅
- Scene hierarchy (Controller → Entities)
- Frame-by-frame update loop
- Component-based architecture with state centralization
- Zero state duplication
- Pure signal wiring
- **Status:** Complete; MVP playable with solid architecture (January 5 refactor)

### Objective 1.2: Paddle Control ✅
- Keyboard input (arrow keys)
- Bounds checking
- Smooth movement

### Objective 1.3: Ball Physics (Basic) ✅
- Constant velocity with bouncing
- Wall collisions
- Paddle collisions
- Brick collisions (recently added)

### Objective 2.1: Scoring & Game State (Next)
- Hit counter and speed increases
- Scoring system (color-based points)
- Lives system
- Level complete detection

### Objective 2.2: Game State Machine (After scoring)
- States: Waiting, Playing, Game Over, Level Complete
- Transitions triggered by game events

---

## Design References

### Canonical Breakout (Original Arcade, 1976)

**Grid:** 8 rows × variable columns (2 rows each color)
**Colors (bottom to top):** Yellow (1pt), Green (3pts), Orange (5pts), Red (7pts)
**Ball Speed Increases:** After 4 hits, 12 hits, and hitting orange/red rows
**Paddle:** Shrinks to 50% after ball breaks through red row
**Scoring:** Max 864 points (2 screens × 432 each)
**Lives:** 3 turns to clear 2 screens

**Reference:** https://en.wikipedia.org/wiki/Breakout_(video_game)#Gameplay

---

## Patterns Applied (Nystrom)

| Pattern | Link | Usage |
|---------|------|-------|
| Update Method | https://gameprogrammingpatterns.com/update-method.html | Entity `_Process()` updates |
| Game Loop | https://gameprogrammingpatterns.com/game-loop.html | Controller drives loop |
| Observer | https://gameprogrammingpatterns.com/observer.html | C# events & Godot Signals |
| Component | https://gameprogrammingpatterns.com/component.html | PhysicsComponent, GameStateComponent (business logic components) |
| Factory | https://gameprogrammingpatterns.com/factory-method.html | EntityFactoryUtility creates entity-component pairs |
| Object Pool | https://gameprogrammingpatterns.com/object-pool.html | Brick grid Dictionary |

---

## Recent Improvements

### Session 2: Full Component Pattern Refinement (January 5, 2026)

**Phase 1-6:** Refactored from "signals only" to true **Nystrom's Component Pattern**:
- Created `PhysicsComponent` (ball physics, collision tracking, speed multipliers)
- Created `GameStateComponent` (game rules, score, lives, speed/shrink logic)
- Created `BrickGridComponent` (grid management, destruction handling)
- Renamed `GameOrchestrator` → `Controller` (accurate naming: dumb wiring)
- Eliminated all state duplication (e.g., shrink logic was in 2 places, now in 1)
- Controller reduced from 100+ lines with decisions to ~50 lines of pure wiring

**Phase 7:** Eliminated unnecessary pass-through method indirection:
- Discovered `Ball.ApplySpeedMultiplier()` was a pass-through to `PhysicsComponent`
- Violated principle: behavior owner (PhysicsComponent) should be wired directly
- Created `EntityComponent.CreateBallWithPhysics()` returning `(Ball, PhysicsComponent)` tuple
- Updated Controller to wire `SpeedIncreaseRequired` directly to `PhysicsComponent.ApplySpeedMultiplier()`
- Removed `Ball.ApplySpeedMultiplier()` pass-through; added `Ball.GetPhysicsComponent()`

**Phase 8:** Reorganized folder structure by architectural role (January 6):
- Reclassified components by role instead of mixing all in one folder
  - **Components/** = business logic (PhysicsComponent, GameStateComponent, future Sound/Rendering/AI)
  - **Infrastructure/** = world structures (Walls, BrickGrid, future LevelLayout/Environment)
  - **Utilities/** = helper functions (EntityFactoryUtility, BrickColorUtility)
  - **Game/** = orchestration (Controller) + config (Config)
- Renamed `EntityComponent` → `EntityFactoryUtility` (factory utility, not component)
- Renamed `BrickGridComponent` → `BrickGrid` (infrastructure, not business logic component)
- Updated all usages to reflect new locations

**Phase 9:** Fixed brick destruction bug:
- Discovered bricks weren't being destroyed on collision
- Root cause: `PhysicsComponent.HandleBrickCollision()` only bounced ball, never destroyed brick
- Added `Brick.Destroy()` method that emits `BrickDestroyed` signal and calls `QueueFree()`
- Updated `HandleBrickCollision()` to call `brick.Destroy()` after bouncing
- Signal flow now works: Brick.Destroy() → BrickGrid listener → emits BrickDestroyedWithColor → GameState rules applied

**Result:**
- ✅ Components own state AND logic (not just data containers)
- ✅ Entities are thin; they forward events only
- ✅ Controller is mechanical signal wiring only (zero state, zero decisions)
- ✅ **No indirection**: all signals wired directly to behavior owners
- ✅ Folder organization reflects architectural intent (Components = logic, Infrastructure = structures, Utilities = helpers)
- ✅ Brick destruction works completely: hit → destroy → signal → rules applied
- ✅ Speed multipliers persist across ball resets
- ✅ Paddle shrinks exactly once (via flag guard)
- ✅ No state polling; pure event-driven
- ✅ **Architecture is solid, complete, and ready for feature development**

### Session 3: UI Component & Config Restructuring (January 8-10, 2026)

**Phase 1: UIComponent Implementation**
- Created `UIComponent` (CanvasLayer) for HUD display (score, lives)
- Implements `OnScoreChanged(int)` and `OnLivesChanged(int)` event handlers
- Implements `ShowGameOverMessage()` for game-over display
- Wired to `GameStateComponent.ScoreChanged` and `LivesChanged` events in Controller

**Phase 2: UI Layout Fix**
- **Problem:** UIComponent labels (Y=10, 32px font) overlapped brick grid starting at Y=40
- **Solution:** Offset brick grid Y position via `Config.BrickGrid.GridStartPosition.Y`: 40 → 65
- **Benefit:** ~55px clearance below UI labels; clean visual separation without moving UI
- **Approach:** Configuration-driven solution (better than moving UI labels)

**Phase 3: Config Restructuring (Brick vs. BrickGrid)**
- **Problem:** Single `Config.Brick` class mixed entity properties with infrastructure layout
- **Split into two independent sections:**
  - `Config.Brick` — owns entity properties (Size, CollisionLayer/Mask)
  - `Config.BrickGrid` — owns infrastructure layout (GridRows, GridColumns, GridStartPosition, GridSpacing)
- **Updated all usages:**
  - `Brick.cs` (entity) → references `Config.Brick.*`
  - `BrickGrid.cs` (infrastructure) → references `Config.BrickGrid.*`
  - `Config.Brick.ComputeBrickSize()` → references `Config.BrickGrid` for grid dimensions
- **Benefit:** Clear separation of concerns; independent tweaking of either subsystem; one-way dependency is minimal

**Phase 4: Physics Refinement & Arcade-Authentic Features (January 10)**
- **Speed Magnitude Preservation:**
  - Fixed speed diffusion on bounces by implementing Pythagorean magnitude preservation
  - `HandlePaddleBounce()` now guarantees `velocity.Length() == speedMagnitude` after reconstruction
  - Increased speed multiplier from 1.05x to 1.15x (15% per milestone) for noticeable difficulty escalation
  - Speed multipliers now persist correctly across all bounce types (wall, paddle, brick)
- **Paddle Speed Compensation:**
  - Added `paddleSpeedMultiplier` to maintain fairness as ball accelerates
  - 10% paddle speed increase at same milestones as ball speed (4, 12 hits, orange/red rows)
  - Wired via `GameStateComponent.PaddleSpeedIncreaseRequired` event
- **Randomized Launch Angles:**
  - Ball reset now randomizes launch angle between 60° and 120° (downward toward paddle)
  - Preserves accumulated speed multipliers while varying direction for replayability
- **Color-Based Audio/Visual Feedback:**
  - Created `SoundComponent` with 8-bit square wave synthesis
  - Polyphonic cracking based on brick color (red=4 cracks, orange=3, green=2, yellow=1)
  - `UIComponent` flashes score synchronized with brick color (same count)
  - Both components subscribe to `BrickDestroyedWithColor` event (no state drilling)
- **UI Polish:**
  - Zero-padded 3-digit score display ("000" initialization)
  - Game over label centered using anchor-based stretching (accounts for wall offset)
  - Bold sans-serif arcade font for authentic feel
- **Paddle Shrink Fix:**
  - Fixed Godot error by using `CallDeferred()` for collision shape modification during physics query
  - Shrink now executes correctly after red row breakthrough + ceiling hit
  - 60% width reduction (changed from 50% for better playability)

**Result:**
- ✅ HUD displays score and lives in real-time with arcade-style formatting
- ✅ UI properly positioned without overlap, centered game over message
- ✅ Config architecture reflects distinction between entity and infrastructure
- ✅ Brick and Grid configurations are independently maintainable
- ✅ Speed multipliers preserved across all bounces with mathematical rigor
- ✅ Paddle speed tracks ball speed for fair gameplay
- ✅ Randomized launch angles improve replayability
- ✅ Color-synchronized audio/visual feedback without state drilling
- ✅ Deferred paddle shrink execution avoids physics query conflicts
- ✅ **MVP is fully playable with arcade-authentic physics and polish**

### Session 3 (Continued): Signal Wiring Refactoring (January 10, 2026)

**Phase 5: Separation of Concerns in Signal Orchestration**
- **Problem:** Controller._Ready() had ~80 lines of mixed signal wiring scattered across multiple domains (game rules, brick events, UI, ball physics, sound, game state)
- **Solution:** Created `SignalWiringUtility.cs` (stateless utility following EntityFactoryUtility pattern) with 7 focused static methods:
  - `WireGameRules()` — Speed increases, paddle shrinking
  - `WireBrickEvents()` — Destruction → game rules/scoring/UI/sound/level complete
  - `WireUIEvents()` — Score display, lives display, flashing animations, game over message
  - `WireBallEvents()` — Ball collisions → game state (lives management)
  - `WireBallSoundEvents()` — Ball collisions → audio feedback
  - `WireGameStateSoundEvents()` — Game state transitions → audio feedback
  - `WireGameOverState()` — Game over → entity disabling (ball/paddle)
- **Refactored Controller:**
  - Reduced _Ready() from ~80 lines of wiring to ~20 lines of utility method calls
  - Now follows true separation of concerns: pure instantiation + pure orchestration
  - Zero signal handling logic in Controller
  - All wiring concerns are visualized in one utility file (single place to see all connections)
- **Architecture Benefits:**
  - Each utility method handles exactly one domain (no mixed concerns)
  - Each method is self-contained and testable in isolation
  - Zero state ownership (stateless utility)
  - No hidden dependencies or interdependencies between wiring methods
  - Follows same pattern as EntityFactoryUtility (established precedent)
  - Controller becomes clean orchestrator (matches Nystrom's vision)

**Result:**
- ✅ Signal orchestration extracted to focused, stateless utility
- ✅ Controller is now truly thin (instantiation + 7 orchestration calls)
- ✅ All signal connections visible in one place (SignalWiringUtility) for easy comprehension
- ✅ Each concern (game rules, UI, sound, etc.) handled by separate utility method
- ✅ Architecture patterns fully applied (Component, Factory, Utility patterns)
- ✅ **Code is now elegant, maintainable, and architecturally sound**

---

## Next Session: Future Objectives

Architecture is solid and MVP is complete. Ready for additional features.

### Potential Next Steps
1. **Level Progression** — Multiple brick layouts with increasing difficulty
2. **Ball Speed Cap** — Prevent unplayable speeds in extremely long games
3. **Replay/Restart** — Reset game state without restarting application
4. **Game Initialization Flow** — Wait for player input before first ball launch
5. **Additional Power-ups** — Multi-ball, paddle extension, etc. (non-canonical)

**Architecture Note:** Current component pattern scales well for all these features. New functionality can be added as components with signal wiring in Controller.

---

## Resources

- **Godot 4.5 Docs:** https://docs.godotengine.org/en/4.5/
- **Game Programming Patterns:** https://gameprogrammingpatterns.com/
- **2D Game Development Curriculum:** See `.context/A Curriculum for 2D Game Development Mastery.md`
- **Development Log:** See `.context/OBJECTIVE_1_1_DEVLOG.md` (not committed; local reference)

---

## Notes for Contributors (or Future Self)

- **DRY Principle:** Configuration is computation-based. Changing `ViewportWidth` or `GridColumns` in GameConfig auto-scales brick layout.
- **Signal Convention:** Signals emit meaningful names (e.g., `BallHitPaddle`), not generic notifications.
- **No Editor Scenes:** All structure defined in C# code, not Godot editor, to deepen understanding of node relationships.
- **Testing:** Currently manual (play in editor). As complexity grows, consider unit tests for physics logic.

---

## License & Attribution

This project is an educational implementation. Breakout is a trademark of Atari, Inc. This code is for learning purposes.

**Learning Resources Credited:**
- Robert Nystrom (Game Programming Patterns)
- Godot Foundation (Engine & Documentation)
- Atari, Inc. (Original Breakout Design)
