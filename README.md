# Breakout

A faithful recreation of Atari's classic *Breakout* (1976) in **Godot 4.5 with C#**, built as a hands-on learning exercise in 2D game development fundamentals and architectural patterns.

**Current Status:** MVP complete (Objective 1.1) — playable end-to-end with basic physics.

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
- **Gameplay:** Ball bounces off walls, paddle, and bricks; bricks destroyed on contact
- **Collision Detection:** Signal-based (`AreaEntered`/`AreaExited` events) instead of polling
- **Configuration:** Centralized in `Config.cs`; dynamic brick grid spacing
- **Brick Colors:** Type-safe enum with scoring metadata (Red=7pts, Orange=5pts, Green=3pts, Yellow=1pt)
- **Architecture:** Self-contained entities communicating via Godot Signals (Observer pattern)

### Not Yet Implemented

- Scoring system
- Speed increases (4, 12 hit milestones)
- Paddle shrinking
- Lives / game state machine
- Level progression

---

## Architecture Notes

### Current Design: Signals + Self-Contained Entities

**Pattern:** Update Method + Observer (Godot Signals)

Each entity owns its state and behavior:
- `Ball._Process()` updates position, detects collisions, bounces
- `Paddle._Process()` handles input, bounds-checking
- `GameOrchestrator` manages entity instantiation and signal connections

**Rationale:** Per Nystrom's warning against deep inheritance hierarchies, we use composition and signals to decouple entities.

### Identified Issue: Physics Logic Duplication

Bounce logic appears in three places (walls, paddle, bricks), violating DRY. This is **acceptable for MVP** but signals a need for refactoring before adding game rules (speed increases, paddle effects, angle-aware bounces).

**Planned Solution (Next Refactor):** Extract `PhysicsComponent` to centralize all bounce and velocity logic. This aligns with Nystrom's **Component** pattern (https://gameprogrammingpatterns.com/component.html).

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
- **Game/Orchestrator.cs** — Main orchestration; creates entities and wires signals
- **Game/Config.cs** — All constants; dynamic layout logic
- **Game/BrickColor.cs** — Brick color enum, scoring config, and row-to-color mapping
- **Entities/Ball.cs** — Physics, collision detection, bounce logic
- **Entities/Paddle.cs** — Input handling, movement constraints
- **Entities/Brick.cs** — Brick state, destruction signals
- **GameConfig.cs** — All constants; dynamic layout computation
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

### Objective 1.1: Scene Structure & Game Loop ✅
- Scene hierarchy (Orchestrator → Entities)
- Frame-by-frame update loop
- Basic collision detection
- **Status:** Complete; MVP playable

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
| Game Loop | https://gameprogrammingpatterns.com/game-loop.html | Orchestrator drives loop |
| Observer | https://gameprogrammingpatterns.com/observer.html | Godot Signals for events |
| Component | https://gameprogrammingpatterns.com/component.html | Planned: PhysicsComponent |
| Object Pool | https://gameprogrammingpatterns.com/object-pool.html | Brick grid Dictionary |

---
Recent Improvements

### BrickColor Enum Refactor (Latest)

Introduced type-safe color handling:
- `BrickColor` enum (Red, Orange, Green, Yellow) replaces hardcoded array indices
- `BrickColorConfig` struct bundles color, visual representation, and scoring
- `BrickColorDefinitions` helper with `GetColorForRow()` and `GetConfig()` for easy lookup
- Canonical Breakout scoring now defined: Red=7pts, Orange=5pts, Green=3pts, Yellow=1pt

**Impact:** Clean foundation for scoring system implementation. No duplicate color definitions. Easy to extend with new color properties.

---

## 
## Next Session: Recommended Focus

### Option A: Refactor to Components (Recommended)
1. Create `PhysicsComponent` class
2. Move bounce logic from Ball into component
3. Enhance bounce to consider paddle velocity and contact point
4. Wire into Orchestrator for rule modifications (speed increases, etc.)
5. **Time:** 2-3 hours | **Benefit:** Unblocks all downstream features cleanly

### Option B: Continue MVP Features
1. Add scoring display
2. Implement hit counter and speed increases
3. Add lives system
4. **Time:** 2-3 hours | **Benefit:** Game feels more complete sooner

**Recommendation:** Option A first. The architectural pain is clear (duplicate bounce logic), and refactoring now prevents larger pain when adding speed/difficulty modifiers.

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
