# Breakout

A faithful recreation of Atari's classic *Breakout* (1976) in **Godot 4.5 with C#**, built as a hands-on learning exercise in 2D game development fundamentals and architectural patterns.

**Current Status:** Objective 1.1 complete (MVP playable) — Nystrom's Component Pattern fully implemented (January 5, 2026).

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

### MVP (Objective 1.1 Complete ✅)

- **Entities:** Paddle (player-controlled), Ball (physics-driven), Brick Grid (8×8, destructible), Walls (boundary)
- **Gameplay:** Ball bounces off walls, paddle, and bricks; bricks destroyed on contact; speed increases (4, 12 hit milestones) working; paddle shrinking working
- **Collision Detection:** Signal-based (`AreaEntered`/`AreaExited` events) instead of polling
- **Components:** PhysicsComponent (ball physics, speed multipliers), GameStateComponent (rules & state), BrickGridComponent (grid management), EntityComponent (factory)
- **Configuration:** Centralized in `Config.cs`; dynamic brick grid spacing
- **Brick Colors:** Type-safe enum with scoring metadata (Red=7pts, Orange=5pts, Green=3pts, Yellow=1pt)
- **Architecture:** Nystrom's Component Pattern — plain C# components own state & logic; thin entities forward events; pure signal wiring; direct component access

### Not Yet Implemented

- Scoring display UI
- Lives display UI
- Game state machine (Playing, GameOver, Level Complete)
- Level progression

---

## Architecture Notes

### Current Design: Nystrom's Component Pattern

**Pattern:** Component (plain C# classes owning state + logic) + Observer (Godot Signals + C# Events)

**Architecture Layers:**

1. **Components (Plain C# Classes)** — Own state and logic, emit C# events:
   - `PhysicsComponent` — Ball velocity, position, collision tracking, speed multipliers, bounce logic
   - `GameStateComponent` — Score, lives, hit count, speed/shrink decision logic
   - `BrickGridComponent` — Brick grid management and destruction tracking
   - `EntityComponent` — Factory for entity-component pair instantiation

2. **Entities (Thin Node2D Containers)** — Forward component events to Godot signals:
   - `Ball` — Delegates to PhysicsComponent; emits `BallHitPaddle`, `BallOutOfBounds`, `BallHitCeiling` signals
   - `Paddle` — Input handling, movement bounds, `Shrink()` action method
   - `Brick` — Collision detector; emits `BrickDestroyed` signal
   - `Walls` — Stateless; boundary nodes only

3. **Controller (Pure Signal Wiring)** — Zero business logic:
   - Instantiates components and entities via `EntityComponent`
   - Wires C# events to Godot signals to other component methods
   - No state; no decisions; mechanical coordination only

**Signal Flow Example:**
```
Brick destroyed → BrickGridComponent emits BrickDestroyedWithColor
                  ↓
                  GameStateComponent.OnBrickDestroyed() checks milestones
                  ├─ Emits SpeedIncreaseRequired → Controller wires DIRECTLY to PhysicsComponent.ApplySpeedMultiplier()
                  │  (no indirection through Ball!)
                  └─ Emits PaddleShrinkRequired → Controller wires to Paddle.Shrink()
```

**Rationale:** Per Nystrom's Component pattern, components own state + behavior. Entities are thin containers. Controller is mechanical wiring. Zero state duplication. Each concern has exactly one owner.

---

## Development Workflow

### Build & Run

```bash
# Build the project
dotnet build

# Run in Godot editor
# Scene: main.tscn (auto-loaded)
```

### Key Files
- **Game/Controller.cs** — Pure signal wiring and instantiation via EntityComponent; zero business logic
- **Game/Config.cs** — All constants; dynamic layout logic
- **Components/EntityComponent.cs** — Factory component; creates entity-component pairs and manages scene tree instantiation
- **Components/PhysicsComponent.cs** — Ball physics, collision tracking, speed multipliers; direct target of Controller wiring
- **Components/GameStateComponent.cs** — Game rules, score, lives, speed/shrink logic; emits events for Controller to wire
- **Components/BrickGridComponent.cs** — Brick grid management and destruction tracking
- **Entities/Ball.cs** — Thin entity; delegates physics to PhysicsComponent; exposes component via GetPhysicsComponent()
- **Entities/Paddle.cs** — Input handling, movement, shrink action
- **Entities/Brick.cs** — Collision detector, destruction signal
- **Infrastructure/Walls.cs** — Boundary walls (positioned outside viewport)

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
| Component | https://gameprogrammingpatterns.com/component.html | PhysicsComponent, GameStateComponent, BrickGridComponent (implemented) |
| Factory | https://gameprogrammingpatterns.com/factory-method.html | EntityComponent creates entity-component pairs |
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

**Result:**
- ✅ Components own state AND logic (not just data containers)
- ✅ Entities are thin; they forward events only
- ✅ Controller is mechanical signal wiring only (zero state, zero decisions)
- ✅ **No indirection**: all signals wired directly to behavior owners
- ✅ Speed multipliers persist across ball resets
- ✅ Paddle shrinks exactly once (via flag guard)
- ✅ No state polling; pure event-driven
- ✅ **Architecture is solid, complete, and ready for feature development**

---

## Next Session: Objective 2.1 (Scoring & Game State UI)

Architecture is solid and ready for feature addition. No refactoring needed.

### Recommended Tasks
1. Create UI layer (CanvasLayer with labels)
2. Bind `ScoreChanged` event to score label
3. Bind `LivesChanged` event to lives label
4. Implement game-over detection when lives reach 0
5. Test speed increases and paddle shrinking
6. **Time:** 2-3 hours | **Benefit:** Game is playable and testable

**Why architecture is ready:** Components emit all necessary events (ScoreChanged, LivesChanged, BallOutOfBounds). UI can simply listen without any architectural changes.

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
