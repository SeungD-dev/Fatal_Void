# CLAUDE.md

이 파일은 이 저장소에서 작업할 때 Claude Code (claude.ai/code)에게 지침을 제공합니다.

## 프로젝트 개요

**Fatal Void**는 Unity 6000.0.24f1로 제작된 2D 생존 스타일 탑다운 슈터 게임입니다. 웨이브 기반의 적 스폰, 자동 공격 무기와 업그레이드 시스템, 장비/인벤토리 시스템이 특징입니다. 이 게임은 Universal Render Pipeline(URP)을 사용하여 모바일 플랫폼에 최적화되어 있습니다.

## 개발 명령어

### 프로젝트 빌드
이 Unity 프로젝트는 표준 Unity 빌드 시스템을 사용합니다. 현재 구현된 커스텀 빌드 스크립트는 없습니다.

**Unity Editor GUI 방법:**
1. File → Build Settings
2. 타겟 플랫폼 선택 (주로 Android로 구성됨)
3. "Build" 또는 "Build and Run" 클릭

**커맨드 라인 빌드 (커스텀 빌드 스크립트 필요):**
```bash
Unity.exe -quit -batchmode -projectPath "." -buildTarget Android -executeMethod BuildScript.BuildGame
```

### 테스트
프로젝트에는 Unity Test Framework (1.4.5)가 포함되어 있습니다:
- **Test Runner**: Window → General → Test Runner
- **성능 테스트**: Window → Analysis → Performance Test Report

### 게임 실행
- Unity Editor에서 `Assets/Scenes/IntroScene.unity` 열기
- Play 버튼을 눌러 전체 게임 플로우 실행: Intro → Title → Loading → Combat

## 아키텍처 개요

### 핵심 시스템
- **GameManager**: 게임 상태와 씬 전환을 관리하는 싱글톤 패턴
- **CombatController**: 전투 메커니즘, 적 사망, 아이템 드롭 처리
- **ObjectPool**: 발사체, 적, VFX를 위한 성능 최적화된 풀링
- **WeaponManager**: 10여 종의 무기를 지원하는 동적 무기 장착 시스템
- **PlayerController**: 조이스틱 입력을 사용한 물리 기반 이동

### 씬 흐름
1. **IntroScene** - 게임 스플래시 스크린과 소개
2. **TitleScene** - 메인 메뉴와 옵션
3. **LoadingScene** - 진행률을 보여주는 로딩 스크린
4. **CombatScene** - 웨이브 기반 생존 메커니즘이 있는 메인 게임플레이

### 주요 디렉토리
- `Assets/Scripts/Combat/` - 핵심 게임플레이 메커니즘 (적, 무기, 플레이어)
- `Assets/Scripts/Core/` - 게임 매니저와 컨트롤러
- `Assets/Scripts/UI/` - 사용자 인터페이스 컴포넌트
- `Assets/Resources/Prefabs/` - 카테고리별로 정리된 게임 오브젝트 프리팹
- `Assets/Data/` - ScriptableObject 데이터 에셋
- `Assets/Externals/` - 카테고리별로 정리된 스프라이트 에셋

### 데이터 아키텍처
프로젝트는 데이터 관리를 위해 ScriptableObject를 사용합니다:
- **WeaponDatabase** - 중앙화된 무기 구성
- **EnemySpawnDatabase** - 적 스폰 패턴과 웨이브 데이터
- **SoundBanks** - 오디오 클립 정리
- **WaveData** - 웨이브 진행 설정

### 성능 시스템
- **Object Pooling**: 발사체, 적, 이펙트에 대한 광범위한 풀링
- **Enemy Culling**: EnemyCullingManager를 통한 화면 밖 적 관리
- **Sprite Atlasing**: 성능 최적화된 스프라이트 렌더링
- **Memory Management**: 씬 전환 중 적극적인 정리

### 주요 종속성
- **DOTween** - 애니메이션과 트위닝 시스템
- **Unity Input System** - 모바일용 현대적 입력 처리
- **Universal Render Pipeline** - 2D 최적화 렌더링
- **TextMesh Pro** - 고급 텍스트 렌더링

## 무기 시스템
모듈식 무기 아키텍처는 쉬운 확장을 지원합니다:
- 각 무기 타입은 `Assets/Scripts/Combat/WeaponMechanism/`에 자체 메커니즘 클래스를 가집니다
- 무기 데이터는 `Assets/Resources/ScriptableObject/Weapons/`의 ScriptableObject를 통해 구성됩니다
- 성능을 위한 오브젝트 풀링이 적용된 발사체 기반 전투

## 개발 노트
- 타겟 프레임 레이트: 30 FPS (모바일 최적화)
- 주요 플랫폼: Android (모바일)
- 일부 영역에서 한국어 네이밍 컨벤션 사용
- 전문적인 Unity 패턴 구현 (Singleton, Object Pooling, State Machines)
- 적극적인 정리 시스템으로 모바일 기기에 메모리 최적화

## 새 콘텐츠 추가
- **새 무기**: WeaponDatabase에서 ScriptableObject를 생성하고 메커니즘 클래스 구현
- **새 적**: 스폰 데이터베이스에 적 타입을 추가하고 AI 상태 머신 구현
- **새 장비**: 스탯 수정자와 함께 장비 시스템에 추가
- **새 씬**: GameManager를 통한 기존 씬 전환 패턴을 따름

## 클로드 코드 개발 규칙
1. **계획 수립** - 문제를 충분히 생각하고 관련 파일의 코드베이스를 읽어 /todo.md에 계획 작성
2. **체크리스트 작성** - 완료 시 체크 표시할 수 있는 할 일 목록 포함
3. **계획 검증** - 작업 시작 전 사용자에게 연락하여 계획 검증
4. **진행 상황 추적** - 할 일 항목을 완료로 표시하며 작업 진행
5. **변경 사항 보고** - 모든 단계에서 어떤 변경을 했는지 간략하게 설명
6. **단순함 유지** - 모든 작업과 코드 변경은 가능한 한 간단하게, 코드에 미치는 영향 최소화
7. **검토 섹션 추가** - /todo.md 파일에 변경 사항 요약과 기타 관련 정보를 담은 검토 섹션 추가
8. **불필요한 디버그 로그 지양** 사용자가 요청하기 전까지는 Debug.log 사용을 지양할 것.
9. **최적화 중요시** 모바일 게임이므로 최적화를 고려하여
Find 함수나 GetComponent 함수를 사용할 때 주의할 것.