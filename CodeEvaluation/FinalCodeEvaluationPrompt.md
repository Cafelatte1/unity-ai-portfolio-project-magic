# ✅ Unity C# Codebase Final Evaluation Guide (최종 종합 평가)

본 문서는 Unity C# 코드베이스 전체(예: `Assets/Scripts/`)를 대상으로 **최종 종합 평가**를 수행하기 위한 가이드라인입니다.
기존 개별 평가 내용이 존재하는 경우, **이미 언급된 이슈는 중복 서술하지 않고 “요약 + 최종 결론” 중심**으로 반영합니다.

---

## ✅ 평가 원칙

### 1) 평가 범위

* 전체 시스템 관점(아키텍처/의존성/확장성/성능/안정성)을 우선
* “좋은 코드 조각”보다 **코드베이스 전체의 일관성과 운영 가능성**에 집중

### 2) 점수 기준 (모든 항목 공통)

* **각 항목은 5점 만점(0~5점)** 으로 채점합니다.

  * **5점**: 모범적 / 일관적 / 확장 및 운영에 문제 없음
  * **4점**: 전반적으로 좋음 / 일부 개선 여지
  * **3점**: 동작은 하나 구조적 부채가 보임 / 리팩토링 필요
  * **2점**: 반복되는 문제로 유지보수 비용 큼 / 결함 가능성 높음
  * **1점**: 구조적으로 위험 / 빈번한 버그·확장 어려움
  * **0점**: 사실상 부재 / 시스템적으로 성립 어려움

### 3) 최종 산출물 구성 원칙

* 각 항목별로: **점수(0~5) + 근거 요약 + 개선안(핵심만)**
* 마지막에: **전체 요약 점수표 + 최우선 개선 3개 + 중장기 리팩토링 3개 + 강점 3개**

---

## 📌 1. Architecture & Unity Component Design (0~5점)

* SRP(Single Responsibility) 준수 여부
* 클래스/컴포넌트 간 책임 분리 적절성
* Unity 생명주기(Awake/Start/Update/FixedUpdate 등) 사용 적절성
* Composition vs Inheritance 구조 선택의 타당성
* ScriptableObject / Event / Message / DI 등 구조적 확장성

---

## 📌 2. Code Readability & Maintainability (0~5점)

* 네이밍 규칙 일관성
* 변수/필드/메서드의 의미 전달력
* 주석/요약(XML) 품질 및 과/부족 여부
* 중복 제거 수준(하드코딩, 반복 로직, copy-paste)
* 모듈화(Too big methods / God class 징후 포함)

---

## 📌 3. Performance & Memory Efficiency (0~5점)

* Update/FixedUpdate/Coroutine 사용 최적화
* 불필요한 GC 발생 요소(LINQ/boxing/new allocation 등)
* 재사용 패턴(Object Pool 등) 적용 적절성
* Physics2D/3D 처리 비용 최적화
* Allocations/string/reflection 사용 통제
* GC-friendly 코딩 여부(프레임 안정성 관점)

---

## 📌 4. Gameplay Logic & Stability (0~5점)

* 입력 처리(Input System) 안정성
* 상태 전이(FSM/BT 등) 안정성과 확장성
* 동시성·타이밍 이슈 가능성(버퍼, 스냅샷 입력, 애니메이션 동기화 등)
* null/exception 방어적 프로그래밍 수준
* Edge case 처리(ground check, 탐지 범위, velocity, animation sync 등)

---

## 📌 5. Unity Engine Best Practices (0~5점)

* GetComponent 호출/캐싱 전략
* Rigidbody/Collider/Animator 접근 및 제어 방식 적절성
* Time.deltaTime 기반 보정의 일관성
* physics와 transform 수정 충돌 여부
* SerializeField / Header / Tooltip 등 인스펙터 설계 품질
* Magic number 제거 및 상수화/데이터화
* Prefab 중심 개발 방식 준수(세팅 분리)

---

## 📌 6. Animation, FSM/BT, Event Flow (0~5점)

* Animator state sync와 code-driven state sync의 정합성
* 공격/피격/이동 동시 처리에서의 충돌 가능성
* root motion과 velocity 기반 이동 혼합 리스크
* FSM/BT transition 구조의 명확성(디버깅 가능성 포함)
* Animation Event 의존도 적절성(과의존 여부)
* StateMachineBehaviour / AnimationController 설계 품질

---

## 📌 7. Safety & Error Handling (0~5점)

* null 방어 코드 수준
* TryGetComponent / Guard clause 활용 여부
* 입력/비동기 처리에서의 안전장치(디바운스, 버퍼, 초기화 순서)
* 경계값(Boundary) 체크 일관성
* 레이스 컨디션/초기화 순서/씬 전환 시 안정성

---

## 📌 8. Scalability & Extensibility (0~5점)

* 캐릭터/스킬/무기/행동 추가 시 유지 가능한 구조인지
* 데이터 기반 구조(테이블/ScriptableObject) 분리 수준
* hard-coded 로직과 데이터의 분리
* Event-driven / SO-driven 구조 판단
* 의존성 역전(DI) 가능성 및 테스트 친화성

---

## 📌 9. Clean Code Practices (0~5점)

* SOLID 원칙 전반 준수
* DRY/KISS/YAGNI 충족 여부(과설계/과최적화 포함)
* 불필요한 public 노출 여부
* property vs field 사용 적절성
* 네임스페이스/어셈블리(asmdef) 구조 평가
* 폴더 구조 및 script organization 품질

---

## 📌 10. Intel OpenVINO 기반 AI 모델 추론 프로세스 (0~5점)

* 초기화/로드 구조 안정성(씬 전환, 재로딩 포함)
* 추론 실행 흐름(동기/비동기/스레딩) 적절성
* Pre/Post-process & 데이터 변환의 책임 분리
* 메모리/리소스 관리 및 오류 처리(Dispose, 예외, 재시도 정책)
* Unity Gameplay 루프와의 통합 품질(지연, UI 반영, 이벤트 흐름)

---

## 📌 최종 종합 평가 출력 형식 (필수)

```
[1. Architecture] (점수: X/5)
- 핵심 평가:
- 주요 리스크/부채(요약):
- 우선 개선안(핵심 1~3개):

...

[10. OpenVINO Inference] (점수: X/5)
- 핵심 평가:
- 주요 리스크/부채(요약):
- 우선 개선안(핵심 1~3개):

[최종 종합]
- 항목별 점수 요약: (1)X/5 (2)X/5 ... (10)X/5
- 총평(한 단락):
- Best parts(강점) TOP 3:
  1)
  2)
  3)
- 개선해야 할 부분 TOP 3:
  1)
  2)
  3)
- 중장기 리팩토링 제안 TOP 3:
  1)
  2)
  3)
```