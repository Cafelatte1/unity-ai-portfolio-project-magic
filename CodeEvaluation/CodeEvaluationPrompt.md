# ✅ **Unity C# Code Evaluation Prompt (부문별 평가)**

아래 기준에 따라 내가 제공하는 Unity C# 코드의 품질을 **부문별로 상세 평가**해주세요.
필요하면 예시, 개선 코드, 설계적 관점 등을 함께 제안해주세요.

---

## 📌 1. **Architecture & Unity Component Design**

* SRP(Single Responsibility) 준수 여부
* 클래스/컴포넌트 간 책임 분리 적절성
* Unity 생명주기(Awake/Start/Update/FixedUpdate 등) 사용 적절성
* Composition vs. Inheritance 구조 판단
* ScriptableObject, Event, Message, DI 등 구조적 확장성
---

## 📌 2. **Code Readability & Maintainability**

* 네이밍 규칙 일관성
* 변수/필드/메서드 명의 의미 전달력
* 적절한 주석/요약 XML 사용 여부
* 불필요한 중복 제거(중복 로직, 하드코딩 등)
* 모듈화 수준(Too big methods 여부 포함)

---

## 📌 3. **Performance & Memory Efficiency**

* Update/FixedUpdate/Coroutine 사용 최적화
* 불필요한 GC 발생 요소(Linq, boxing, new allocation 등)
* 객체 재사용 패턴 유무(Pool 필요 여부 판단)
* Physics2D/3D 처리 비용 최적화
* Allocations, string 처리, reflection 사용 여부
* GC-friendly 코드인지 평가

---

## 📌 4. **Gameplay Logic & Stability**

* 입력 처리(Input System) 안정성
* 상태 전이(FSM, BT 등) 안정성과 확장성
* 동시성·타이밍 문제 가능성(예: Jump buffer, 공격 중 이동 문제)
* null/exception 방어적 프로그래밍 여부
* Edge case 처리(탐지 범위, ground check, velocity, animation sync 등)

---

## 📌 5. **Unity-Engine Best Practices**

* GetComponent 호출 횟수 / cache 전략
* Rigidbody, Collider, Animator 등 Unity 객체 접근 방식
* Time.deltaTime 기반 보정 적절성
* physics와 transform 수정 충돌 여부
* SerializableField, Header/Tooltip 설정 여부
* Magic number 제거 여부
* Prefab 중심 개발 방식 준수

---

## 📌 6. **Animation, FSM/BT, Event Flow**

* Animator sync와 code-driven state sync
* Attack 중 움직임 문제 가능성
* root motion/velocity 기반 이동 혼합 문제
* FSM/BT transition 구조 적절성
* Animation Event 사용 적절성 (과한 의존?)
* StateMachineBehaviour, AnimationController 구조 평가

---

## 📌 7. **Safety & Error Handling**

* null 방어 코드
* TryGetComponent 활용 여부
* Input 비동기 처리 문제(버퍼, snapshot input, debounce 등)
* Boundary 체크 철저함
* 레이스 컨디션, initialize 순서 문제 가능성

---

## 📌 8. **Scalability / Extensibility**

* 캐릭터가 늘어나도 유지 가능한 구조인지
* 스킬, 무기, 행동 등이 추가되었을 때 확장 가능성
* hard-coded 로직과 데이터 기반 구조 분리
* Event-driven, ScriptableObject-driven 구조 판단
* 의존성 역전(DI) 가능성

---

## 📌 9. **Clean Code Practices**

* SOLID 원칙 전반 평가
* DRY/KISS/YAGNI 충족 여부
* 불필요한 public 필드 여부
* property vs field 적절성
* 네임스페이스 구조 평가
* 폴더 구조 및 script organization best practice

---

## 📌 **10. Intel OpenVINO 기반 AI 모델 추론 프로세스**

* 초기화 및 로드 구조 안정성
* 추론 실행 흐름 및 비동기 처리 적절성
* 데이터 변환(Process & Pre/Post)
* 메모리 및 오류 관리 안정성
* Unity Gameplay와의 통합 품질

---

## 📌 **종합 평가(10/10) + 개선 우선순위**

* 10가지 항목의 요약 점수 제시
* "즉시 개선해야 할 부분" 3개
* "중장기적 리팩토링 제안" 3개
* "Best parts" 3개(장점)

---

# 📌 출력 형식 예시

```
[1. Architecture]
- 평가 + 문제점 + 개선안

[2. Readability]
- 평가 + 문제점 + 개선안

...

[종합 평가]
- 총평
- 우선 개선 3개
- 중장기 리팩토링 3개
- 잘한 점 3개
```
