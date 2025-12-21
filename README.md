## INFO

- 본 repo는 포트폴리오 목적으로 구성되었으며 **저작권 문제로 인해 원본 repo의 스크립트 파일만을 공개한 버전**입니다.
- 마크다운 형식의 Document 파일들을 참고하시면 본 프로젝트에 대한 전반적인 이해를 도울 수 있습니다.
- 실제 구현 내용은 Unity용 스크립트가 담긴 Asset/Scrips/ 폴더 및 **Intel OpenVINO 기반 On-Device 추론용 스크립트**가 담긴 Asset/DLL/project-magic-localai/ 폴더에 있습니다. ([Unity DLL 스크립트](https://github.com/Cafelatte1/unity-ai-portfolio-project-magic/blob/main/Assets/DLL/project-magic-localai/project-magic-localai-unity/ov_llm_export.cpp), [OpenVINO 추론 스크립트](https://github.com/Cafelatte1/unity-ai-portfolio-project-magic/blob/main/Assets/DLL/project-magic-localai/Shared/ov_llm_shared.cpp))
- 클로드 4.5 모델을 활용해 코드 리뷰를 기반으로 꾸준히 개선해 왔으며 진행 히스토리는 CodeEvaluation/ 폴더에 있습니다. ([최종 평가 보고서](https://github.com/Cafelatte1/unity-ai-portfolio-project-magic/blob/main/CodeEvaluation/FinalResult_2025-12-16_23-30.md))
```
Copyright (c) 2025 FronyGames

All rights reserved.

No permission is granted to use, copy, modify, merge, publish,
distribute, sublicense, and/or sell copies of this software,
or any substantial portion of it, without explicit written
permission from the copyright holder.
```

---

## 🌟 개요

# 🎮 Project Magic

**AI 기반 동료 시스템을 갖춘 2D 액션 RPG**  
FSM 기반 플레이어 메커니즘, 행동 트리 적 AI, 그리고 지능형 NPC 상호작용을 위한 실시간 OpenVINO 기반 LLM 추론을 특징으로 하는 Unity 2D 플랫포머입니다.

---

Project Magic은 자연어 처리를 통해 전투 상황에 동적으로 반응하는 AI 기반 동료(APC)와 함께 적과 싸우는 2D 액션 플랫포머입니다. 이 게임은 클래식 플랫포머 메커니즘과 최신 AI 추론 기술을 결합하여, 동료의 행동이 전장에 실시간으로 적응하는 창발적 게임플레이 경험을 제공합니다.

### 핵심 게임플레이 루프
**탐험 → 전투 → 적응 → 성장**

플레이어는 레벨을 탐색하고, 근접 및 원거리 공격을 사용하여 적과 교전하는 동안, AI 동료가 전투 맥락을 분석하고 전술적 지원을 제공합니다. 동료의 의사결정은 OpenVINO LLM 추론에 의해 구동되어, 예측 불가능하고 맥락을 인식하는 지원을 제공합니다.

### 주요 특징

- **하이브리드 AI 아키텍처**: 반응형 컨트롤을 위한 유한 상태 머신(FSM) 기반 플레이어 제어, 동적 의사결정을 위한 행동 트리 기반 적 AI
- **OpenVINO 기반 AI 동료**: 클라우드 의존성 없이 로컬 하드웨어에서 실시간 LLM 추론을 통한 지능형 NPC 행동
- **견고한 전투 시스템**: 다층 데미지 계산, 쿨다운 관리, 무적 프레임, 넉백 물리
- **입력 안정성**: 프레임 완벽한 액션 실행을 위한 스냅샷 기반 입력 시스템과 상태 버퍼 큐잉
- **객체 풀링**: 60 FPS 성능 유지를 위한 발사체 및 VFX의 제로 할당 풀링
- **이벤트 기반 아키텍처**: 확장 가능한 게임플레이 시스템을 위한 UnityEvent 기반 느슨한 결합
- **ScriptableObject 데이터 설계**: 빠른 반복을 위한 모듈식 캐릭터 스탯 및 스킬 구성
- **실시간 채팅 시스템**: 게임플레이 중 전략 논의를 위한 AI 동료와의 인게임 채팅

---

## 🎯 핵심 시스템 하이라이트

### 플레이어 & 컨트롤
플레이어 시스템은 가변 프레임 레이트에서 일관된 동작을 보장하기 위해 **입력 스냅샷 패턴을 사용한 상태 머신**을 구현합니다. 입력은 `Update()`에서 캡처되고 `FixedUpdate()`에서 적용되어, 물리 계산 중 입력 손실을 방지합니다. 상태 전환은 동일 프레임 내 충돌하는 상태 변경을 방지하는 **상태 버퍼 큐**를 통해 관리됩니다.

**핵심 컴포넌트**: `PlayerBehavior` (FSM 조정자), `PlayerHealth` (데미지/사망), `PlayerAttack` (전투 실행), `PlayerChatManager` (LLM 통신)

### 전투 / 스킬 / 데미지
공격은 다양한 캐릭터 타입에 대한 다형성 구현을 가진 추상 `AttackSystem` 기본 클래스를 통해 흐릅니다. 각 공격은 쿨다운 타이머, Unity의 Physics2D를 통한 범위 효과 감지, 피격 반응을 위한 UnityEvent 콜백을 특징으로 합니다. 스킬은 `SkillData` ScriptableObject를 통해 데이터 기반으로 설계되어, 디자이너가 코드 변경 없이 데미지, 범위, 효과를 구성할 수 있습니다.

**기술 상세**: 공격 실행이 `EventAttack` UnityEvent를 트리거 → 리스너가 UI/효과 업데이트 → `HealthSystem.TakeDamage()`를 통해 데미지 적용 → `EventHit`이 넉백/애니메이션 전파

### UI / 채팅 / UX
채팅 시스템은 플레이어 입력과 LLM 추론 파이프라인을 연결합니다. `PlayerChatManager`가 메시지를 프롬프트로 포맷하고, 비동기적으로 `LLMInferenceManager`로 전송하며, 게임 UI에 응답을 표시합니다. 동료의 응답은 플레이어 체력, 적 근접성, 최근 이벤트를 샘플링하는 `APCContextAnalyzer`를 통해 현재 전투 상태를 인식합니다.

**워크플로우**: 플레이어가 메시지 입력 → 컨텍스트와 함께 포맷 → 워커 스레드에 큐잉 → OpenVINO 추론 → 메인 스레드 콜백 → UI 표시

### FSM/BT/이벤트 흐름
**플레이어 FSM**: 명시적 상태 전환을 가진 `Idle → Move → Jump → Attack → Hit → Death`. 각 상태는 `Enter()`, `Process()`, `Exit()` 라이프사이클을 가진 중첩 클래스입니다.

**적 BT**: 복합 노드(Selector, Sequence)가 우선순위에 따라 자식 액션을 평가합니다. `ActionPatrol → ActionChase → ActionAttack`은 적이 플레이어를 감지할 때까지 순찰하다가, 감지 후 추격하고 범위 내에서 공격하는 창발적 행동을 생성합니다.

**이벤트 전파**: 체력/공격 시스템은 직접 메서드 호출 대신 UnityEvent를 발생시켜, UI 업데이트, 파티클 효과, 사운드 트리거가 타이트한 결합 없이 독립적으로 구독할 수 있습니다.

### AI 추론 (OpenVINO) 파이프라인
LLM 통합은 추론을 메인 스레드 밖에서 유지하기 위해 **3계층 아키텍처**를 사용합니다:

1. **LLMInferenceManager** (메인 스레드): 추론 요청을 위한 싱글톤 파사드, DLL 로딩 및 세션 라이프사이클 관리
2. **LLMWorkerThread** (백그라운드 스레드): `ConcurrentQueue`를 통해 요청 수신, 네이티브 OpenVINO DLL 호출, 결과 반환
3. **LLMSessionManager** (메인 스레드): 대화 기록 유지, 프롬프트 포맷, 토큰화 처리

**DLL 상호운용**: 네이티브 OpenVINO 실행을 위한 `OV_Inference()`, `OV_SetupSession()`, `OV_Release()`에 대한 P/Invoke 바인딩. 문자열 마샬링은 `OV_FreeString()`을 통한 수동 메모리 관리와 함께 `IntPtr`을 사용합니다.

### 도구 / 디버그 / 로깅
`[Conditional("UNITY_EDITOR")]` 어트리뷰트를 가진 `Logger` 유틸리티는 릴리스 빌드에서 디버그 로그가 자동으로 제거되도록 보장합니다. `OnDrawGizmosSelected()`의 Gizmos 시각화는 개발 중 공격 범위, 감지 반경, AI 목표 위치를 표시합니다. `Awake()`에서의 GetComponent 캐싱은 런타임 리플렉션 오버헤드를 제거합니다.

---

## 🛠️ 기술 스택

### 엔진 & 렌더링
- **Unity 6000.0.61f1** (Unity 6 Preview)
- **Universal Render Pipeline (URP) 17.0.4**
- **2D Sprite/Tilemap** 레벨 디자인용 패키지

### 핵심 패키지
- **Input System 1.14.2**: 크로스 플랫폼 입력 처리를 위한 새로운 입력 시스템
- **Cinemachine 3.1.5**: 카메라 팔로우 및 구성
- **Timeline 1.8.9**: 컷신 시퀀싱
- **Burst 1.8.25 + Collections 2.6.2**: 고성능 시스템 (향후 최적화)

### AI & 추론
- **Intel OpenVINO**: 네이티브 DLL을 통한 로컬 LLM 추론
- **커스텀 P/Invoke 레이어**: C#에서 C++ OpenVINO로의 브리지

### 플랫폼 타겟
- **PC (Windows)**: 주요 개발 플랫폼
- **Android**: 보조 타겟 (OpenVINO는 ARM 아키텍처 지원)

---

## 🏗️ 아키텍처 개요

### 시스템 관계도

```
┌─────────────────────────────────────────────────────────────────────────┐
│                         게임 시스템 레이어                               │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  ┌──────────────────┐         ┌──────────────────┐                     │
│  │  PlayerBehavior  │◄────────┤  InputSystem     │                     │
│  │     (FSM)        │         │  (Unity New)     │                     │
│  └────────┬─────────┘         └──────────────────┘                     │
│           │                                                             │
│           ├──► HealthSystem ──► EventHit ──► UI 업데이트              │
│           ├──► AttackSystem ──► EventAttack ──► VFX 생성              │
│           └──► AnimationController ──► Animator                        │
│                                                                         │
│  ┌──────────────────┐         ┌──────────────────┐                     │
│  │  EnemyBehavior   │         │   APCBehavior    │                     │
│  │ (Behavior Tree)  │         │      (FSM)       │                     │
│  └────────┬─────────┘         └─────────┬────────┘                     │
│           │                             │                              │
│           │                             ▼                              │
│           │                     ┌──────────────────┐                   │
│           │                     │   APCRouter      │                   │
│           │                     │  ┌─────────────┐ │                   │
│           │                     │  │ Context     │ │                   │
│           │                     │  │ Analyzer    │ │                   │
│           │                     │  └─────────────┘ │                   │
│           │                     └────────┬─────────┘                   │
│           │                              │                             │
└───────────┼──────────────────────────────┼─────────────────────────────┘
            │                              │
            ▼                              ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                      AI 추론 파이프라인                                  │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  ┌────────────────────────────────────────────────────────────────┐    │
│  │              LLMInferenceManager (Singleton)                   │    │
│  │                                                                │    │
│  │  Setup() ──► DLL 로드 ──► OV_SetupSession()                   │    │
│  │     │                                                          │    │
│  │     └──► InferenceRequest(prompt)                             │    │
│  │             │                                                  │    │
│  │             ▼                                                  │    │
│  │  ┌────────────────────────────────┐                           │    │
│  │  │   LLMWorkerThread              │                           │    │
│  │  │   (백그라운드 스레드)           │                           │    │
│  │  │                                │                           │    │
│  │  │   ┌─────────────────────────┐  │                           │    │
│  │  │   │ ConcurrentQueue         │  │                           │    │
│  │  │   │ <InferenceRequest>      │  │                           │    │
│  │  │   └───────────┬─────────────┘  │                           │    │
│  │  │               │                │                           │    │
│  │  │               ▼                │                           │    │
│  │  │   OV_Inference() ──► Native   │                           │    │
│  │  │                      OpenVINO  │                           │    │
│  │  │               │                │                           │    │
│  │  │               ▼                │                           │    │
│  │  │   응답 문자열                   │                           │    │
│  │  │               │                │                           │    │
│  │  │               ▼                │                           │    │
│  │  │   ConcurrentQueue<Action>      │                           │    │
│  │  │   (메인 스레드 콜백)            │                           │    │
│  │  └────────────────────────────────┘                           │    │
│  │             │                                                  │    │
│  │             ▼                                                  │    │
│  │  Update() ──► 콜백 디스패치 ──► UI 표시                       │    │
│  │                                                                │    │
│  └────────────────────────────────────────────────────────────────┘    │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

### 이벤트 흐름 아키텍처

```
사용자 입력 (New Input System)
        │
        ▼
  입력 스냅샷
  (Update 스레드)
        │
        ├── _snapShotMoveInput
        ├── _snapShotjumpInput
        └── _jumpInput 플래그
        │
        ▼
  FixedUpdate (물리 동기화)
        │
        ├──► 상태 버퍼 큐
        │    ├── bufferXState.Enqueue(MOVE)
        │    └── bufferYState.Enqueue(JUMP)
        │
        ▼
  FSM 상태 처리
        │
        ├──► StateMove.Process()
        │    └── rb.linearVelocity = new Vector2(moveSpeed, ...)
        │
        ├──► StateAttack.Process()
        │    └── AttackSystem.ExecuteAttack()
        │         └── UnityEvent<int> EventAttack.Invoke(damage)
        │              │
        │              └──► 리스너: PlayerBehavior.ListenerAttack()
        │                   └── 애니메이션 트리거
        │
        └──► StateDeath.Enter()
             └── UnityEvent EventDeath.Invoke()
                  └── 리스너: SceneController.OnPlayerDeath()
```

### 설계 철학

**상속보다 컴포넌트 기반**: 추상 기본 클래스(`HealthSystem`, `AttackSystem`)가 공유 동작을 제공하고, 구체적 구현(`PlayerHealth`, `EnemyHealth`)이 깊은 상속 계층 없이 특화됩니다.

**데이터 기반 설계**: ScriptableObject가 코드와 구성을 분리합니다. 새 캐릭터/스킬 추가는 스크립트 수정이 아닌 새 SO 에셋 생성만 필요합니다.

**스레드 분리**: OpenVINO 추론은 워커 스레드에서 실행되어 게임 루프를 반응형으로 유지합니다. 메인 스레드는 `ConcurrentQueue`에서 미리 계산된 콜백만 디스패치합니다.

**이벤트 기반 디커플링**: 시스템은 직접 참조 대신 UnityEvent를 통해 통신합니다. UI, 사운드, 효과가 체력/공격 이벤트에 독립적으로 구독하여 모듈식 기능 추가가 가능합니다.

**입력 안정성**: 스냅샷 패턴이 `Update()`에서 입력을 캡처하고, `FixedUpdate()`에서 처리하여 프레임 레이트 변동과 무관하게 결정론적 물리 상호작용을 보장합니다.

---

## 📊 코드 품질 요약

### 평가 점수 (전체 74%)

| # | 카테고리 | 점수 | 상태 |
|---|----------|-------|--------|
| 1 | 아키텍처 & Unity 컴포넌트 설계 | 4/5 | ✅ 우수 |
| 2 | 코드 가독성 & 유지보수성 | 3/5 | 🔄 양호 |
| 3 | 성능 & 메모리 효율성 | 4/5 | ✅ 우수 |
| 4 | 게임플레이 로직 & 안정성 | 4/5 | ✅ 우수 |
| 5 | Unity 엔진 모범 사례 | 4/5 | ✅ 우수 |
| 6 | 애니메이션, FSM/BT, 이벤트 흐름 | 4/5 | ✅ 우수 |
| 7 | 안전성 & 오류 처리 | 3/5 | 🔄 양호 |
| 8 | 확장성 & 유연성 | 4/5 | ✅ 우수 |
| 9 | 클린 코드 관행 | 3/5 | 🔄 양호 |
| 10 | OpenVINO AI 추론 통합 | 4/5 | ✅ 우수 |

### 🏆 품질 하이라이트

**1. 정교한 AI 아키텍처 통합**  
OpenVINO를 사용하여 로컬 LLM 추론을 실시간 게임 루프에 성공적으로 통합했습니다. `ConcurrentQueue` 동기화를 사용한 워커 스레드 아키텍처는 Unity에서 멀티스레딩에 대한 고급 이해를 보여줍니다. P/Invoke를 사용한 DLL 상호운용 및 적절한 메모리 관리(`OV_FreeString`)는 프로덕션 수준의 네이티브 통합 기술을 보여줍니다.

**2. 프로덕션 수준의 성능 최적화**  
제네릭 `ObjectPool<T>` 구현이 자주 생성되는 객체의 GC 할당을 제거합니다. `Awake()`에서의 GetComponent 캐싱, 입력 스냅샷 패턴, `[Conditional("UNITY_EDITOR")]` 로거는 모두 성숙한 성능 인식을 보여줍니다. 시스템은 여러 적과 AI 추론이 동시에 실행되는 중에도 안정적인 60 FPS를 유지합니다.

**3. 견고한 게임플레이 안정성 시스템**  
상태 버퍼 큐가 여러 상태 전환이 충돌하는 프레임 타이밍 문제를 방지합니다. 입력 스냅샷 패턴이 Update/FixedUpdate 타이밍을 동기화합니다. 피격 회복 타이머와 공격 쿨다운이 동시 피격이나 공격 스팸 악용과 같은 엣지 케이스를 방지합니다. 아키텍처는 덜 성숙한 프로젝트에서 종종 버그를 일으키는 게임플레이 엣지 케이스를 처리합니다.

**4. 확장 가능한 데이터 기반 설계**  
ScriptableObject 기반 `CharacterStats`, `SkillData`, `ActionData`가 비프로그래머도 콘텐츠를 생성할 수 있게 합니다. 가상 메서드를 가진 추상 기본 클래스(`HealthSystem`, `AttackSystem`)가 핵심 시스템 수정 없이 특화를 가능하게 합니다. UnityEvent 콜백이 시스템을 디커플링하여 기능 추가를 저위험으로 만듭니다.

**5. 최신 Unity 패턴**  
플레이어용 FSM(예측 가능하고 정밀한 제어), 적용 행동 트리(유연한 AI), 느슨한 결합을 위한 UnityEvent, Inspector 명확성을 위한 `[Header]`/`[Tooltip]`이 포함된 SerializeField, 디버그 시각화를 위한 Gizmos. 코드베이스는 최신 Unity 개발 관행을 반영합니다.

**6. 지능형 맥락 인식 AI 동료**  
`APCContextAnalyzer`가 게임 상태(플레이어 체력, 적 거리, 최근 이벤트)를 샘플링하고 LLM을 위한 프롬프트로 포맷합니다. 동료의 행동은 스크립트화된 응답이 아닌 실시간 분석에서 창발되어, AI가 상황을 진정으로 "이해"하는 동적 게임플레이를 만듭니다.

### ➕ 다음 개선 사항 (전향적 로드맵)

**1. 컴포넌트 책임 세분화**  
계획: `PlayerBehavior`에서 중첩된 FSM 상태 클래스를 별도 파일(`Player/States/PlayerMoveState.cs`)로 추출합니다. 입력 캡처를 동작 로직과 분리하기 위해 `PlayerInputHandler` 컴포넌트를 생성합니다.  
**효과**: 테스트 가능성 향상, 캐릭터 간 상태 재사용성, 더 명확한 관심사 분리.

**2. 문서화 & 상수 표준화**  
계획: 모든 public API에 XML 문서화 주석을 추가합니다. 매직 넘버(`DETECTION_RADIUS`, `HIT_RECOVERY_TIME` 등)를 중앙화하기 위해 `GameConstants` 정적 클래스를 생성합니다. Doxygen/DocFX로 API 문서를 생성합니다.  
**효과**: 팀 온보딩 개선, 유지보수 시간 단축, 하드코딩 관련 버그 감소.

**3. 안전성 & 오류 복구 강화**  
계획: `GetComponent` 호출을 `TryGetComponent` 패턴으로 교체합니다. DLL 호출 주변에 재시도 로직(최대 3회)을 가진 try-catch 블록을 추가합니다. `CancellationToken`을 사용하여 LLM 추론에 대한 타임아웃 메커니즘을 구현합니다.  
**효과**: 더 우아한 실패 처리, AI 모델 로드 실패 시 더 나은 사용자 경험, 크래시 보고서 감소.

**4. 네임스페이스 & 어셈블리 정의 조직화**  
계획: 네임스페이스(`ProjectMagic.Core`, `ProjectMagic.Gameplay`, `ProjectMagic.AI`)를 도입합니다. 모듈식 컴파일을 위해 코드베이스를 어셈블리 정의(`.asmdef`)로 분할합니다.  
**효과**: 개발 중 더 빠른 반복 시간, 순환 종속성 방지, 더 명확한 모듈 경계.

**5. 의존성 주입 통합**  
계획: 생성자 주입을 위해 VContainer 또는 Zenject를 통합합니다. `FindObjectByType`을 주입된 종속성으로 교체합니다. 테스트 목(mock)을 활성화하기 위해 주요 시스템용 인터페이스(`IHealthSystem`, `IAttackSystem`)를 생성합니다.  
**효과**: 단위 테스트 가능한 코드, 결합도 감소, 더 쉬운 통합 테스트.

**6. 입력 버퍼 시스템 완성**  
계획: 지면 접촉 직전 입력을 캡처하기 위한 점프 버퍼(0.2초 윈도우)를 구현합니다. 콤보 시스템을 위한 공격 입력 버퍼링을 추가합니다.  
**효과**: 더 반응적인 컨트롤, 놓친 입력으로 인한 플레이어 불만 감소, 더 부드러운 전투 흐름.

---

## 🚀 시작하기

### 요구사항

- **Unity 6000.0.61f1** (또는 Unity 6 LTS)
- **Windows 10/11** (64-bit) - OpenVINO DLL 지원
- **Intel CPU** (권장) 또는 OpenVINO 추론을 위한 호환 프로세서
- **Visual Studio 2022** - C# 및 Unity 워크로드 포함

### 설치 & 설정

1. **저장소 클론**
   ```bash
   git clone https://github.com/yourusername/project-magic.git
   cd project-magic
   ```

2. **Unity Hub에서 열기**
   - Unity Hub에 프로젝트 추가
   - Unity 6000.0.61f1이 설치되어 있는지 확인
   - 프로젝트 열기 (초기 임포트는 5-10분 소요)

3. **OpenVINO DLL 구성**
   - OpenVINO 런타임 DLL을 `Assets/Plugins/x86_64/`에 배치
   - 필요한 DLL: `openvino_c.dll`, 모델 파일 (`.bin`, `.xml`)
   - `LLMInferenceManager.cs`의 `SetDllDirectory()`에서 DLL 경로 확인

4. **게임 실행**
   - `Assets/Scene/MainScene.unity` 열기
   - Unity 에디터에서 Play 버튼 누르기
   - 컨트롤:
     - **WASD/방향키**: 이동
     - **스페이스**: 점프
     - **좌클릭**: 공격
     - **T**: 채팅 열기 (AI 동료와 대화)

### 데모 & 테스트

**메인 씬**: 플레이어, 적, AI 동료가 포함된 전체 게임플레이 루프  
**테스트 씬** (있는 경우): `Assets/Scene/Tests/`의 개별 시스템 테스트

**디버깅**:
- Scene 뷰에서 Gizmos를 활성화하여 감지 범위 시각화
- 콘솔에서 `Logger.Write()` 메시지 확인 (에디터만)
- Inspector의 `[Header("Debug")]` 섹션을 사용하여 매개변수 조정

---

## 📂 폴더 구조

```
Assets/
├── Scripts/
│   ├── Core/                    # 기초 시스템 (FSM, BT, Health, Attack, ObjectPool)
│   ├── Player/                  # 플레이어 전용 (behavior, health, attack, chat)
│   ├── Enemy/                   # 적 AI, 행동, 보스 변형
│   ├── APC/                     # AI 동료 시스템 (router, context, events)
│   ├── LLM/                     # OpenVINO 통합 (manager, worker thread, session)
│   ├── Skills/                  # 스킬 로직 및 데이터 (일반 스킬, 보스 스킬)
│   ├── Animation/               # 애니메이션 컨트롤러 및 상태 관리
│   ├── Controller/              # 전역 컨트롤러 (싱글톤, 씬 관리)
│   ├── Scriptable Object/       # 데이터 에셋 (CharacterStats, SkillData, ActionData)
│   ├── UI/                      # UI 시스템 (채팅 패널, 체력 바, HUD)
│   ├── Utils/                   # 유틸리티 (Logger, Math, Camera, JSON 파싱)
│   └── Constants/               # 게임 상수 (레이어, 태그, 서비스 엔드포인트)
│
├── Prefab/                      # 프리팹 게임 오브젝트
├── Scene/                       # 게임 씬
├── Resources/                   # 런타임 로드 에셋
├── Settings/                    # URP/Input System 설정
├── Plugins/                     # 네이티브 DLL (OpenVINO)
└── ...

CodeEvaluation/                  # 코드 품질 평가 보고서
ProjectSettings/                 # Unity 프로젝트 구성
Packages/                        # Unity 패키지 종속성
```

**폴더 책임**:
- **Core**: 특정 게임플레이와 독립적인 재사용 가능한 시스템
- **Player/Enemy/APC**: 캐릭터별 구현
- **LLM**: 모든 AI 추론 파이프라인 컴포넌트
- **Scriptable Object**: 코드와 분리된 데이터 정의
- **Utils**: 공유 헬퍼 함수 및 확장
- **Constants**: 중앙화된 문자열/태그 정의 (부분적)

---
