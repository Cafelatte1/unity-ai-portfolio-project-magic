using UnityEngine;

public class PlayerChatManager : MonoBehaviour
{
    ChatDisposer chatDisposer;
    public string sessionId { get; private set; }

    void Awake()
    {
        chatDisposer = GetComponent<ChatDisposer>();
        sessionId = CommonUtils.GetUUIDstring();
    }

    public void SendToUI(string text)
    {
        chatDisposer.Display(text);
    }

    public void SendToChatEmitterUI(string text)
    {
        chatDisposer.DisplayToChatEmitter(text);
    }

    public void SendToChatPannelUI(string text)
    {
        chatDisposer.DisplayToChatPannel(text);
    }
}
