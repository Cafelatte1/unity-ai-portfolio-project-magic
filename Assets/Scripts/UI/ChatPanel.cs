using UnityEngine;
using System.Collections.Generic;

public class ChatPanel : MonoBehaviour
{
    [SerializeField] public GameObject chatPanelHistory;
    [SerializeField] GameObject chatBubbleUserPrefab;
    [SerializeField] GameObject chatBubbleAssistantPrefab;
    [SerializeField] Transform ContentRoot;
    UIController uiController;
    public bool IsInteracting => chatPanelHistory.activeSelf;

	void Awake()
	{
		uiController = GetComponentInParent<UIController>();
	}

    public void SetMessageToUI(string role, string text)
    {
        CreateChatBubbles(new Message(role: role, content: text));
    }

    public void SetMessageToUI()
    {
        var sessionId = uiController.playerChatMgr.sessionId;
        var messages = LLMSessionManager.Instance.GetSessionMessages(sessionId);
        if (messages == null)
        {
            Logger.Write($"not found chat history; display nothing / sessionI={sessionId[..8]}", "WARNING");
            return;
        }
        CreateChatBubbles(messages);
    }

    void CreateChatBubbles(Message msg)
    {
        var instance = Instantiate(msg.role == "user" ? chatBubbleUserPrefab : chatBubbleAssistantPrefab, ContentRoot);
        if (instance.TryGetComponent<ChatBubbleSizeHandler>(out ChatBubbleSizeHandler handler))
            handler.Init(msg.content);
        else
            Logger.Write("ChatBubbleSizeHandler component not found; can't adjsut background image for chat bubble", "WARNING");
    }

    void CreateChatBubbles(List<Message> messages)
    {
        if (messages.Count == 0) return;

        messages.Reverse();
        foreach (var msg in messages)
        {
            if (msg.role == "system" || msg.role == "tool") continue;

            var instance = Instantiate(msg.role == "user" ? chatBubbleUserPrefab : chatBubbleAssistantPrefab, ContentRoot);
            if (instance.TryGetComponent<ChatBubbleSizeHandler>(out ChatBubbleSizeHandler handler))
                handler.Init(msg.content);
            else
                Logger.Write("ChatBubbleSizeHandler component not found; can't adjsut background image for chat bubble", "WARNING");
        }
    }

    public void OnClick()
    {
        chatPanelHistory.SetActive(!chatPanelHistory.activeSelf);
    }
}
