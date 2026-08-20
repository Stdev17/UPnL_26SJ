# SIGNAL RUSH Tuning Validation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Create the approved read-only tuning asset and enforce the scalar invariants that do not depend on unfinished movement or input design.

**Architecture:** `SignalRushTuning` remains one `ScriptableObject` under the runtime Tuning feature. Unity `OnValidate` normalizes invalid Inspector values at the data owner; one EditMode test verifies both contract defaults and normalization. `TUNE-P1` is omitted because the latest GDD instruction removes health, while jump reachability validation waits for the movement formula it must share.

**Tech Stack:** Unity 6000.5.9f1, C#, Unity Test Framework 1.7.0, NUnit EditMode tests.

**Spec:** `Docs/NEXT_STEP_GDD_V0.2_REQUEST.md`

## Global Constraints

- Runtime code stays in the existing `UPnL.SignalRush.Runtime` assembly and uses namespace `UPnL.SignalRush.Tuning`.
- Inspector data uses `[SerializeField] private`; consumers receive read-only properties.
- `PixelsPerUnit = 32`, `BaseRunSpeed = 6`, `MaxRunSpeed = 10`, `RespawnLockSeconds = 1`, `ProjectileSpeed = 18`, `SpawnAheadChunkCount = 2`, `MaxChunkHeightDelta = 1.5`, `MaxChunkGap = 2`, and `SniperWarningSeconds = 0.8` by default.
- Validation preserves `0 < BaseRunSpeed < MaxRunSpeed`; all durations and speeds are positive; counts and PPU are at least one; distances are non-negative.
- Do not implement health, combo, input, movement, prefab, scene, or reachability logic in this slice.

---

### Prerequisite: Restore the Unity baseline

**Files:**
- Modify: `Packages/manifest.json`
- Unity-resolved: `Packages/packages-lock.json`

- [x] Upgrade Cinemachine from `3.1.5` to stable `3.1.7`, whose official changelog includes the InstanceID-to-EntityID conversion required by Unity 6000.5.
- [x] Run the complete EditMode suite and require a clean compile before adding gameplay tests.

---

### Task 1: Read-only tuning asset and scalar validation

**Files:**
- Create: `Assets/Game/Tests/EditMode/UPnL.SignalRush.EditModeTests.asmdef`
- Create: `Assets/Game/Tests/EditMode/Tuning/SignalRushTuningTests.cs`
- Create: `Assets/Game/Scripts/Runtime/Tuning/SignalRushTuning.cs`

**Interfaces:**
- Consumes: Unity `ScriptableObject`, `Mathf`, and serialized Inspector values.
- Produces: read-only `PixelsPerUnit`, `BaseRunSpeed`, `MaxRunSpeed`, `RespawnLockSeconds`, `ProjectileSpeed`, `SpawnAheadChunkCount`, `MaxChunkHeightDelta`, `MaxChunkGap`, and `SniperWarningSeconds` properties.

- [x] **Step 1: Add the EditMode test assembly and failing tests**

```json
{
  "name": "UPnL.SignalRush.EditModeTests",
  "rootNamespace": "UPnL.SignalRush.Tests",
  "references": ["UPnL.SignalRush.Runtime"],
  "optionalUnityReferences": ["TestAssemblies"],
  "includePlatforms": ["Editor"]
}
```

```csharp
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UPnL.SignalRush.Tuning;

namespace UPnL.SignalRush.Tests.Tuning
{
    public sealed class SignalRushTuningTests
    {
        [Test]
        public void DefaultsMatchApprovedContract()
        {
            var tuning = ScriptableObject.CreateInstance<SignalRushTuning>();
            Assert.That(tuning.PixelsPerUnit, Is.EqualTo(32));
            Assert.That(tuning.BaseRunSpeed, Is.EqualTo(6f));
            Assert.That(tuning.MaxRunSpeed, Is.EqualTo(10f));
            Assert.That(tuning.SpawnAheadChunkCount, Is.EqualTo(2));
            Object.DestroyImmediate(tuning);
        }

        [Test]
        public void OnValidateNormalizesInvalidScalarValues()
        {
            var tuning = ScriptableObject.CreateInstance<SignalRushTuning>();
            var serialized = new SerializedObject(tuning);
            serialized.FindProperty("_pixelsPerUnit").intValue = 0;
            serialized.FindProperty("_baseRunSpeed").floatValue = 0f;
            serialized.FindProperty("_maxRunSpeed").floatValue = 0f;
            serialized.FindProperty("_spawnAheadChunkCount").intValue = 0;
            serialized.FindProperty("_maxChunkHeightDelta").floatValue = -1f;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            Assert.That(tuning.PixelsPerUnit, Is.EqualTo(1));
            Assert.That(tuning.BaseRunSpeed, Is.GreaterThan(0f));
            Assert.That(tuning.MaxRunSpeed, Is.GreaterThan(tuning.BaseRunSpeed));
            Assert.That(tuning.SpawnAheadChunkCount, Is.EqualTo(1));
            Assert.That(tuning.MaxChunkHeightDelta, Is.Zero);
            Object.DestroyImmediate(tuning);
        }
    }
}
```

- [x] **Step 2: Run EditMode tests and verify RED**

Run:

```bash
/Applications/Unity/Hub/Editor/6000.5.9f1/Unity.app/Contents/MacOS/Unity -batchmode -projectPath . -runTests -testPlatform EditMode -testFilter UPnL.SignalRush.Tests.Tuning.SignalRushTuningTests -testResults /tmp/signal-rush-red.xml
```

Expected: compilation fails because `UPnL.SignalRush.Tuning.SignalRushTuning` does not exist.

- [x] **Step 3: Add the minimal `SignalRushTuning` implementation**

Create a sealed `ScriptableObject` with the nine serialized defaults and read-only properties above. Its private `OnValidate` uses `Mathf.Max`: PPU/count clamp to `1`, positive floats clamp to `0.01f`, distances clamp to `0f`, and maximum speed clamps to at least `BaseRunSpeed + 0.01f`.

- [x] **Step 4: Run focused EditMode tests and verify GREEN**

Run the Step 2 command with result path `/tmp/signal-rush-green.xml`.

Expected: two tests pass with zero failures.

- [x] **Step 5: Run the complete EditMode suite**

Run the same Unity command without `-testFilter` and write `/tmp/signal-rush-editmode.xml`.

Expected: all EditMode tests pass with zero failures.
