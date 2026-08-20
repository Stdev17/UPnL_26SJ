# Codex 다중 에이전트 Worktree 플레이북

## 결론

이 리포는 **조건부로 안전하다**. Git worktree를 사용할 수 있고 Unity의 `Library`·`Temp`·`Logs`는 이미 무시되어 각 worktree가 자체 임포트 캐시를 갖는다. 또한 scene, prefab, asset, meta 파일은 자동 병합하지 않도록 설정되어 있다.

다만 현재 프로젝트 내부 `.worktrees/`는 Git ignore 대상이 아니다. worktree는 반드시 리포 밖의 형제 디렉터리 `../UPnL_26SJ-worktrees/`에 만들거나, 그 전에 `.worktrees/`를 ignore 규칙에 추가한다.

## 역할

- **통합 담당자 1명**: `main`에서 merge, package/ProjectSettings/Input Actions/공유 scene 변경을 소유한다.
- **작업 에이전트 1명당 worktree 1개**: 하나의 독립된 기능 또는 파일 소유권만 맡는다.
- **검토 에이전트**: worktree를 수정하지 않고 branch diff와 검증 결과만 본다.

같은 `.unity`, `.prefab`, `.asset`, `.meta`, `.inputactions`, `Packages/manifest.json`, `Packages/packages-lock.json`, `ProjectSettings/*`는 동시에 두 worktree에서 수정하지 않는다.

## 시작 전 확인

통합 담당자가 main에서 실행한다.

```bash
git switch main
git pull --ff-only
git status --short
git worktree list
```

`git status --short` 출력이 비어 있지 않으면 새 worktree를 만들지 않는다. 먼저 변경의 소유자를 확인하고 커밋·stash·폐기 중 하나를 명시적으로 결정한다. 다른 에이전트의 변경을 reset 또는 checkout으로 지우지 않는다.

## 작업 worktree 만들기

브랜치명과 폴더명은 `agent/<짧은-범위>`와 `<짧은-범위>`를 사용한다. 예: `agent/combo-rules`.

```bash
mkdir -p ../UPnL_26SJ-worktrees
git worktree add -b agent/combo-rules ../UPnL_26SJ-worktrees/combo-rules main
git -C ../UPnL_26SJ-worktrees/combo-rules status --short
```

새 worktree는 main에서 분기한 깨끗한 상태여야 한다. Unity Editor는 해당 worktree 경로를 별도 프로젝트로 열어야 하며, 다른 worktree의 `Library`를 복사하거나 공유하지 않는다.

## 에이전트 작업 계약

작업을 지시할 때 아래 다섯 항목을 함께 준다.

```text
Worktree: ../UPnL_26SJ-worktrees/<scope>
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
git -C ../UPnL_26SJ-worktrees/combo-rules diff --check
git -C ../UPnL_26SJ-worktrees/combo-rules status --short
```

Unity 코드 또는 asset 변경이면 해당 worktree를 대상으로 Unity Test Runner 또는 batchmode compile도 실행한다. 검증이 실패하면 커밋이나 통합 전에 원인을 기록하고 해결한다.

작업 완료 후에는 작업 branch에만 필요한 파일을 명시적으로 커밋한다.

```bash
git -C ../UPnL_26SJ-worktrees/combo-rules add Assets/Game/Scripts/Runtime/Combo
git -C ../UPnL_26SJ-worktrees/combo-rules commit -m "feat: add combo rules"
```

통합 담당자는 main에서 한 branch씩 통합하고 매번 검증한다.

```bash
git switch main
git pull --ff-only
git merge --no-ff agent/combo-rules
git diff --check
```

Unity 변경이면 merge 직후 main에서 Unity 검증을 다시 실행한다. 한 branch의 검증이 실패하면 다음 branch를 merge하지 않는다.

## 정리와 복구

통합이 끝난 뒤에만 worktree와 branch를 제거한다.

```bash
git worktree remove ../UPnL_26SJ-worktrees/combo-rules
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
- worktree는 리포 밖 `../UPnL_26SJ-worktrees/`에만 생성한다.
