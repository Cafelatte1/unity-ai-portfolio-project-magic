using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading;
using System.Reflection;

public class Message
{
    public string role;
    public string content;

    public Message(string role, string content)
    {
        this.role = role;
        this.content = content;
    }
}

public enum SessionState
{
    Idle,
    Running
}


public class Session
{
    public SessionState state;
    public List<Message> messages = new List<Message>();

    public Session(string systemPrompt)
    {
        messages.Add(new Message("system", systemPrompt));
    }

    public void AddUser(string text) => messages.Add(new Message("user", text));
    public void AddAssistant(string text) => messages.Add(new Message("assistant", text));
    public void AddTool(string text) => messages.Add(new Message("tool", text));
}

public class LLMRequest
{
    public string requestId { get; private set; }
    public string sessionId { get; private set; }
    public string messagesJson { get; private set; }
    public string toolsJson { get; private set; }
    public Action<LLMResponse> onCompleted;

    public LLMRequest(string sessionId, string messagesJson, string toolsJson)
    {
        this.requestId = CommonUtils.GetUUIDstring();
        this.sessionId = sessionId;
        this.messagesJson = messagesJson;
        this.toolsJson = toolsJson;
    }
}

public class LLMRequestQueue
{
    private readonly ConcurrentQueue<LLMRequest> _queue = new ConcurrentQueue<LLMRequest>();
    private readonly AutoResetEvent _signal = new AutoResetEvent(false);

    public void Enqueue(LLMRequest req)
    {
        _queue.Enqueue(req);
        _signal.Set();
    }

    public bool TryDequeue(out LLMRequest req)
    {
        return _queue.TryDequeue(out req);
    }

    public void WaitForNewItem()
    {
        _signal.WaitOne();
    }
}

public class LLMOutput
{
    public string text { get; private set; }
    public string image { get; private set; }
    public uint elapsed { get; private set; }

    public LLMOutput(string text, string image, float elapsed)
    {
        this.text = text;
        this.image = image;
        this.elapsed = (uint)(elapsed);
    }
}

public class LLMResponse
{
    public string requestId { get; private set; }
    public string sessionId { get; private set; }
    public LLMOutput output { get; private set; }

    public LLMResponse(LLMRequest request, LLMOutput output)
    {
        this.requestId = request.requestId;
        this.sessionId = request.sessionId;
        this.output = output;
    }
}

public class ToolCallJson
{
    public string name;
    public Dictionary<string, object> arguments;
}

public enum ToolCallResult
{
    Success,
    Failed,
}

public class ToolCallOutput
{
    public ToolCallResult result { get; private set; }
    public Dictionary<string, object> output { get; private set; }

    public ToolCallOutput(ToolCallResult result, Dictionary<string, object> output)
    {
        this.result = result;
        this.output = output;
    }
}

public class ToolExecutor
{
    private Dictionary<string, MethodInfo> toolMethods;
    private Dictionary<string, object> toolTargets;
    public string[] predefinedMessageWhenSuccess;
    public string[] predefinedMessageWhenFailed;
    public string[] predefinedMessageWhenTooluse;

    public ToolExecutor(string[] predefinedMessageWhenSuccess = null, string[] predefinedMessageWhenFailed = null, string[] predefinedMessageWhenTooluse = null)
    {
        toolMethods = new Dictionary<string, MethodInfo>();
        toolTargets = new Dictionary<string, object>();
        this.predefinedMessageWhenSuccess = predefinedMessageWhenSuccess;
        this.predefinedMessageWhenFailed = predefinedMessageWhenFailed;
        this.predefinedMessageWhenTooluse = predefinedMessageWhenTooluse;
    }

    public void Add(string toolName, Delegate method)
    {
        toolMethods[toolName] = method.Method;
        toolTargets[toolName] = method.Target;
    }

    public object Execute(ToolCallJson toolCallJson)
    {
        if (!toolMethods.TryGetValue(toolCallJson.name, out MethodInfo method))
        {
            Logger.Write($"[ToolExecutor] Unknown tool: {toolCallJson.name}");
            return null;
        }

        object target = toolTargets[toolCallJson.name];
        ParameterInfo[] parameters = method.GetParameters();
        object[] finalParams = new object[parameters.Length];

        try
        {
            for (int i = 0; i < parameters.Length; i++)
            {
                var p = parameters[i];
                string paramName = p.Name;

                if (!toolCallJson.arguments.TryGetValue(paramName, out object rawValue))
                {
                    Logger.Write($"[ToolExecutor] Missing argument '{paramName}' for tool '{toolCallJson.name}'");
                    return null;
                }

                object converted = ConvertParameter(rawValue, p.ParameterType);
                finalParams[i] = converted;
            }

            object output = method.Invoke(target, finalParams);
            return output;
        }
        catch (Exception e)
        {
            Logger.Write($"[ToolExecutor] Error executing tool / tool={toolCallJson.name}, name={e}");
            return null;
        }
    }

    private object ConvertParameter(object rawValue, Type targetType)
    {
        if (targetType == typeof(float))
            return Convert.ToSingle(rawValue);
        if (targetType == typeof(int))
            return Convert.ToInt32(rawValue);
        if (targetType == typeof(bool))
            return Convert.ToBoolean(rawValue);
        if (targetType == typeof(string))
            return rawValue.ToString();

        return Convert.ChangeType(rawValue, targetType);
    }
}
