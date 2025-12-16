using UnityEngine;

public class APCChatManager : MonoBehaviour
{
    ChatDisposer chatDisposer;

    void Awake()
    {
        chatDisposer = GetComponent<ChatDisposer>();
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
