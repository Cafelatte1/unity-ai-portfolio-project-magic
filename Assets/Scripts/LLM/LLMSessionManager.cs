using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SimpleJSON;
using UnityEngine;
using UnityEngine.Events;

public enum QueryType
{
    User,
    Assistant,
    Tool
}

public enum LLMResult
{
    Success,
    Failed,
    Toolcall
}

public class LLMSessionManager : Singleton<LLMSessionManager>
{
    [SerializeField] [TextArea] string systemPrompt;
    public Dictionary<string, Session> sessionContainer { get; private set; }
    public UnityEvent<LLMResult, string> EventLLMResult;

    protected override void Awake()
    {
        base.Awake();

        sessionContainer = new Dictionary<string, Session>();
    }

    public SessionState? GetSessionState(string sessionId)
    {
        if (sessionContainer.TryGetValue(sessionId, out Session session))
        {
            return session.state;
        }
        else
        {
            Logger.Write("session id not found; return null");
            return null;
        }
    }

    public List<Message> GetSessionMessages(string sessionId)
    {
        if (sessionContainer.TryGetValue(sessionId, out Session session))
        {
            return session.messages;
        }
        else
        {
            Logger.Write("session id not found; return null");
            return null;
        }
    }

    public bool RequestQuery(string sessionId, string query, QueryType queryType, string toolsJson = null, ToolExecutor toolExecutor = null)
    {
        if (!LLMInferenceManager.Instance.IsModelReady)
        {
            Logger.Write("Model is not loaded properly; can't request llm inference", "ERROR");
            return false;   
        }

        Session session;
        if (sessionContainer.TryGetValue(sessionId, out session)) { }
        else
        {
            sessionContainer[sessionId] = new Session(systemPrompt);
            session = sessionContainer[sessionId];
        }
        switch (queryType)
        {
            case QueryType.Assistant:
                session.AddAssistant(query);
                break;
            case QueryType.Tool:
                session.AddTool(query);
                break;
            default:
                session.AddUser(query);
                break;
        }

        var messageJson = SerializeMessages(session.messages);
        LLMRequest request = new LLMRequest(sessionId, messageJson, toolsJson);
        request.onCompleted = (response) => { HandleLLMResponse(response, toolExecutor); };
        
        Logger.Write($"history messages / {GetLastMessages(session.messages, 3)}");
        Logger.Write($"request to inference / query={query}, requestId={request.requestId}, sessionId={request.sessionId}");
        session.state = SessionState.Running;
        LLMInferenceManager.Instance.RequestInference(request);
        
        return true;
    }

    public void HandleLLMResponse(LLMResponse response, ToolExecutor toolExecutor)
    {
        string requestId = response.requestId;
        string sessionId = response.sessionId;
        // INFO: now only support text output
        string text = response.output.text;
        uint elpased = response.output.elapsed;
        string returnMsg;
        returnMsg = text;
        Logger.Write($"handling llm response / text={text}, elapsed={elpased}sec, requestId={requestId[..8]}, sessionId={sessionId[..8]}");

        // assistant 메시지를 세션에 추가
        if (sessionContainer.TryGetValue(sessionId, out Session session))
        {
            session.AddAssistant(text);
            if (text.StartsWith("<tool_call>"))
            {
                if (toolExecutor == null)
                {
                    Logger.Write("llm use tool call but ToolExecutor is null; return raw text", "WARNING");
                    EventLLMResult?.Invoke(LLMResult.Success, returnMsg);
                    session.state = SessionState.Idle;
                    return;
                }

                var toolCallBlock = ExtractToolCallBlock(text);
                if (toolCallBlock == null)
                {
                    Logger.Write("tool_call tag found but JSON block invalid", "ERROR");
                    EventLLMResult?.Invoke(LLMResult.Failed, returnMsg);
                    session.state = SessionState.Idle;
                    return;
                }

                // ToDo: tool call output 구조 수정 필요
                // 실행에 실패하면 null이 아니고 result에 failed로 담아서 하면 애초에 obejct로 리턴안해도 되고
                // 그럼 type casting 할 필요도 없음
                var toolCallJson = ParseToolCall(toolCallBlock);
                var toolCallOutput = (ToolCallOutput)toolExecutor.Execute(toolCallJson);
                if (ToolCallResult.Success == toolCallOutput?.result)
                {
                    Logger.Write($"success to execute tool / toolName={toolCallJson.name}");
                    var toolQuery = new Dictionary<string, object>()
                    {
                        { "name", toolCallJson.name },
                        { "result", ToolCallResult.Success.ToString() },
                        { "output", CommonUtils.DictToString(toolCallOutput.output) },
                    };
                    var toolMsg = CommonUtils.DictToString(toolQuery);
                    // 사전 정의된 내용으로 응답
                    if ((toolExecutor.predefinedMessageWhenSuccess?.Length ?? 0) > 0)
                    {
                        Logger.Write("use predifined success message");
                        var msgs = toolExecutor.predefinedMessageWhenSuccess;
                        returnMsg = RandomUtils.SampleArray(msgs, 1).First();
                        session.AddTool(toolMsg);
                        session.AddAssistant(returnMsg);
                        EventLLMResult?.Invoke(LLMResult.Success, returnMsg);
                        session.state = SessionState.Idle;
                        return;
                    }
                    // LLM을 통해 최종 응답
                    else
                    {
                        Logger.Write("Re-request inference with tool call output");
                        RequestQuery(sessionId, toolMsg, QueryType.Tool);
                        if ((toolExecutor.predefinedMessageWhenTooluse?.Length ?? 0) > 0)
                        {
                            var msgs = toolExecutor.predefinedMessageWhenTooluse;
                            returnMsg = RandomUtils.SampleArray(msgs, 1).First();
                        }
                        EventLLMResult?.Invoke(LLMResult.Toolcall, returnMsg);
                        session.state = SessionState.Idle;
                        return;
                    }
                }
                else
                {
                    Logger.Write($"failed to execute tool / toolName={toolCallJson.name}");
                    var toolQuery = new Dictionary<string, object>()
                    {
                        { "name", toolCallJson.name },
                        { "result", ToolCallResult.Failed.ToString() },
                    };
                    var toolMsg = CommonUtils.DictToString(toolQuery);
                    // 사전 정의된 내용으로 응답
                    if ((toolExecutor.predefinedMessageWhenFailed?.Length ?? 0) > 0)
                    {
                        Logger.Write("use predifined failed message");
                        var msgs = toolExecutor.predefinedMessageWhenFailed;
                        returnMsg = RandomUtils.SampleArray(msgs, 1).First();
                        session.AddTool(toolMsg);
                        session.AddAssistant(returnMsg);
                        EventLLMResult?.Invoke(LLMResult.Failed, returnMsg);
                        session.state = SessionState.Idle;
                        return;
                    }
                    else
                    {
                        Logger.Write("tool call failed; do nothing");
                        session.AddTool(toolMsg);
                        EventLLMResult?.Invoke(LLMResult.Failed, returnMsg);
                        session.state = SessionState.Idle;
                        return;  
                    }
                }
            }
            // tool call text가 아닌 일반 생성 text
            else
            {
                EventLLMResult?.Invoke(LLMResult.Success, returnMsg);
                session.state = SessionState.Idle;
                return;  
            }
        }
        else
        {
            Logger.Write("sessionId not found though inference request received");
            EventLLMResult?.Invoke(LLMResult.Failed, returnMsg);
            session.state = SessionState.Idle;
            return;
        }
    }

    string ExtractToolCallBlock(string text)
    {
        const string startTag = "<tool_call>";
        const string endTag = "</tool_call>";

        int start = text.IndexOf(startTag);
        if (start < 0) return null;

        int end = text.IndexOf(endTag, start);
        if (end < 0) return null;

        start += startTag.Length;
        string json = text.Substring(start, end - start).Trim();
        return json;
    }

    ToolCallJson ParseToolCall(string json)
    {
        var node = JSON.Parse(json);
        var result = new ToolCallJson
        {
            name = node["name"],
            arguments = new Dictionary<string, object>()
        };

        var args = node["arguments"].AsObject;
        foreach (var kv in args)
        {
            // 숫자 / 문자열 / bool 자동 변환
            if (kv.Value.IsNumber) result.arguments[kv.Key] = kv.Value.AsFloat;
            else if (kv.Value.IsBoolean) result.arguments[kv.Key] = kv.Value.AsBool;
            else result.arguments[kv.Key] = kv.Value.Value;
        }

        return result;
    }

    string SerializeMessages(List<Message> messages)
    {
        var sb = new StringBuilder();
        sb.Append("[");

        for (int i = 0; i < messages.Count; i++)
        {
            var m = messages[i];
            string role = EscapeJson(m.role ?? "");
            string content = EscapeJson(m.content ?? "");

            sb.Append("{\"role\":\"")
              .Append(role)
              .Append("\",\"content\":\"")
              .Append(content)
              .Append("\"}");

            if (i < messages.Count - 1)
                sb.Append(",");
        }

        sb.Append("]");
        return sb.ToString();
    }

    static string EscapeJson(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";

        return s
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\n", "\\n")
            .Replace("\r", "\\r")
            .Replace("\t", "\\t");
    }

    string GetLastMessages(List<Message> messages, int n)
    {
        string buffer = "";
        int start = Math.Max(0, messages.Count - n);
        for (int i = start; i < messages.Count; i++)
        {
            var m = messages[i];
            buffer += $"[{i}th_Message={m.role}, {m.content}]\n";
        }
        return buffer;
    }
}
