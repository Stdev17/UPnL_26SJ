# SIGNAL RUSH Playable Slice Implementation Plan

**Spec:** `Docs/Plans/2026-08-21-playable-fanout-design.md` and the corrected contracts in `Docs/NEXT_STEP_GDD_V0.2_REQUEST.md`

**Goal:** Deliver one graybox scene that compiles and supports automatic running, correction, jump, break/parry, combo speed, hit recovery, fall respawn, goal finish, result display, and Attack-to-restart.

**Global constraints:** Runtime input is exactly `Move`, `Jump`, `Attack`. There is no health count. `PlayerStatus` owns status and timers. Shared Unity YAML/assets are integration-owned and serial. Every task commits tests and production code in its own worktree; `main` receives only a verified wave branch by fast-forward.

## Queue

| ID | Status | Depends on | Owner paths | Verification |
|---|---|---|---|---|
| T00 tuning | complete | baseline | `Runtime/Tuning`, `Tests/EditMode/Tuning` | EditMode tuning tests: 2 passed |
| T01 combo | complete | T00 | `Runtime/Combo`, `Tests/EditMode/Combo` | EditMode combo tests: 5 passed |
| T02 status | complete | T00 | `Runtime/Player/PlayerStatus.cs`, `Tests/EditMode/Player/PlayerStatusTests.cs` | EditMode status tests: 5 passed |
| T03 targets | complete | T00 | `Runtime/Combat/BreakableObstacle.cs`, `Projectile.cs`, matching tests | EditMode target tests: 3 passed |
| T04 motor | complete | T01,T02 | `Runtime/Player/PlayerMotor2D.cs`, `Runtime/World/JumpReachability.cs`, matching tests | EditMode motor/reachability tests: 15 passed |
| T05 combat | complete | T01,T02,T03 | `Runtime/Player/PlayerCombat.cs`, matching tests | EditMode combat tests: 7 passed |
| T06 run | complete | T02 | `Runtime/Run`, `Tests/EditMode/Run` | EditMode lifecycle tests: 7 passed; Update delegation deferred to T10 PlayMode |
| T07 world | complete | T03,T04 | `Runtime/World` except `JumpReachability.cs`, matching tests | EditMode world tests: 19 passed; Combat missed tests: 5 passed |
| T08 adapter/views | complete | T05,T06 | `Runtime/Player/PlayerInput.cs`, `Runtime/UI`, matching tests | EditMode input/motor tests: 12 passed; view tests: 4 passed |
| T09 integration | ready | T04-T08 | input asset, layers, tuning asset, prefabs, playable scene | batchmode compile + EditMode |
| T10 acceptance | pending | T09 | PlayMode tests and minimal tuning fixes | EditMode + PlayMode |

All paths above are relative to `Assets/Game/Scripts` or `Assets/Game/Tests` as appropriate. Workers must not edit scenes, prefabs, `.inputactions`, `ProjectSettings`, packages, this plan, or another task's paths.

## Task contracts

### T00 — Complete tuning inputs

Add only values needed by the approved behavior: horizontal correction speed, jump velocity, falling gravity multiplier, attack window, hit lock, invulnerability, and target run duration. Validate positive values and retain existing defaults. Write failing tests first.

### T01 — Combo rules

Implement `ComboCounter` with cap 20. Break/parry each increment once when called. Hit records `Interrupted`, resets `Current`, preserves `Best`, and emits one change. `SpeedMultiplier` linearly maps base speed to max speed at combo 20.

### T02 — Player status

Implement `PlayerState { Active, Hit, Respawning, Dead }` and `PlayerStatus`. Damage is ignored during invulnerability/respawn/dead; accepted damage enters Hit and starts hit-lock plus invulnerability timers. Respawn locks for `RespawnLockSeconds`; dead is terminal until reset. No health API.

### T03 — One-shot combat targets

Implement idempotent `BreakableObstacle.TryBreak()` and `Projectile.TryParry()`. Each successful first resolution emits exactly one event; later calls return false. Projectile player-hit resolution is also one-shot.

### T04 — Motor and shared reachability math

Implement automatic rightward Rigidbody2D movement plus clamped correction input, grounded jump, extra fall gravity, control lock, safe-position capture, and zero-velocity respawn. `JumpReachability` must use the same jump/gravity model to bound chunk height and gap.

### T05 — Player combat

One request opens an attack window and resolves every overlapping unresolved target once. Buffer at most one additional attack. Hit/respawn/dead interruption closes the window and clears the buffer. Avoid global registries; use the serialized attack hitbox overlap results.

### T06 — Run lifecycle

Implement running/respawning/finished lifecycle, elapsed time, restart, and one final result. Queue goal/dead requests per fixed step so GoalReached wins a same-step tie. `GoalTrigger` emits once per reset.

### T07 — World lifetime and spawning

Implement chunk role/placement, sniper warning and one active projectile, cleanup blocking while warning/projectile is unresolved, and bounded role-prefab selection. Reject missing required lists clearly. Use `JumpReachability` before placement.

### T08 — Input adapter and views

Read only `Move`, `Jump`, `Attack`. During a finished run, Attack calls restart instead of combat. HUD/result components only observe and render existing state.

### T09 — Serial Unity integration

Create the three-action gameplay map, gameplay layers, tuning asset, graybox prefabs, and `SCN_SignalRush_Playable`. Wire serialized references in the Editor. No parallel agent edits shared YAML.

### T10 — Acceptance

Add the smallest PlayMode checks proving the scene loads and the critical loop is wired. Run all EditMode and PlayMode tests. Record result XML paths and leave every worktree clean.

## Orchestrator loop

For each wave: create `agent/wave-N` from the current main; create ready task branches/worktrees from that wave base; dispatch up to three non-overlapping workers; require tests, `git diff --check`, commit, and report; review and merge task branches into the wave branch; run the combined suite; fast-forward main; update this queue; clean worktrees and branches; repeat until T10 completes or a reproducible blocker remains.
