# Breakout in the Key of C#

![Breakout Poster](https://flyers.arcade-museum.com/videogame-flyers/0/breakout-00145-01.jpg)

A faithful recreation of Atari's classic *Breakout* (1976) in **Godot 4.5 with C#**, demonstrating pattern-driven game architecture following Robert Nystrom's *Game Programming Patterns*.

**Current Status:** ✅ **Objective 1.1++ Complete** — Fully playable MVP with arcade-authentic physics, UI, audio, and transitions (January 2026).

---

## Features

### Core Gameplay
- **8×8 Brick Grid** — Canonical Breakout color scheme (red→orange→green→yellow, top to bottom)
- **Physics-Based Ball** — Speed magnitude preservation across all bounces; randomized launch angles (60°-120°)
- **Speed Progression** — 15% increases at canonical milestones (4 hits, 12 hits, orange/red row contact)
- **Paddle Mechanics** — Player-controlled with 10% speed compensation matching ball acceleration; shrinks 40% on red row breakthrough
- **Lives System** — 3 lives; ball resets on out-of-bounds with speed preservation
- **Scoring** — Color-based points (Red=7, Orange=5, Green=3, Yellow=1) with zero-padded 3-digit display

### Polish Features
- **Color-Synchronized Feedback** — Score flashing and polyphonic 8-bit crack sounds based on brick color (red=4, orange=3, green=2, yellow=1)
- **Smooth Transitions** — Fade-in effects for destroyed bricks on ball restart
- **Auto-Play Test Mode** — Press spacebar to toggle full-width paddle for testing
- **Deferred Physics** — Proper collision shape modifications to avoid Godot query errors

### Not Yet Implemented
- Level progression and level complete state
- Ball speed cap for extreme scenarios
- Replay/restart functionality without application restart
- Game initialization flow (wait for player input before launch)

---

## Architecture

This project implements **Nystrom's Component Pattern** ([Game Programming Patterns](https://gameprogrammingpatterns.com/)) with clear separation of concerns:

### Folder Organization

```
Components/          # Business logic (plain C# classes owning state + behavior)
├─ PhysicsComponent     — Ball velocity, collision detection, bounce logic, speed multipliers
├─ GameStateComponent   — Score, lives, hit count, canonical Breakout rules
├─ UIComponent          — HUD display (score, lives), game over messaging
├─ SoundComponent       — 8-bit audio synthesis, polyphonic brick cracks
└─ TransitionComponent  — Fade effects for destroyed bricks

Infrastructure/      # World structures (entity collections forming environment)
├─ BrickGrid            — Brick grid management, destruction tracking
└─ Walls                — Boundary wall instantiation and management

Utilities/           # Helper functions (stateless)
├─ EntityFactoryUtility    — Factory for entity-component pair creation
├─ SignalWiringUtility     — Pure signal orchestration (7 focused wiring methods)
└─ BrickColorUtility       — Color-to-config lookup

Entities/            # Godot Node2D containers (thin wrappers forwarding events)
├─ Ball                 — Delegates physics to PhysicsComponent; emits signals
├─ Paddle               — Input handling, movement, shrink action
├─ Brick                — Destroy() method, emits destruction signal
└─ Wall                 — Immobile boundary segment

Game/                # Orchestration + configuration
├─ Controller           — Pure instantiation and signal wiring (zero business logic)
└─ Config               — Centralized constants (Brick, BrickGrid, Paddle sections)
```

### Key Principles

**Component Pattern:**
- ✅ Plain C# components own state AND logic (not just data)
- ✅ Entities are thin containers that forward component events to Godot signals
- ✅ Direct component access (no unnecessary pass-through methods)
- ✅ Single source of truth per concern (zero state duplication)

**Signal Flow Example:**
```
Ball hits brick → PhysicsComponent.HandleBrickCollision()
                  ├─ Bounces ball (preserves speed magnitude)
                  └─ Calls brick.Destroy()
                     ↓
                  BrickGrid.OnBrickDestroyed()
                  ├─ Removes brick from grid
                  └─ Emits BrickDestroyedWithColor(color)
                     ↓
                  GameStateComponent.OnBrickDestroyed(color)
                  ├─ Updates score/lives/hits
                  ├─ Checks milestones
                  └─ Emits SpeedIncreaseRequired/PaddleShrinkRequired
                     ↓
                  Controller wires directly to:
                  ├─ PhysicsComponent.ApplySpeedMultiplier()
                  └─ Paddle.Shrink()
```

**Observer Pattern:**
- C# events for component-to-component communication
- Godot signals for entity-to-entity communication
- Pure event-driven (no state polling)

**Factory Pattern:**
- EntityFactoryUtility creates all entity-component pairs
- Returns tuples for direct component access when needed
- SignalWiringUtility handles all signal orchestration

---
## Getting Started

### Prerequisites
- .NET 8.0 or later
- Godot 4.5 with .NET support

### Build & Run
```bash
# Build the project
dotnet build

# Open in Godot Editor
# Load main.tscn and run (F5)
```

### Controls
- **Arrow Keys** — Move paddle left/right
- **Spacebar** — Toggle auto-play test mode (full-width paddle)

---

## Configuration

All game parameters are centralized in [Config.cs](Game/Config.cs):

```csharp
// Entity properties
Config.Brick.Size
Config.Paddle.InitialSize, .ShrinkFactor, .Speed

// Infrastructure layout
Config.BrickGrid.GridRows, .GridColumns, .GridStartPosition
// Physics and gameplay
Config.Ball.InitialSpeed, .SpeedMultiplier
Config.GameState.InitialLives, .SpeedMilestones

// Viewport and visual
Config.Viewport.Size
```

**Design Note:** Config uses computation-based values (e.g., brick size calculated from viewport width and grid columns) to minimize cascading changes when tweaking parameters.

---

## Development Patterns

This project implements patterns from Robert Nystrom's [Game Programming Patterns](https://gameprogrammingpatterns.com/):

| Pattern | Usage |
|---------|-------|
| [Component](https://gameprogrammingpatterns.com/component.html) | PhysicsComponent, GameStateComponent (business logic components) |
| [Observer](https://gameprogrammingpatterns.com/observer.html) | C# events & Godot Signals for event-driven communication |
| [Update Method](https://gameprogrammingpatterns.com/update-method.html) | Entity `_Process()` updates each frame |
| [Game Loop](https://gameprogrammingpatterns.com/game-loop.html) | Controller drives the main loop |
| [Factory](https://gameprogrammingpatterns.com/factory-method.html) | EntityFactoryUtility creates entity-component pairs |
| [Object Pool](https://gameprogrammingpatterns.com/object-pool.html) | Brick grid Dictionary for efficient lookup |

---

## Canonical Breakout Reference

**Grid:** 8 rows × 8 columns (2 rows per color)  
**Colors (bottom→top):** Yellow (1pt), Green (3pts), Orange (5pts), Red (7pts)  
**Speed Increases:** After 4 hits, 12 hits, orange row contact, red row contact  
**Paddle Shrink:** After ball breaks through red row and hits ceiling  
**Lives:** 3 turns  
**Max Score:** 336 points per screen (1 row red × 8 bricks × 7pts + 2 rows orange × 8 × 5pts + 2 rows green × 8 × 3pts + 2 rows yellow × 8 × 1pt)

**Reference:** [Wikipedia: Breakout (video game)](https://en.wikipedia.org/wiki/Breakout_(video_game)#Gameplay)

---

## Project Status

### Completed
- ✅ **Objective 1.1:** Scene structure, game loop, component architecture
- ✅ **Objective 1.2:** Paddle control with speed compensation
- ✅ **Objective 1.3:** Ball physics with speed magnitude preservation
- ✅ **Objective 2.1:** Scoring system and game state management
- ✅ **Polish:** UI, audio, transitions, auto-play test mode

### Next Steps
- Level progression and level complete state
- Ball speed cap for extreme scenarios
- Replay/restart functionality
- Game initialization flow (wait for player input)

**Architecture Note:** Current component pattern scales well for additional features. New functionality can be added as components with signal wiring.

---

## Resources

- **Godot 4.5 Documentation:** https://docs.godotengine.org/en/4.5/
- **Game Programming Patterns:** https://gameprogrammingpatterns.com/
- **Development Log:** See [.context/OBJECTIVE_1_1_DEVLOG.md](.context/OBJECTIVE_1_1_DEVLOG.md) for detailed implementation history

---

## License & Attribution

This project is an educational implementation. Breakout is a trademark of Atari, Inc.

**Learning Resources:**
- Robert Nystrom — *Game Programming Patterns*
- Godot Foundation — Engine & Documentation
- Atari, Inc. — Original Breakout Design
