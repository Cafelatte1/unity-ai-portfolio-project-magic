using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InputChat : MonoBehaviour
{
    [SerializeField] TMP_InputField textArea;
	[SerializeField] Button sendButton;
	[SerializeField] bool applyTrimBeforeRequest;
	UIController uiController;
	// check inference process is running
	public bool IsRunning { get; private set; }

	void Awake()
	{
		uiController = GetComponentInParent<UIController>();
		sendButton.interactable = false;
	}
	
    void Start()
    {
		LLMInferenceManager.Instance.EventModelReady.AddListener(ListenerModelReady);
		LLMSessionManager.Instance.EventLLMResult.AddListener(ListenerLLMResponse);
    }

    void OnEnable()
    {
        if (!LLMInferenceManager.Instance.IsModelReady || IsRunning) sendButton.interactable = false;
		else sendButton.interactable = true;
    }

    public void OnValueChanged(string str)
	{
		textArea.text = str;
	}
	
	public void OnClicked()
	{
		sendButton.interactable = false;
		IsRunning = true;
		var userQuery = textArea.text;
		if (applyTrimBeforeRequest) userQuery = userQuery.Trim();
		textArea.text = "";

		if (uiController.playerChatMgr == null)
		{
			Logger.Write("player chat manager is null !", "ERROR");
			return;
		}
		if (uiController.apcEventTrigger == null)
		{
			Logger.Write("apc event trigger is null !", "ERROR");
			return;
		}
		var result = uiController.apcEventTrigger.ReceiveUserChat(uiController.playerChatMgr.sessionId, userQuery);
		if (result) uiController.chatPanel.SetMessageToUI("user", userQuery);	
	}

	void ListenerModelReady()
	{
		sendButton.interactable = true;
	}
    
    void ListenerLLMResponse(LLMResult llmResult, string returnMsg)
    {
        sendButton.interactable = true;
		IsRunning = false;
    }
}