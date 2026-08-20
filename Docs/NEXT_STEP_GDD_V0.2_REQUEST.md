# SIGNAL RUSH v0.2 구현 계약 요청서

Unity 프로젝트 기반 세팅 다음에는 gameplay 계약이 필요합니다. 개정 GDD를 보내거나 이 문서를 직접 채워 회신해 주세요. 설계·코드·테스트·커밋이 같은 대상을 가리킬 수 있도록 기존 ID(`OBJ-*`, `SYS-*`, `F*`, `TUNE-*`, `Q*`)는 유지합니다.

## 현재 프로젝트 현황

- `Assets/Game`과 `UPnL.SignalRush.Runtime` 단일 runtime assembly가 생성되어 있다.
- 코딩·애셋·Git 협업 규칙은 `Docs/Engineering.md`가 정본이다.
- Product Name은 `SIGNAL RUSH`, Company Name은 `UPnL`, Linear color space와 새 Input System은 유지되어 있다.
- Cinemachine 3.1.5가 manifest에 추가됐다. 로컬 Unity Licensing Client의 프로토콜 불일치로 batchmode가 끝나지 않아 `packages-lock.json` 갱신과 최종 compile 확인은 Editor를 정상 실행한 뒤 남아 있다.
- gameplay class, scene, prefab, test는 GDD v0.2 계약 전이라 의도적으로 생성하지 않았다.
- 기존 SampleScene과 범용 Input Actions는 보존했다. v0.2 계약 수신 후 게임용 scene과 `Move`, `Jump`, `Attack`, `Restart` binding을 확정한다.

## 1. 현재 미결 항목 확정

| ID | 필요한 결정 | 응답 형식 |
|---|---|---|
| Q1 | 청크 prefab 수 | 3–5 중 정수 하나와 각 청크의 gameplay 역할 |
| Q2 | 16×32 간판 장애물 포함 여부 | `포함` 또는 `제외` |
| Q3 | 낙사 결과 | `체력 1 감소 후 복귀` 또는 `즉사` |
| Q4 | Pixels Per Unit | 모든 gameplay sprite에 적용할 양의 정수 하나 |
| Q5 | 파편 sprite 구성 | `공용 1세트` 또는 `장애물별` |

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
| Run | Jump 입력 | 접지 상태, Hit/Dead 아님 | Jump | Move, Attack | 착지 → Run; 피해 → Hit/Dead |
| Run | Attack 입력 | 공격 중 아님, Hit/Dead 아님 | Attack overlay on Run | Move, Jump, Attack | 공격 판정/애니메이션 종료 → Run |
| Run | 피해 요청 | 무적 아님, 체력 > 1 | Hit | 없음 | 경직/무적 시간 종료 → Run |
| Run | 치명 피해 또는 즉사 낙사 | 체력 ≤ 0 또는 Q3이 즉사 | Dead | Restart만 | Restart → Run |
| Jump | Attack 입력 | 공격 중 아님, Hit/Dead 아님 | Attack overlay on Jump | Move, Attack | 공격 판정/애니메이션 종료 → Jump |
| Jump | 착지 | 지면 감지 | Run | Move, Jump, Attack | 피해 → Hit/Dead |
| Jump | 피해 요청 | 무적 아님, 체력 > 1 | Hit | 없음 | 경직/무적 시간 종료 → Run |
| Jump | 치명 피해 또는 즉사 낙사 | 체력 ≤ 0 또는 Q3이 즉사 | Dead | Restart만 | Restart → Run |
| Attack | Attack 입력 | 공격 중이고 buffer가 비어 있음 | 다음 Attack 1회 buffer | Move/Jump는 기반 상태에 따름 | 현재 공격 종료 후 buffered Attack 시작; 없으면 기반 상태(Run/Jump) 복귀 |
| Attack | 피해 요청 | 무적 아님, 체력 > 1 | Hit | 없음 | 경직/무적 시간 종료 → Run |
| Attack | 치명 피해 또는 즉사 낙사 | 체력 ≤ 0 또는 Q3이 즉사 | Dead | Restart만 | Restart → Run |
| Hit | 경직 종료 | 생존 | Run | 없음 | Run 진입 후 Move, Jump, Attack 허용 |
| Hit | 치명 피해 | 체력 ≤ 0 | Dead | Restart만 | Restart → Run |
| Dead | Restart 입력 | 재시작 가능 | Run | Restart만 | 플레이어·런 상태 초기화 완료 |

Attack은 배타 상태가 아니라 Run 또는 Jump 위에서 동작하는 overlay
Hit 종료 시 접지 상태면 Run, 공중이면 Jump로 복귀한다. Hit/Dead 진입 시 공격 buffer는 폐기한다.

## 3. 입력 binding 확정

GDD에서 다른 승인 액션을 추가하지 않는 한 runtime action은 `Move`, `Jump`, `Attack`, `Restart`만 둡니다.

| Action | 기본 키 | 보조 키 | press/hold 동작 |
|---|---|---|---|
| Move |  |  |  |
| Jump |  |  |  |
| Attack |  |  |  |
| Restart |  |  |  |
키는 나중에 정하도록 하겠습니다. 

## 4. 타입 책임과 공개 계약 확정

각 책임을 승인·이름 변경·병합·분리해 주세요. signature는 컴포넌트 사이 통신에 필요한 것만 적고 private 구현 메서드는 GDD에 넣지 않습니다.

| 제안 타입 | 소유 책임 | 필수 입력 | 외부 출력/event | 결정 |
|---|---|---|---|---|
| `SignalRushTuning` | 모든 TUNE 값과 값 사이 제약 | 확정 튜닝 값 | read-only 값 |  |
| `RunController` | 런 상태와 통계 | 목표 도달, 사망, 재시작 | 런 상태/결과 변경 |  |
| `PlayerMotor2D` | F1 이동과 F2 점프 | 이동/점프 입력, 콤보 속도 배율 | 접지/위치 상태 |  |
| `PlayerCombat` | F3 공격과 F4 패링 | 공격 입력, 장애물/총알 접촉 | 파괴/패링 결과 |  |
| `PlayerHealth` | 체력, 무적, 피격, 사망 | 피해/낙사 요청 | 체력, 피격, 사망 |  |
| `ComboCounter` | 현재/최고 콤보와 속도 배율 | 파괴/패링/피격 | 콤보 변경 |  |
| `BreakableObstacle` | 몸통 충돌과 공격 충돌의 차이 | 플레이어/공격 충돌 | 파괴 또는 피해 요청 |  |
| `Sniper` | 예고와 발사 phase | 활성 허가, 목표 snapshot | 총알 생성/대기 |  |
| `Projectile` | 비행, 충돌, 패링 | 생성 궤적, 공격 입력 | 피격 또는 패링 |  |
| `Chunk` | anchor, slot, 활성 위험 요소 | slot 선택 | 제거 가능 여부 |  |
| `ChunkSpawner` | 도달 가능한 배치와 정리 | 튜닝, 청크 후보 | 청크 생성/제거 |  |
| `GoalTrigger` | 목표 도달 1회 전달 | 플레이어 overlap | 목표 도달 |  |
| `RunHud` | 런 중 표시 | 체력/콤보/진행도 event | 시각 표현만 |  |
| `ResultView` | 결과 표시와 재시작 요청 | 런 결과, 재시작 입력 | 재시작 요청 |  |

승인한 컴포넌트 간 메서드 또는 event마다 아래 양식을 작성해 주세요.

```text
소유 타입:
멤버 이름:
종류: method | property | event
parameter와 타입:
return 타입:
caller/subscriber:
유효한 호출/발행 시점:
실패 또는 무시되는 호출의 동작:
```

## 5. Prefab 구성 확정

`OBJ-PLAYER`, `OBJ-SNIPER`, `OBJ-OBSTACLE`, `OBJ-CHUNK`, `OBJ-GOAL`의 GameObject hierarchy와 component 소유자를 작성해 주세요. 각 물리 오브젝트의 Rigidbody2D body type, collider type, trigger 여부, layer를 명시하고 visual child와 hitbox의 소유 오브젝트를 확정합니다.

## 6. 구현 필수 튜닝 값 입력

현재 미정인 `TUNE-P1`, `TUNE-P2`, `TUNE-P3`, `TUNE-P6`, `TUNE-S2`, `TUNE-G1`, `TUNE-G2`, `TUNE-G4`의 초기값을 주세요. 다음 관계는 유지해야 합니다.

- `TUNE-P2 < TUNE-P3`.
- 청크 높이차는 최대 점프 높이에서 명시적 안전 마진을 뺀 값 이하로 clamp한다.
- 청크 간격은 기본 속도에서 도달 가능하다.
- 예고 후 총알 비행은 최소 0.5초의 반응 시간을 제공한다.

## 7. 경계 acceptance example 추가

기존 60초 vertical slice 시나리오는 유지하고, 아래 모호성을 해소하는 결과만 추가해 주세요.

1. 장애물과 총알이 동시에 유효할 때 공격 입력 결과
2. 무적 중 피해 요청 결과
3. Q3에 따른 낙사와 안전 지점 복귀 결과
4. 스나이퍼 예고 중이거나 총알이 활성인 청크의 정리 시도 결과
5. 같은 physics step에 목표 도달과 사망이 함께 발생한 경우의 우선순위

이 계약이 도착하면 튜닝 validation과 순수 콤보 테스트부터 시작하고, 이동 → 전투/체력 → 청크 생성 → 런/UI 순서로 구현합니다.
