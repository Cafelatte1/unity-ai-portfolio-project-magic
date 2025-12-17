# 시스템 아키텍처 다이어그램

## 사용자 쿼리 입력부터 APC 행동 결정 및 UI 출력까지의 전체 흐름

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                           1. 사용자 입력 레이어 (UI)                          │
└─────────────────────────────────────────────────────────────────────────────┘
                                      │
                    ┌─────────────────┴─────────────────┐
                    │      InputChat.cs                 │
                    │  • OnClicked()                    │
                    │  • 사용자 쿼리 텍스트 수집           │
                    │  • Trim 적용                      │
                    └─────────────────┬─────────────────┘
                                      │
                                      ▼
                    ┌─────────────────────────────────────────┐
                    │    UIController.playerChatMgr.sessionId  │
                    │    + userQuery                           │
                    └─────────────────┬───────────────────────┘
                                      │
┌─────────────────────────────────────────────────────────────────────────────┐
│                        2. APC 이벤트 처리 레이어                              │
└─────────────────────────────────────────────────────────────────────────────┘
                                      │
                                      ▼
                    ┌─────────────────────────────────────┐
                    │   APCEventTrigger.cs                │
                    │  • ReceiveUserChat()                │
                    │  • toolsJson 준비                   │
                    │  • apcToolExecutor 준비             │
                    └─────────────────┬───────────────────┘
                                      │
                                      ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                       3. LLM 세션 관리 레이어                                 │
└─────────────────────────────────────────────────────────────────────────────┘
                                      │
                    ┌─────────────────┴─────────────────┐
                    │  LLMSessionManager.cs             │
                    │  • RequestQuery()                 │
                    │  • Session 생성/조회               │
                    │  • Message History 관리            │
                    │    - System Prompt                │
                    │    - User Messages                │
                    │    - Assistant Messages           │
                    │    - Tool Messages                │
                    │  • SerializeMessages()            │
                    └─────────────────┬─────────────────┘
                                      │
                                      ▼
                    ┌─────────────────────────────────────┐
                    │   LLMRequest 생성                   │
                    │  • requestId                        │
                    │  • sessionId                        │
                    │  • messagesJson (직렬화된 대화 기록) │
                    │  • toolsJson (도구 정의)            │
                    │  • onCompleted (콜백 등록)          │
                    └─────────────────┬───────────────────┘
                                      │
┌─────────────────────────────────────────────────────────────────────────────┐
│                      4. LLM 추론 관리 레이어                                  │
└─────────────────────────────────────────────────────────────────────────────┘
                                      │
                    ┌─────────────────┴─────────────────┐
                    │  LLMInferenceManager.cs           │
                    │  • RequestInference()             │
                    │  • _requestQueue에 요청 추가       │
                    │  • _pipeline (C++ DLL 연결)       │
                    └─────────────────┬─────────────────┘
                                      │
                                      ▼
                    ┌─────────────────────────────────────┐
                    │   LLMRequestQueue                   │
                    │  • ConcurrentQueue                  │
                    │  • Thread-Safe Enqueue/Dequeue     │
                    └─────────────────┬───────────────────┘
                                      │
┌─────────────────────────────────────────────────────────────────────────────┐
│                   5. 워커 스레드 레이어 (백그라운드)                           │
└─────────────────────────────────────────────────────────────────────────────┘
                                      │
                    ┌─────────────────┴─────────────────┐
                    │  LLMWorkerThread.cs               │
                    │  • ThreadLoop() - 백그라운드 실행  │
                    │  • Queue 모니터링                  │
                    │  • DLL 함수 호출 준비              │
                    └─────────────────┬─────────────────┘
                                      │
                                      ▼
                    ┌─────────────────────────────────────────┐
                    │   P/Invoke (C# ↔ C++)                  │
                    │  • OV_Inference(pipeline, messagesJson, │
                    │                 toolsJson)              │
                    │  • IntPtr → C++ 함수 호출              │
                    └─────────────────┬─────────────────────┘
                                      │
┌─────────────────────────────────────────────────────────────────────────────┐
│                    6. C++ DLL 추론 레이어 (Native)                            │
└─────────────────────────────────────────────────────────────────────────────┘
                                      │
                    ┌─────────────────┴─────────────────────┐
                    │  ov_llm_export.cpp                    │
                    │  • OV_Inference()                     │
                    │  • CreateChatHistoryFromJson()        │
                    │    - messagesJson 파싱                │
                    │    - toolsJson 파싱                   │
                    │  • ChatHistory 생성                   │
                    └─────────────────┬───────────────────┘
                                      │
                                      ▼
                    ┌─────────────────────────────────────────┐
                    │   OpenVINO GenAI Pipeline               │
                    │  • ov::genai::LLMPipeline              │
                    │  • generate() 실행                      │
                    │  • Tool Call 지원                       │
                    │  • 모델 추론 (CPU/GPU)                  │
                    └─────────────────┬─────────────────────┘
                                      │
                                      ▼
                    ┌─────────────────────────────────────────┐
                    │   생성된 텍스트 반환                     │
                    │  • string result                        │
                    │  • dup_string() - C++ heap 할당        │
                    │  • IntPtr → Marshal.PtrToStringUTF8    │
                    └─────────────────┬─────────────────────┘
                                      │
┌─────────────────────────────────────────────────────────────────────────────┐
│                    7. 응답 처리 레이어 (메인 스레드)                          │
└─────────────────────────────────────────────────────────────────────────────┘
                                      │
                    ┌─────────────────┴─────────────────┐
                    │  LLMWorkerThread.cs               │
                    │  • OV_FreeString() - 메모리 해제   │
                    │  • LLMOutput 생성                  │
                    │  • _dispatchResponse() 호출        │
                    └─────────────────┬─────────────────┘
                                      │
                                      ▼
                    ┌─────────────────────────────────────┐
                    │  LLMInferenceManager.cs             │
                    │  • _mainThreadActions.Enqueue()     │
                    │  • LateUpdate()에서 처리             │
                    │  • OnWorkerCompleted()              │
                    └─────────────────┬───────────────────┘
                                      │
                                      ▼
                    ┌─────────────────────────────────────┐
                    │  LLMResponse 생성                   │
                    │  • requestId                        │
                    │  • sessionId                        │
                    │  • LLMOutput (text, elapsed)        │
                    └─────────────────┬───────────────────┘
                                      │
                                      ▼
                    ┌─────────────────────────────────────┐
                    │  request.onCompleted() 콜백 실행     │
                    └─────────────────┬───────────────────┘
                                      │
┌─────────────────────────────────────────────────────────────────────────────┐
│                    8. Tool Call 처리 레이어                                   │
└─────────────────────────────────────────────────────────────────────────────┘
                                      │
                    ┌─────────────────┴─────────────────────┐
                    │  LLMSessionManager.cs                 │
                    │  • HandleLLMResponse()                │
                    │  • Session에 Assistant 메시지 추가     │
                    │  • <tool_call> 태그 검사              │
                    └─────────────────┬─────────────────────┘
                                      │
                          ┌───────────┴──────────┐
                          │                      │
                    [Tool Call]            [일반 응답]
                          │                      │
                          ▼                      ▼
    ┌───────────────────────────────────┐  ┌──────────────────────────┐
    │  • ExtractToolCallBlock()         │  │ EventLLMResult?.Invoke() │
    │  • ParseToolCall()                │  │ (LLMResult.Success)      │
    │  • ToolCallJson 생성               │  │                          │
    │    - name                         │  └──────────┬───────────────┘
    │    - arguments                    │             │
    └───────────────┬───────────────────┘             │
                    │                                 │
                    ▼                                 │
    ┌───────────────────────────────────┐             │
    │  ToolExecutor.Execute()           │             │
    │  • APCEventTrigger 내부            │             │
    │  • CreatePlayerHealthPointTrigger │             │
    │    - PlayerHealthPointTrigger 생성 │             │
    │    - triggerContainer에 저장       │             │
    │    - 조건: HP <= threshold        │             │
    │    - 스킬: SHIELD, INVINCIBLE     │             │
    └───────────────┬───────────────────┘             │
                    │                                 │
                    ▼                                 │
    ┌───────────────────────────────────┐             │
    │  ToolCallOutput 생성               │             │
    │  • result: Success/Failed         │             │
    │  • output: Dictionary             │             │
    └───────────────┬───────────────────┘             │
                    │                                 │
                    ▼                                 │
    ┌───────────────────────────────────┐             │
    │  사전 정의된 메시지 선택             │             │
    │  • predefinedMessageWhenSuccess   │             │
    │  • predefinedMessageWhenFailed    │             │
    │  • predefinedMessageWhenTooluse   │             │
    │  • RandomUtils.SampleArray()      │             │
    └───────────────┬───────────────────┘             │
                    │                                 │
                    ▼                                 │
    ┌───────────────────────────────────┐             │
    │  Session에 Tool 메시지 추가        │             │
    │  Session에 최종 응답 추가          │             │
    └───────────────┬───────────────────┘             │
                    │                                 │
                    └─────────────┬───────────────────┘
                                  │
┌─────────────────────────────────────────────────────────────────────────────┐
│                      9. APC 행동 결정 레이어                                  │
└─────────────────────────────────────────────────────────────────────────────┘
                                  │
                    ┌─────────────┴────────────────┐
                    │  APCRouter.cs                │
                    │  • Update() - 매 프레임 실행  │
                    │  • triggerContainer 순회      │
                    └─────────────┬────────────────┘
                                  │
                                  ▼
    ┌───────────────────────────────────────────────┐
    │  각 Trigger의 Evaluate() 호출                  │
    │  • PlayerHealthPointTrigger                   │
    │    - currentHP / maxHP <= threshold 검사      │
    │    - Timer (Cooldown) 검사                    │
    │    - 조건 만족 시 true 반환                    │
    └───────────────┬───────────────────────────────┘
                    │
                    ▼ [조건 만족]
    ┌───────────────────────────────────────────────┐
    │  APCRouter.cs                                 │
    │  • eventTrigger.ApplyCooldown()               │
    │  • switch (eventTrigger.state)                │
    │    - case APCState.SHIELD:                    │
    │      EventShield?.Invoke(duration)            │
    │    - case APCState.INVINCIBLE:                │
    │      (해당 이벤트 실행)                        │
    └───────────────┬───────────────────────────────┘
                    │
                    ▼
    ┌───────────────────────────────────────────────┐
    │  실제 게임 로직 실행                           │
    │  • 플레이어에게 실드 부여                      │
    │  • 무적 상태 적용                             │
    │  • 스킬 이펙트 표시                           │
    └───────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────────────┐
│                         10. UI 출력 레이어                                    │
└─────────────────────────────────────────────────────────────────────────────┘
                                  │
        ┌─────────────────────────┴───────────────────────────┐
        │                                                     │
        ▼                                                     ▼
┌──────────────────────┐                      ┌──────────────────────────┐
│ 즉시 표시 (User)      │                      │ LLM 응답 표시 (APC)       │
│ InputChat.cs         │                      │ APCEventTrigger.cs       │
│ • chatPanel.         │                      │ • ListenerLLMResponse()  │
│   SetMessageToUI()   │                      │ • APCChatManager.        │
│                      │                      │   SendToUI()             │
└──────────┬───────────┘                      └──────────┬───────────────┘
           │                                             │
           └─────────────────┬───────────────────────────┘
                             │
                             ▼
            ┌────────────────────────────────┐
            │  APCChatManager.cs             │
            │  • SendToUI()                  │
            │  • SendToChatEmitterUI()       │
            │  • SendToChatPannelUI()        │
            └────────────────┬───────────────┘
                             │
                             ▼
            ┌────────────────────────────────┐
            │  ChatDisposer.cs               │
            │  • Display()                   │
            │  • DisplayToChatEmitter()      │
            │  • DisplayToChatPannel()       │
            └────────────────┬───────────────┘
                             │
                ┌────────────┴────────────┐
                │                         │
                ▼                         ▼
    ┌───────────────────┐    ┌───────────────────────┐
    │ ChatEmitter.cs    │    │ ChatPanel.cs          │
    │ • SetChat()       │    │ • SetMessageToUI()    │
    │ • 말풍선 표시      │    │ • CreateChatBubbles() │
    │ • APC 위에 표시   │    │ • 채팅 패널에 기록     │
    └───────────────────┘    └───────────────────────┘
```

## 상세 데이터 흐름

### 1단계: 사용자 입력
```
User Input → InputChat.textArea → Trim → userQuery (string)
                                        → sessionId (GUID)
```

### 2단계: 세션 메시지 구조
```json
{
  "sessionId": "abc123...",
  "messages": [
    {"role": "system", "content": "You are a helpful assistant..."},
    {"role": "user", "content": "체력이 30% 이하일 때 실드 걸어줘"},
    {"role": "assistant", "content": "<tool_call>\n{\"name\":\"CreatePlayerHealthPointTrigger\", \"arguments\":{\"skill\":\"SHIELD\",\"threshold\":0.3}}\n</tool_call>"},
    {"role": "tool", "content": "{\"name\":\"CreatePlayerHealthPointTrigger\",\"result\":\"Success\",\"output\":{\"threshold\":0.3}}"},
    {"role": "assistant", "content": "알겠어! 체력 30% 이하일 때 실드를 발동시킬게!"}
  ]
}
```

### 3단계: C++ DLL 호출 구조
```cpp
// Unity (C#)
IntPtr resultPtr = OV_Inference(_pipeline, messagesJson, toolsJson);
string result = Marshal.PtrToStringUTF8(resultPtr);
OV_FreeString(resultPtr);

// C++ DLL
const char* OV_Inference(void* pipelinePtr, 
                         const char* messagesJson, 
                         const char* toolsJson) {
    ChatHistory chatHistory = CreateChatHistoryFromJson(...);
    std::string result = pipeline->generate(chatHistory, ...);
    return dup_string(result);  // C++ heap allocation
}
```

### 4단계: Tool Call 실행
```csharp
// Tool JSON 정의
{
  "type": "function",
  "function": {
    "name": "CreatePlayerHealthPointTrigger",
    "parameters": {
      "skill": "SHIELD",
      "threshold": 0.3
    }
  }
}

// C# 함수 실행
CreatePlayerHealthPointTrigger("SHIELD", 0.3f)
  → PlayerHealthPointTrigger 생성
  → triggerContainer["PlayerHealthPointTrigger&SHIELD"] = trigger
```

### 5단계: Trigger 평가 및 실행
```csharp
// 매 프레임 (Update)
if (currentHP / maxHP <= 0.3 && Timer <= 0) {
    EventShield?.Invoke(duration);  // 실드 스킬 발동
    Timer = cooldown;  // 쿨다운 시작
}
```

## 핵심 컴포넌트 역할

| 컴포넌트 | 역할 | 위치 |
|---------|------|------|
| **InputChat** | 사용자 입력 수집 및 전송 | UI Layer |
| **APCEventTrigger** | Tool 등록, Tool Call 처리, Trigger 관리 | APC Layer |
| **LLMSessionManager** | 대화 히스토리 관리, 응답 처리, Tool Call 파싱 | LLM Layer |
| **LLMInferenceManager** | DLL 초기화, 요청 큐 관리, 메인 스레드 동기화 | LLM Layer |
| **LLMWorkerThread** | 백그라운드 추론 실행, DLL 호출 | Worker Layer |
| **ov_llm_export.cpp** | OpenVINO GenAI 인터페이스, JSON 파싱 | C++ DLL |
| **APCRouter** | Trigger 평가 및 스킬 발동 | APC Layer |
| **ChatDisposer** | UI 출력 분배 (말풍선/패널) | UI Layer |

## 스레드 구조

```
┌────────────────────────────────────────────────────────────┐
│                      Main Thread (Unity)                   │
│  • UI 입력 처리                                             │
│  • LateUpdate()에서 _mainThreadActions 실행                │
│  • Trigger 평가 (Update)                                   │
│  • UI 업데이트                                              │
└────────────────────────────────────────────────────────────┘
                            ↕
            (ConcurrentQueue + _mainThreadActions)
                            ↕
┌────────────────────────────────────────────────────────────┐
│                   Worker Thread (Background)               │
│  • _requestQueue 모니터링                                   │
│  • OV_Inference() 호출 (Blocking)                          │
│  • 결과를 _dispatchResponse로 메인 스레드에 전달            │
└────────────────────────────────────────────────────────────┘
```

## 주요 이벤트 흐름

```
EventModelReady (Unity Event)
  → InputChat: sendButton.interactable = true
  → APCEventTrigger: "시스템 초기화가 완료되었어..."

EventLLMResult (Unity Event)
  → InputChat: IsRunning = false, sendButton.interactable = true
  → APCEventTrigger: APCChatManager.SendToUI()

EventShield (Unity Event)
  → PlayerBehavior: 실드 적용 로직
```

## 에러 처리 및 안전장치

1. **스레드 안전성**: `ConcurrentQueue<LLMRequest>` 사용
2. **메모리 관리**: `OV_FreeString()`으로 C++ heap 해제
3. **상태 관리**: `SessionState` (Idle/Running)으로 중복 요청 방지
4. **Cooldown**: Trigger별 재실행 대기 시간 관리
5. **예외 처리**: try-catch로 DLL 호출 실패 처리
6. **널 체크**: sessionId, Pipeline, ToolExecutor 등 검증

## 성능 최적화

- **비동기 처리**: Worker Thread로 UI 블로킹 방지
- **배치 처리**: LateUpdate에서 _mainThreadActions 일괄 처리
- **메시지 캐싱**: Session별 대화 히스토리 메모리 관리
- **조건부 로깅**: `Logger.DEBUG` 플래그로 성능 측정 선택적 활성화
