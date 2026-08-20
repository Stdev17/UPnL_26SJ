# Codex 다중 에이전트 Worktree 플레이북

## 결론

이 리포는 **조건부로 안전하다**. Git worktree를 사용할 수 있고 Unity의 `Library`·`Temp`·`Logs`는 이미 무시되어 각 worktree가 자체 임포트 캐시를 갖는다. 또한 scene, prefab, asset, meta 파일은 자동 병합하지 않도록 설정되어 있다.

Codex 자동 작업은 리포 내부의 ignore된 `.worktrees/`만 사용한다. 이 위치는 프로젝트의 허용된 쓰기 루트 안에 있어 파일 수정과 검증을 추가 권한 요청 없이 재현할 수 있다. 리포 밖의 형제 디렉터리는 현재 권한 모델에서 사용하지 않는다.

## Codex 자동 실행 계약

사용자는 이 리포에서 아래 조건을 만족하는 Codex 작업에 대해 `.worktrees/` 생성과 정리를 매번 다시 확인하지 않아도 된다고 승인했다.

1. 승인된 문서나 구현 계획에서 하나의 좁은 task scope를 정하고 `agent/<scope>` branch와 `.worktrees/<scope>`를 만든다. 병렬 웨이브라면 먼저 `agent/wave-<n>` 통합 branch를 만들고 모든 task branch를 같은 wave 기준점에서 분기한다.
2. worktree를 만들기 전에 `.worktrees/`가 Git ignore 대상인지, 같은 branch/path가 없는지, main의 기존 변경이 다른 작업 소유가 아닌지 확인한다.
3. 작업 파일과 검증 명령은 아래 작업 계약에 기록하고 그 범위만 수정한다.
4. worktree에서 필요한 테스트와 `git diff --check`가 모두 성공해야 커밋한다.
5. 단일 task는 기존처럼 main에 fast-forward한다. 병렬 task는 통합 담당자가 wave branch에 하나씩 병합·검증한 뒤, wave 분기 이후 main이 바뀌지 않았고 main worktree가 깨끗할 때만 `git merge --ff-only agent/wave-<n>`로 자동 반영한다.
6. main에서 같은 검증을 다시 통과한 뒤에만 worktree, task branch, wave branch를 일반 삭제한다.

다음 경우에는 자동 진행을 멈추고 사용자에게 보고한다.

- 단일 task 또는 wave 분기 이후 main이 변경되어 fast-forward할 수 없다.
- 다른 작업이 같은 `.unity`, `.prefab`, `.asset`, `.meta`, `.inputactions`, package 또는 `ProjectSettings` 파일을 소유한다.
- 테스트 실패, 아래 1회 복구 후에도 남는 Unity licensing 문제, merge 충돌 또는 보존되지 않은 변경이 있다.
- `pull`, `push`, 강제 삭제, rebase, reset처럼 이 계약에 포함되지 않은 Git 작업이 필요하다.

## 역할

- **통합 담당자 1명**: `main`에서 merge, package/ProjectSettings/Input Actions/공유 scene 변경을 소유한다.
- **작업 에이전트 1명당 worktree 1개**: 하나의 독립된 기능 또는 파일 소유권만 맡는다.
- **검토 에이전트**: worktree를 수정하지 않고 branch diff와 검증 결과만 본다.

같은 `.unity`, `.prefab`, `.asset`, `.meta`, `.inputactions`, `Packages/manifest.json`, `Packages/packages-lock.json`, `ProjectSettings/*`는 동시에 두 worktree에서 수정하지 않는다.

## 시작 전 확인

통합 담당자가 main에서 실행한다.

```bash
git switch main
git status --short
git worktree list
```

`git status --short` 출력이 비어 있지 않으면 새 worktree를 만들지 않는다. 먼저 변경의 소유자를 확인하고 커밋·stash·폐기 중 하나를 명시적으로 결정한다. 다른 에이전트의 변경을 reset 또는 checkout으로 지우지 않는다.

## 작업 worktree 만들기

브랜치명과 폴더명은 `agent/<짧은-범위>`와 `.worktrees/<짧은-범위>`를 사용한다. 예: `agent/combo-rules`.

```bash
git check-ignore -q .worktrees
mkdir -p .worktrees
git worktree add -b agent/combo-rules .worktrees/combo-rules main
git -C .worktrees/combo-rules status --short
```

새 worktree는 main에서 분기한 깨끗한 상태여야 한다. Unity Editor는 해당 worktree 경로를 별도 프로젝트로 열어야 하며, 다른 worktree의 `Library`를 복사하거나 공유하지 않는다.

## 에이전트 작업 계약

작업을 지시할 때 아래 다섯 항목을 함께 준다.

```text
Worktree: .worktrees/<scope>
Branch: agent/<scope>
목표: 한 문장
소유 파일/폴더: 정확한 경로
금지 파일/폴더: 공유 YAML, package 또는 다른 에이전트 소유 경로
검증: 실행할 Unity test, batchmode compile 또는 정적 검사
```

작업 중 규칙:

1. 자기 worktree에서만 파일을 수정한다.
2. `git status --short`로 시작과 종료 상태를 확인한다.
3. 새 public 계약, prefab hierarchy, tuning 값은 GDD ID로 기록한다.
4. 씬·prefab·입력 액션 충돌이 예상되면 수정하지 말고 통합 담당자에게 넘긴다.
5. Unity `.meta`는 삭제·재생성하지 않고, Unity Editor로 이동·이름 변경한다.

## Unity 작업 분배

병렬화하기 좋은 범위:

- 서로 다른 C# feature 폴더와 그 EditMode 테스트
- 서로 다른 신규 prefab과 독립 sprite/animation 파일
- 문서와 tuning 초안

직렬로 처리할 범위:

- `Packages/manifest.json`, `Packages/packages-lock.json`
- `ProjectSettings/*`, `Assets/Settings/*`
- 같은 scene, prefab, ScriptableObject, Input Actions
- 동일 GUID를 참조하는 asset 이동 또는 이름 변경

Unity YAML은 자동 병합을 하지 않는다. 충돌이 나면 통합 담당자가 최신 파일을 기준으로 더 작은 변경을 Editor에서 수동 재적용한다.

## 검증·커밋·통합

작업 에이전트는 자기 worktree에서 최소 검증을 마친다.

```bash
git -C .worktrees/combo-rules diff --check
git -C .worktrees/combo-rules status --short
```

Unity 코드 또는 asset 변경이면 해당 worktree를 대상으로 Unity Test Runner 또는 batchmode compile도 실행한다. 검증이 실패하면 커밋이나 통합 전에 원인을 기록하고 해결한다.

`-runTests`에는 `-quit`을 함께 주지 않는다. Test Runner가 완료 후 직접 종료하며, 종료 코드뿐 아니라 지정한 `-testResults` XML의 `result="Passed"`와 실패 0건을 확인한다.

Unity가 `Unity-LicenseClient-<user>-<editor-version>` 채널을 기다리다 실패하면 먼저 해당 Unity 실행이 끝났는지 확인한다. 실패한 실행이 남긴 같은 채널의 Editor 전용 Licensing Client만 정확한 PID로 한 번 종료하고 재시도한다. Hub의 `Unity-LicenseClient-<user>`는 종료하지 않으며, 재시도도 실패하면 자동 진행을 멈춘다.

작업 완료 후에는 작업 branch에만 필요한 파일을 명시적으로 커밋한다.

```bash
git -C .worktrees/combo-rules add Assets/Game/Scripts/Runtime/Combo
git -C .worktrees/combo-rules commit -m "feat: add combo rules"
```

단일 task는 통합 담당자가 main에서 통합하고 검증한다.

```bash
git switch main
git merge --ff-only agent/combo-rules
git diff --check
```

Unity 변경이면 merge 직후 main에서 Unity 검증을 다시 실행한다. 한 branch의 검증이 실패하면 다음 branch를 merge하지 않는다.

병렬 웨이브는 main을 그대로 둔 채 통합 branch에서 task를 하나씩 병합한다. 첫 task는 fast-forward할 수 있고, 이후 task는 충돌 없는 일반 merge로 합친다. 각 병합 직후 focused 검증을 통과해야 다음 task를 합친다.

```bash
git branch agent/wave-1 main
git switch agent/wave-1
git merge agent/combo-rules
git merge --no-ff agent/status-rules
git diff --check
```

웨이브 전체 검증이 성공하면 main만 wave branch로 fast-forward한다.

```bash
git switch main
git merge --ff-only agent/wave-1
```

## 정리와 복구

통합이 끝난 뒤에만 worktree와 branch를 제거한다.

```bash
git worktree remove .worktrees/combo-rules
git branch -d agent/combo-rules
git worktree prune
```

`git worktree remove --force`, `git reset --hard`, `git checkout --`는 사용하지 않는다. 삭제가 거부되면 해당 worktree의 `git status --short`를 확인하고 남은 변경을 먼저 보존한다.

## 운영 체크리스트

- main은 통합 담당자만 수정한다.
- 한 에이전트는 하나의 worktree와 하나의 좁은 소유 범위를 가진다.
- 공유 Unity YAML과 package 설정은 동시에 편집하지 않는다.
- 모든 merge 전에 `git diff --check`를 실행한다.
- Unity 검증은 변경이 발생한 worktree와 merge된 main에서 각각 실행한다.
- Codex worktree는 ignore된 `.worktrees/` 아래에만 생성한다.
- 자동 통합은 단일 task 또는 검증된 wave branch가 main에 fast-forward 가능한 경우에만 수행한다.
