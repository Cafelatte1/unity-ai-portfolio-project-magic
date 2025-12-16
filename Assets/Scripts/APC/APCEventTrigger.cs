using System;
using System.Collections.Generic;
using UnityEngine;

public class APCEventTrigger : MonoBehaviour
{
    [SerializeField] List<string> debugQuery;
    [SerializeField] HealthSystem playerHealthSystem;
    [SerializeField] float threshold;
    [SerializeField] float actionCooldown;
    [SerializeField] float actionDuration;
    [SerializeField] string[] predefinedMessageWhenSuccess;
    [SerializeField] string[] predefinedMessageWhenFailed;
    [SerializeField] string[] predefinedMessageWhenTooluse;
    APCChatManager chatMgr;
    public Dictionary<string, Trigger> triggerContainer { get; private set; }
    string toolsJson;
    ToolExecutor apcToolExecutor;
    Dictionary<APCState, ActionData> actionDatas;
    
    void Awake()
    {
        chatMgr = GetComponent<APCChatManager>();
        triggerContainer = new Dictionary<string, Trigger>();
        toolsJson = BuildToolsJson();
        apcToolExecutor = new ToolExecutor(
            predefinedMessageWhenSuccess,
            predefinedMessageWhenFailed,
            predefinedMessageWhenTooluse
        );
        apcToolExecutor.Add("CreatePlayerHealthPointTrigger", (Func<string, float, ToolCallOutput>)CreatePlayerHealthPointTrigger);
    }

    void Start()
    {
        LLMInferenceManager.Instance.EventModelReady.AddListener(ListenerModelReady);
        LLMSessionManager.Instance.EventLLMResult.AddListener(ListenerLLMResponse);
        actionDatas = new Dictionary<APCState, ActionData>();
        foreach (var data in GetComponentInChildren<ActionsContainer>().actionDatas)
        {
            actionDatas[data.state] = data;
        }
    }

    void Update()
    { 
        if (LLMInferenceManager.Instance.IsInitializing) chatMgr.SendToChatEmitterUI("시스템을 초기화하고 있어 기다려 !");
         
        TickUpdate();
    }

    void TickUpdate()
    {
        foreach (var eventTrigger in triggerContainer.Values)
        {
            eventTrigger.TickUpdate(Time.deltaTime);
        }
    }

    public bool ReceiveUserChat(string sessionId, string userQuery)
    {
        var result = LLMSessionManager.Instance.RequestQuery(sessionId, userQuery, QueryType.User, toolsJson, apcToolExecutor);
        if (!result)
            chatMgr.SendToChatEmitterUI("시스템이 망가진것 같아... 개발자를 불러야해 !" + (Logger.DEBUG ? $" / userQuery={userQuery}" : ""));
        return result;
    }

    void ListenerModelReady()
    {
        var returnMsg = "시스템 초기화가 완료되었어 이제 명령을 내려줘 !";
        chatMgr.SendToChatEmitterUI(returnMsg);
    }
    
    void ListenerLLMResponse(LLMResult llmResult, string returnMsg)
    {
        Logger.Write($"APC listene llm response / result={llmResult}, msg={returnMsg}");
        if (llmResult == LLMResult.Success)
        {
            chatMgr.SendToUI(returnMsg);
        }
    }
    
    string BuildToolsJson()
    {
        return @"
[
  {
    ""type"": ""function"",
    ""function"": {
      ""name"": ""CreatePlayerHealthPointTrigger"",
      ""description"": ""Create a trigger that casts a skill when player HP is below a threshold."",
      ""parameters"": {
        ""type"": ""object"",
        ""properties"": {
          ""skill"": {
            ""type"": ""string"",
            ""enum"": [""SHIELD"", ""INVINCIBLE""],
            ""description"": ""executed skill type when player HP <= threshold""
          },
          ""threshold"": {
            ""type"": ""number"",
            ""description"": ""HP ratio between 0 and 1. For example, 0.3 means HP <= 30%.""
          }
        },
        ""required"": [""skill"", ""threshold""]
      }
    }
  }
]
";
    }

    public ToolCallOutput CreatePlayerHealthPointTrigger(string skill, float threshold)
    {
        skill = skill.ToUpper();
        if (Enum.TryParse<APCState>(skill, out APCState state))
        {
            if (actionDatas.TryGetValue(state, out ActionData data))
            {
                // add random noise
                var applyThreshold = threshold + RandomUtils.RandomNormal(data.adjustNoiseMean, data.adjustNoiseStd);
                var trigger = new PlayerHealthPointTrigger(
                    playerHealthSystem, state, data, applyThreshold
                );
                var triggerKey = $"{typeof(PlayerHealthPointTrigger).Name}&{skill}";
                if (triggerContainer.ContainsKey(triggerKey)) Logger.Write($"trigger already exists; update data / triggerKey={triggerKey}");
                triggerContainer[$"{typeof(PlayerHealthPointTrigger).Name}&{skill}"] = trigger;
                Logger.Write($"Trigger created / triggerKey={triggerKey}, skill={skill}, threshold={threshold}, applyThreshold={applyThreshold}");
                var output = new Dictionary<string, object>()
                {
                    { "threshold", threshold },
                };
                var toolCallOutput = new ToolCallOutput(ToolCallResult.Success, output);
                return toolCallOutput;
            }
            else
            {
                Logger.Write($"action data not found / skill={skill}, keys={actionDatas.Keys}");
                var output = new Dictionary<string, object>();
                var toolCallOutput = new ToolCallOutput(ToolCallResult.Failed, output);
                return toolCallOutput;
            }
        }
        else
        {
            Logger.Write($"state not found / skill={skill}");
            var output = new Dictionary<string, object>();
            var toolCallOutput = new ToolCallOutput(ToolCallResult.Failed, output);
            return toolCallOutput;
        }
    }

    public class PlayerHealthPointTrigger : HealthPointTrigger<HealthSystem>
    {
        float threshold;
        public float Timer { get; private set; }

        public PlayerHealthPointTrigger(HealthSystem healthsystem, APCState state, ActionData data, float threshold) : base(healthsystem, state, data)
        {
            if (threshold < 0 || threshold > 1) Logger.Write($"threshold must be in 0-1; force to clamp value / threshold={threshold}", "WARNING");
            this.threshold = Mathf.Clamp01(threshold);
        }

        public override bool Evaluate()
        {
            if (Timer > 0)
            {
                Logger.Write($"trigger eval; timer is remaining / Timer={Timer}");
                return false;
            }
            else
            {
                Logger.Write($"trigger eval / currentHP={ctx.CurrentHealth}, maxHP={ctx.MaxHealth}");
                return (ctx.CurrentHealth / ctx.MaxHealth) <= threshold;
            }
        }

        public override void TickUpdate(float deltaTime)
        {
            if (Timer > 0) Timer -= deltaTime;
        }

        public override void ApplyCooldown()
        {
            Timer = data.colldown;
        }
    }
}
