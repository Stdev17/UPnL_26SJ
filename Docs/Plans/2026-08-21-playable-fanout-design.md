# SIGNAL RUSH Playable Slice and Fanout Design

## Goal

Build a playable graybox vertical slice from the approved GDD contracts, while proving a worktree fanout loop that can drain a dependency-aware task queue without allowing parallel Unity YAML edits.

## Product decisions

- Runtime input has exactly three actions: `Move`, `Jump`, and `Attack`.
- `Move` is left/right correction while the runner advances automatically. `Jump` is a single grounded jump. `Attack` breaks obstacles and parries every unresolved projectile present at that input instant.
- A finished run reuses `Attack` as confirmation to restart; there is no fourth `Restart` action.
- `PlayerHealth` is replaced by `PlayerStatus`. It owns `Active`, `Hit`, `Respawning`, and `Dead` state, invulnerability, and control locking. It has no `CurrentHealth` or `HealthChanged` API.
- A hit is ignored while invulnerable, otherwise enters `Hit`, clears combo and attack buffering, and temporarily locks control. Falling enters `Respawning`, waits `RespawnLockSeconds`, and returns the player to the latest safe position without health loss.
- Goal completion wins when goal and failure are requested in the same physics step. A run finishes once.
- The first playable artifact is graybox-quality. It uses simple runtime visuals and the approved generated anchor art where it already fits; new final art is not a prerequisite.

## Runtime ownership

### Tuning

`SignalRushTuning` remains the single owner of variable game-feel values. Existing approved values stay unchanged. The playable slice adds only values required to run the existing GDD behavior: jump velocity, extra falling gravity, attack window, hit lock, invulnerability, and target run duration. Combo cap remains the fixed GDD value `20` and is not configurable.

### Combo

`ComboCounter` owns current, best, interrupted, and speed multiplier. Break and parry increment once per resolved target. Hit atomically records the interrupted value and resets current to zero. At combo 20 the multiplier maps `BaseRunSpeed` to `MaxRunSpeed`.

### Player

`PlayerMotor2D` owns Rigidbody2D movement, grounded state, safe position, jump, speed multiplier, control lock, and respawn. `PlayerStatus` owns semantic state and timers. `PlayerInput` reads only the three Input System actions and sends commands to motor/combat or reuses `Attack` to confirm a finished run.

### Combat

`BreakableObstacle.TryBreak()` and `Projectile.TryParry()` are one-shot operations. `PlayerCombat.RequestAttack()` opens one attack window, resolves all overlapping obstacles once, and resolves all active projectiles once. One extra attack may be buffered during the current window; status interruption discards it.

### World

`Chunk` owns role, placement, and cleanup eligibility. `Sniper` owns warning and one unresolved projectile. `ChunkSpawner` chooses from the three role-specific prefab lists, rejects missing required candidates, and places gameplay chunks within the reachability limits calculated from the same jump model used by the motor.

### Run and view

`RunController` owns running/respawning/finished state, elapsed time, and a single final result. `GoalTrigger` publishes once. `RunHud` and `ResultView` observe state only. Scene wiring uses serialized references; no service locator, global event bus, or persistent singleton is introduced.

## Unity asset integration

Code-only features and EditMode tests may run concurrently. The following remain integration-owned and serial:

- `Assets/Settings/InputSystem_Actions.inputactions`
- `ProjectSettings/TagManager.asset` and other `ProjectSettings/*`
- shared scene, prefab, ScriptableObject, and their `.meta` files
- packages and render settings

The integration pass creates the gameplay layers, three-action input map, tuning asset, graybox prefabs, and `SCN_SignalRush_Playable`. Prefabs follow the approved hierarchy in `Docs/NEXT_STEP_GDD_V0.2_REQUEST.md`.

## Fanout queue

The queue is a checked-in plan with stable task IDs, dependencies, owned paths, forbidden paths, verification, and status. The orchestrator performs this loop:

1. Refresh repository and queue state.
2. Select `ready` tasks whose dependencies are complete and whose owned paths do not overlap active work.
3. Create one `agent/wave-<n>` integration branch, then one `agent/<task-id>` branch and `.worktrees/<task-id>` worktree per selected task from that wave base.
4. Dispatch up to the available worker count with the complete worktree contract.
5. Require focused tests, full relevant Unity tests, `git diff --check`, and a task commit.
6. Review the diff and merge each non-overlapping task branch into the wave integration branch. Re-run focused verification after each merge.
7. When the wave is green and main is unchanged from the wave base, fast-forward main to the wave integration branch. Re-run main verification, mark the tasks complete, remove clean worktrees/branches, and immediately select the next ready tasks.
8. Finish when no ready or active tasks remain. Report blocked tasks instead of inventing work.

Initial waves:

| Wave | Parallel tasks | Dependency |
|---|---|---|
| 1 | Combo rules; status rules; combat targets; run lifecycle | tuning baseline |
| 2 | motor/reachability; player combat; sniper/chunk lifetime | Wave 1 public contracts |
| 3 | input adapter; chunk spawner; HUD/result views | Waves 1–2 |
| 4 | shared Unity assets and playable scene | all code tasks |
| 5 | PlayMode acceptance and tuning fixes | playable scene |

## Long-session strategy (August 2026 baseline)

Use a durable goal for one coherent, verifiable outcome, not for an open-ended backlog. The current goal is the playable slice; the task queue is its checkpoint ledger. Each worker gets a fresh bounded task and rehydrates from the spec, queue entry, dependency commits, and tests. Git commits are checkpoints; test results are evidence; the repository is authoritative state. This follows the current Codex guidance for multi-hour work: give the goal a stopping condition, explicit files, proof commands, checkpoints, and a progress log.

An optional future replenisher runs only after the current goal or queue drains. It may inspect the repository and propose the next bounded goal or tasks, but cannot silently turn one goal into an endless Kanban backlog, make product decisions, or enqueue speculative features. A refill task needs a reproducible gap, an owner, a runnable acceptance check, and non-overlapping paths. Proposed tasks are reviewed before they become `ready`.

## Anti-patterns and guards

- Do not preserve one ever-growing worker conversation. Keep the coordinator goal stable, but start fresh per bounded task; summaries lose details and old context raises cost and drift.
- Do not use a loose list of unrelated backlog items as one durable goal. Finish or explicitly stop one verifiable objective before a replenisher proposes the next.
- Do not let agents edit the same scene, prefab, input asset, project setting, or package file concurrently.
- Do not use an unchecked `while true` agent prompt. Bound iterations, require a terminal state, and stop after repeated identical failures.
- Do not grant broad shell permission just to avoid pauses. Allow only task-relevant build/test/Git commands; destructive Git, process killing, push, and force deletion remain forbidden.
- Do not let the orchestrator silently rewrite requirements or mark its own output correct. A task must pass executable checks and a separate integration review.
- Do not treat activity as progress. No diff, repeated failure, cyclic dependency, stale base, or unchanged external state triggers reconciliation rather than another blind iteration.

## Completion criteria

- Unity compiles with zero C# errors and all EditMode/PlayMode tests pass.
- The playable scene supports automatic running, direction correction, jumping, breaking, parrying, combo speed, hit recovery, fall respawn, goal completion, result display, and attack-to-restart.
- Runtime input contains only `Move`, `Jump`, and `Attack`.
- `PlayerStatus` exposes no health count or health-change event.
- Every queue task is complete or explicitly blocked with evidence; no worktree owns uncommitted changes when the queue stops.
