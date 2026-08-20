# Unity Engineering Guide

## Ownership

- Game-owned files live under `Assets/Game`.
- Third-party code lives under `Assets/Plugins` and is not edited locally.
- URP, Input System, and other technical assets remain under `Assets/Settings`.
- `Assets/Resources` is reserved for assets required there by installed plugins.
- Organize runtime code by GDD feature. Promote code to `Core` only after two features share it.
- Do not add `Managers`, `Services`, `Shared`, or `Utils` catch-all folders.

## C# conventions

- Namespace: `UPnL.SignalRush.<Feature>`.
- Types, methods, and public properties use `PascalCase`; private fields use `_camelCase`; locals and parameters use `camelCase`; interfaces use `IPascalCase`.
- Keep one type per file. Put Unity messages near the top in this order: `Awake`, `OnEnable`, `Start`, `Update`, `FixedUpdate`, `OnDisable`, `OnDestroy`.
- Expose Inspector values with `[SerializeField] private`; do not add public fields.
- Cache component references. Do not use `GetComponent`, `Find*`, LINQ, or string-based lookup in `Update` or `FixedUpdate`.
- Keep one source of truth for each state. Views observe state; they do not copy or own it.
- Prefer serialized references and narrow C# events. Do not introduce a service locator, global event bus, DI container, or persistent singleton without a demonstrated need.
- Use UniTask only when real asynchronous work needs cancellation.

## Asset conventions

- Use these prefixes: scene `SCN_`, prefab `PF_`, ScriptableObject `SO_`, sprite `SPR_`, animation clip `AN_`, animator controller `AC_`, material `MAT_`.
- Use ASCII names and `_`. Replace `Final`, `New`, and `Copy` suffixes with names that describe the asset's role.
- Pixel sprites use Point filtering, no compression, and preserved alpha. Lock one Pixels Per Unit value after GDD Q4 is decided.
- Keep raw AI output outside runtime asset folders. Commit only approved, corrected assets under `Assets/Game/Art` with their `.meta` files.
- Move and rename Unity assets in the Editor so GUIDs are preserved.

## Collaboration

- Pull before starting and commit small feature- or asset-sized changes.
- Announce scene and shared-prefab ownership before editing. Do not edit the same `.unity`, `.prefab`, or `.inputactions` file concurrently.
- Scene and prefab files deliberately do not auto-merge. Resolve conflicts by reapplying the smaller change onto the newer file.
- Never delete or regenerate a tracked `.meta` file.

## SIGNAL RUSH implementation gate

GDD v0.1 deliberately defers concrete types and function signatures. Do not create gameplay stubs until v0.2 resolves that contract. Then implement in this order:

1. `SignalRushTuning` constraints
2. `ComboCounter` formulas
3. `PlayerMotor2D` jump and movement
4. Combat and player status interaction
5. Chunk reachability and lifetime rules
6. Run lifecycle and UI wiring

Write the smallest failing EditMode or PlayMode test for each non-trivial invariant before its production code.
