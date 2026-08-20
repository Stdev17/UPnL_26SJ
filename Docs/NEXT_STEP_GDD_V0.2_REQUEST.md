# SIGNAL RUSH v0.2 구현 계약 요청서

Unity 프로젝트 기반 세팅 다음에는 gameplay 계약이 필요합니다. 개정 GDD를 보내거나 이 문서를 직접 채워 회신해 주세요. 설계·코드·테스트·커밋이 같은 대상을 가리킬 수 있도록 기존 ID(`OBJ-*`, `SYS-*`, `F*`, `TUNE-*`, `Q*`)는 유지합니다.

## 현재 프로젝트 현황

- `Assets/Game`과 `UPnL.SignalRush.Runtime` 단일 runtime assembly가 생성되어 있다.
- 코딩·애셋·Git 협업 규칙은 `Docs/Engineering.md`가 정본이다.
- Product Name은 `SIGNAL RUSH`, Company Name은 `UPnL`, Linear color space와 새 Input System은 유지되어 있다.
- Cinemachine 3.1.7로 갱신했고 `packages-lock.json`과 Unity 6000.5 EditMode compile/test를 확인했다.
- gameplay class, scene, prefab, test는 GDD v0.2 계약 전이라 의도적으로 생성하지 않았다.
- 기존 SampleScene과 범용 Input Actions는 보존했다. gameplay runtime action은 `Move`, `Jump`, `Attack` 세 개로 확정한다. 런 종료 뒤 `Attack`을 재시작 확인으로 재사용한다.

## 1. 현재 미결 항목 확정

| ID | 결정 | 구현 해석 |
|---|---|---|
| Q1 | 역할별 복수 청크 prefab | `GameplayFront`(플레이어가 밟음), `DecorFront`(앞쪽과 함께 이동, 판정 없음), `SniperRear`(독립 속도로 뒤에서 이동) 세 후보군을 두고 각 후보군에 AI 생성 prefab을 여러 개 등록한다. 총개수는 고정하지 않는다. |
| Q2 | 포함 | 원본은 32×32 sprite로 만들고 가로 절반을 투명 처리한다. 물리 collider는 불투명한 16×32 영역에만 둔다. |
| Q3 | 체력 감소 없이 안전 지점 복귀 | 낙사하면 일정 시간 조작을 잠그고 리스폰 연출 뒤 최근 안전 지점으로 복귀시켜 시간을 잃게 한다. |
| Q4 | `SignalRushTuning.PixelsPerUnit`로 기준값 주입 | 초기 제안값은 `32`이다. 이 값은 기준값이며 실제 sprite의 `SpriteImporter.spritePixelsPerUnit`도 같은 값이어야 한다. |
| Q5 | v0.2에서는 파편 sprite 미사용 | 파괴 위치를 일정 시간 검정색으로 덮는 임시 연출만 구현한다. 파편 구성은 아트 확정 때 다시 결정한다. |

## 2. 플레이어 상태 전이 확정

전이마다 한 행을 작성해 주세요. `Attack`이 `Run`/`Jump` 위에 함께 동작하는 overlay인지, 다른 동작을 막는 배타 상태인지도 명시합니다.

| From | Trigger | Guard | To/overlay | 허용 입력 | 종료 조건 |
|---|---|---|---|---|---|
| Run |  |  |  |  |  |
| Jump |  |  |  |  |  |
| Attack |  |  |  |  |  |
| Hit |  |  |  |  |  |
| Dead |  |  |  |  |  |

공격 판정 시간 중 들어온 추가 공격 입력을 무시할지, 다음 공격으로 buffering할지도 결정해 주세요.

| From | Trigger | Guard | To/overlay | 허용 입력 | 종료 조건 |
|---|---|---|---|---|---|
| Run | Jump 입력 | 접지 상태, Hit/Dead 아님 | Jump | Move, Attack | 착지 → Run; 피해 → Hit |
| Run | Attack 입력 | 공격 중 아님, Hit/Dead 아님 | Attack overlay on Run | Move, Jump, Attack | 공격 판정/애니메이션 종료 → Run |
| Run | 피해 요청 | 무적 아님 | Hit | 없음 | 경직 종료 → Run, 무적은 별도 타이머 종료 |
| Run/Jump | 낙사 요청 | Respawning/Dead 아님 | Respawning | 없음 | `RespawnLockSeconds` 뒤 안전 위치에서 Run |
| Jump | Attack 입력 | 공격 중 아님, Hit/Dead 아님 | Attack overlay on Jump | Move, Attack | 공격 판정/애니메이션 종료 → Jump |
| Jump | 착지 | 지면 감지 | Run | Move, Jump, Attack | 피해 → Hit |
| Jump | 피해 요청 | 무적 아님 | Hit | 없음 | 경직 종료 → 접지면 Run, 아니면 Jump |
| Attack | Attack 입력 | 공격 중이고 buffer가 비어 있음 | 다음 Attack 1회 buffer | Move/Jump는 기반 상태에 따름 | 현재 공격 종료 후 buffered Attack 시작; 없으면 기반 상태(Run/Jump) 복귀 |
| Attack | 피해 요청 | 무적 아님 | Hit | 없음 | 공격 buffer 폐기, 경직 종료 뒤 기반 상태 복귀 |
| Hit | 경직 종료 | 런 진행 중 | Run 또는 Jump | 없음 | 복귀 뒤 Move, Jump, Attack 허용 |
| Dead | Attack 입력 | 런 종료 뒤 재시작 가능 | Run | Attack만 | 플레이어·런 상태 초기화 완료 |

Attack은 배타 상태가 아니라 Run 또는 Jump 위에서 동작하는 overlay
Hit 종료 시 접지 상태면 Run, 공중이면 Jump로 복귀한다. Hit/Respawning/Dead 진입 시 공격 buffer는 폐기한다. 체력 수치는 없으며 `Dead`는 런 종료 상태 표현에만 사용한다.

## 3. 입력 binding 확정

GDD에서 다른 승인 액션을 추가하지 않는 한 runtime action은 `Move`, `Jump`, `Attack`만 둡니다.

| Action | 기본 키 | 보조 키 | press/hold 동작 |
|---|---|---|---|
| Move |  |  |  |
| Jump |  |  |  |
| Attack |  |  |  |
런 종료 화면에서는 `Attack` press를 재시작 확인으로 해석합니다. 키는 나중에 정하도록 하겠습니다.

## 4. 타입 책임과 공개 계약 확정

아래는 검토용 공개 계약 제안이다. 모든 타입과 멤버의 상태는 **승인**이며, 승인 전에는 gameplay code를 만들지 않는다. Unity message와 private 구현 메서드는 계약에서 제외한다. 의존 타입도 Unity/.NET 기본 타입에 닿을 때까지 함께 정의했다.

```csharp
// 상태: 제안·미구현
public enum RunPhase { Running, Respawning, Finished }
public enum RunResult { GoalReached, Dead }
public enum DamageCause { Projectile, OutOfScreen }
public enum ChunkRole { GameplayFront, DecorFront, SniperRear }

public readonly struct ChunkSlot
{
    public ChunkRole Role { get; }
    public Vector2 Position { get; }
}

public sealed class SignalRushTuning : ScriptableObject
{
    public int PixelsPerUnit { get; }
    public float BaseRunSpeed { get; }            // TUNE-P2
    public float MaxRunSpeed { get; }             // TUNE-P3
    public float RespawnLockSeconds { get; }      // TUNE-P6
    public float ProjectileSpeed { get; }         // TUNE-S2
    public int SpawnAheadChunkCount { get; }      // TUNE-G1
    public float MaxChunkHeightDelta { get; }     // TUNE-G2
    public float MaxChunkGap { get; }             // TUNE-G4
    public float SniperWarningSeconds { get; }    // TUNE-T1
}

public sealed class RunController : MonoBehaviour
{
    public RunPhase Phase { get; }
    public event Action<RunPhase> PhaseChanged;
    public event Action<RunResult> RunFinished;
    public void ReportGoalReached();
    public void ReportPlayerDead();
    public void Restart();
}

public sealed class PlayerMotor2D : MonoBehaviour
{
    public bool IsGrounded { get; }
    public Vector2 Position { get; }
    public Vector2 SafePosition { get; }
    public event Action<bool> GroundedChanged;
    public void SetMoveInput(float horizontal);
    public void RequestJump();
    public void SetSpeedMultiplier(float multiplier);
    public void LockControl(float seconds);
    public void Respawn(Vector2 position);
}

public sealed class PlayerCombat : MonoBehaviour
{
    public bool IsAttacking { get; }
    public event Action<BreakableObstacle> ObstacleBroken;
    public event Action<Projectile> ProjectileParried;
    public void RequestAttack();
}

public enum PlayerState { Active, Hit, Respawning, Dead }

public sealed class PlayerStatus : MonoBehaviour
{
    public PlayerState State { get; }
    public bool IsInvulnerable { get; }
    public bool IsControlLocked { get; }
    public event Action<PlayerState> StateChanged;
    public event Action<DamageCause> Hit;
    public void RequestDamage(DamageCause cause);
    public void RequestRespawn();
    public void MarkDead();
}

public sealed class ComboCounter : MonoBehaviour
{
    public int Current { get; }
    public int Best { get; }
    public int Interrupted { get; }
    public float SpeedMultiplier { get; }
    public event Action<int, int, int, float> Changed;
    public void RecordBreak();
    public void RecordParry();
    public void RecordHit();
}

public sealed class BreakableObstacle : MonoBehaviour
{
    public bool IsBroken { get; }
    public event Action<BreakableObstacle> Broken;
    public bool TryBreak();
}

public sealed class Sniper : MonoBehaviour
{
    public bool IsTargetting { get; }
    public event Action<Projectile> ProjectileSpawned;
    public bool TryActivate();
}

public sealed class Projectile : MonoBehaviour
{
    public bool IsResolved { get; }
    public event Action<Projectile> HitPlayer;
    public event Action<Projectile> Parried;
    public bool TryParry();
}

public sealed class Chunk : MonoBehaviour
{
    public ChunkRole Role { get; }
    public bool CanDespawn { get; }
    public void Place(ChunkSlot slot);
}

public sealed class ChunkSpawner : MonoBehaviour
{
    public void Begin();
    public void Stop();
}

public sealed class GoalTrigger : MonoBehaviour
{
    public event Action Reached;
}

public sealed class RunHud : MonoBehaviour
{
    // 공개 멤버 없음: serialize된 상태 소스를 구독해 표시만 한다.
}

public sealed class ResultView : MonoBehaviour
{
    public void Show(RunResult result);
}
```

계약 동작:

- `RunController.ReportGoalReached()`와 `ReportPlayerDead()`는 `Running`에서만 접수한다. 같은 physics step에 둘 다 접수되면 `GoalReached` 하나만 발행한다. 종료 뒤 호출은 무시한다.
- `PlayerMotor2D`의 입력 요청은 조작 잠금 또는 런 종료 중 무시한다. `Respawn()`은 velocity를 초기화하고 지정 위치로 옮긴다.
- `PlayerCombat.RequestAttack()` 한 번은 같은 공격 판정에 겹친 모든 유효 장애물과 총알을 각각 한 번 처리한다. 공격 중 재입력 규칙은 2번 단락에서 확정한다.
- `PlayerStatus.RequestDamage()`는 무적, 리스폰 또는 사망 상태에서 무시한다. `CurrentHealth`와 `HealthChanged`는 존재하지 않는다.
- `ComboCounter`는 파괴와 패링마다 증가하고 피격 때 초기화한다. 같은 대상의 중복 event는 대상 쪽에서 차단한다.
- `BreakableObstacle.TryBreak()`와 `Projectile.TryParry()`는 최초 성공만 `true`와 event를 반환하고 이후에는 `false`를 반환한다.
- `Sniper.TryActivate()`는 이미 예고/투사체 처리 중이면 `false`를 반환한다. 목표 위치는 발사 직전까지 계속 플레이어를 정확히 조준하며, 추후 시각 요소 구현에 상태 값이 필요하다.
- `Chunk.CanDespawn`은 소유 스나이퍼가 예고 중이거나 투사체가 미해결이면 `false`이다.
- `ChunkSpawner`는 `SignalRushTuning`과 역할별 serialize된 prefab 후보를 입력으로 사용한다. 빈 필수 후보군은 시작 시 명시적 오류로 중단한다.
- `GoalTrigger.Reached`는 한 런에 한 번만 발행한다. `RunHud`와 `ResultView`는 gameplay 상태를 소유하거나 변경하지 않는다.

## 5. Prefab 구성 확정

`OBJ-PLAYER`, `OBJ-SNIPER`, `OBJ-OBSTACLE`, `OBJ-CHUNK`, `OBJ-GOAL`의 GameObject hierarchy와 component 소유자를 작성해 주세요. 각 물리 오브젝트의 Rigidbody2D body type, collider type, trigger 여부, layer를 명시하고 visual child와 hitbox의 소유 오브젝트를 확정합니다.

모든 제안의 상태는 **승인**, prefab 소유자는 `윤슬`이다. 테스트 시 터지면 에러 로그를 보면 되므로 일단 속히 구현한다.

검토할 실제 Unity 옵션:

- `Rigidbody2D.bodyType`: `Dynamic` / `Kinematic` / `Static`.
- 2D collider: `BoxCollider2D` / `CapsuleCollider2D` / `CircleCollider2D` / `PolygonCollider2D` / `EdgeCollider2D` / `CompositeCollider2D`.
- trigger: `On` / `Off`.
- layer: built-in `Default` 또는 프로젝트에 추가할 `Player` / `PlayerAttack` / `Obstacle` / `Projectile` / `World` / `Goal`. 제안 layer는 아직 생성되지 않았다.

제안 hierarchy와 선택값:

```text
OBJ-PLAYER / PF_Player (owner: 윤슬, layer: Player)
├─ Root: PlayerMotor2D, PlayerCombat, PlayerStatus, ComboCounter
│  ├─ Rigidbody2D: Dynamic
│  └─ CapsuleCollider2D: trigger Off, 몸통/지면 판정 소유
├─ Visual (layer: Player): SpriteRenderer, Animator
└─ AttackHitbox (layer: PlayerAttack)
   └─ BoxCollider2D: trigger On, PlayerCombat 소유

OBJ-SNIPER / PF_Sniper (owner: 윤슬, layer: Default)
├─ Root: Sniper (Rigidbody2D와 collider 없음, Chunk에 부착되어 이동)
├─ Visual (layer: Default): SpriteRenderer, Animator
└─ Muzzle: 발사 Transform

OBJ-OBSTACLE / PF_Obstacle_* (owner: 윤슬, layer: Obstacle)
├─ Root: BreakableObstacle
│  ├─ Rigidbody2D: Kinematic
│  └─ BoxCollider2D: trigger Off, 불투명한 16×32 영역의 몸통/공격 충돌 소유
└─ Visual (layer: Obstacle): SpriteRenderer, 32×32 sprite

OBJ-CHUNK / PF_Chunk_* (owner: 윤슬, layer: World)
├─ Root: Chunk
│  ├─ Rigidbody2D: Kinematic
│  └─ BoxCollider2D: trigger Off, 발판 판정 소유
├─ Visual (layer: World): SpriteRenderer
├─ ObstacleSlots: OBJ-OBSTACLE 배치 anchor들
└─ SniperSlot: OBJ-SNIPER 배치 anchor (SniperRear 역할에서만 사용)

OBJ-GOAL / PF_Goal (owner: 윤슬, layer: Goal)
├─ Root: GoalTrigger
│  ├─ Rigidbody2D: Kinematic
│  └─ BoxCollider2D: trigger On, 목표 도달 판정 소유
└─ Visual (layer: Goal): SpriteRenderer, Animator
```

`DecorFront` 청크는 collider와 위험 요소 없이 visual만 가진다. `SniperRear` 청크는 독립 속도로 이동하며 활성 스나이퍼/투사체가 있는 동안 제거하지 않는다.

## 6. 구현 필수 튜닝 값 입력

아래 값은 **제안·미구현 초기값**이다. 미지의 ID는 앞으로 처음 등장하는 위치에 의미와 단위를 함께 적는다.

| ID | 의미 | 단위 | 초기값 |
|---|---|---|---:|
| `TUNE-P2` | 콤보가 없을 때 기본 달리기 속도 | world unit/s | `6.0` |
| `TUNE-P3` | 콤보로 증가할 수 있는 최대 달리기 속도 | world unit/s | `10.0` |
| `TUNE-P6` | 낙사 후 조작 잠금과 리스폰 연출 시간 | s | `1.0` |
| `TUNE-S2` | 스나이퍼 투사체 이동 속도 | world unit/s | `18.0` |
| `TUNE-G1` | 플레이어 앞에 유지할 gameplay 청크 수 | chunk | `2` |
| `TUNE-G2` | 인접 gameplay 청크의 최대 높이차 | world unit | `1.5` |
| `TUNE-G4` | 인접 gameplay 청크 사이의 최대 수평 간격 | world unit | `2.0` |
| `TUNE-T1` | 스나이퍼 예고 시작부터 발사까지의 고정 딜레이 | s | `0.8` |

**중요**: 플레이어 최대 체력은 존재하지 않으며 체력 기능을 구현하지 않는다. 장애물이나 투사체에 맞으면 `PlayerStatus`가 Hit/무적을 처리하고 콤보가 초기화된다. 낙사는 Respawning으로 전환해 시간을 잃게 한다.

검증 규칙:

- `0 < TUNE-P2 < TUNE-P3`이고 `TUNE-P6`, `TUNE-S2`, `TUNE-G1`은 양수여야 한다.
- 물리 설정으로 계산한 최대 점프 높이를 `H`, 안전 마진을 `0.25 world unit`이라 할 때 실제 높이차는 `min(TUNE-G2, H - 0.25)`로 제한한다.
- `TUNE-G4`는 기본 속도 `TUNE-P2`에서의 실제 점프 궤적으로 건널 수 있는 값 이하여야 한다. 값 검증은 이동 구현의 순수 계산과 같은 식을 사용한다.
- 예고 뒤 플레이어에게 보이는 총알 비행 구간은 약 `0.2s`만 제공한다. 반응형 회피가 아니라 리듬 예측형 공격이므로 예고 시작과 발사 사이에는 항상 `TUNE-T1`의 고정 딜레이를 둔다.
- `PixelsPerUnit`은 양의 정수여야 하며 모든 gameplay sprite importer의 PPU와 일치해야 한다.

## 7. 경계 acceptance example 추가

기존 60초 vertical slice 시나리오는 유지하고, 아래 모호성을 해소하는 결과만 추가해 주세요.

1. 장애물과 총알이 동시에 유효할 때 공격 입력 결과
   - 같은 공격 판정 안의 장애물 파괴와 총알 패링이 모두 성공한다.
2. 무적 중 피해 요청 결과
   - 상태·콤보·무적 시간을 변경하지 않는다.
3. Q3에 따른 낙사와 안전 지점 복귀 결과
   - `TUNE-P6` 동안 조작을 잠근 뒤 최근 안전 지점으로 복귀한다.
4. 스나이퍼 예고 중이거나 총알이 활성인 청크의 정리 시도 결과
   - 스나이퍼는 `SniperRear` 청크에만 등장한다. 예고 중이거나 총알이 미해결인 청크는 정리하지 않고, 총알이 적중·패링·화면 이탈로 해결된 뒤 정리할 수 있다.
5. 같은 physics step에 목표 도달과 사망이 함께 발생한 경우의 우선순위
   - 목표 도달을 우선해 `GoalReached`로 한 번만 종료하고 사망 요청은 무시한다.

## 다음 턴 검토 준비

- 4번의 공개 타입·멤버에서 승인, 이름 변경, 병합 또는 삭제할 항목을 표시한다.
- 5번의 Rigidbody2D, collider, trigger, layer 제안 중 바꿀 선택값을 표시한다.
- 6번 초기값을 플레이 감각 기준으로 승인하거나 원하는 값으로 교체한다.
- Q1의 역할별 최소 prefab 개수와 Q4의 초기 PPU `32`를 확정한다.
  - **결정**: 초기값을 prefab 2개로 시작한다. 초기 PPU도 `32`로 시작한다.
- 다른 팀원이 작성 중인 2·3번 단락의 완성본을 준비한다.

예상 검토 시간: **10~15분**.

이 계약이 도착하면 튜닝 validation과 순수 콤보 테스트부터 시작하고, 이동 → 전투/상태 → 청크 생성 → 런/UI 순서로 구현합니다.
