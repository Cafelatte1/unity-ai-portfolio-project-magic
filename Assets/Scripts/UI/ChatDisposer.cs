using UnityEngine;

public class ChatDisposer : MonoBehaviour
{
    [SerializeField] ChatEmitter chatEmitterPrefab;
    [SerializeField] Vector3 positionoffset;
    public UIController uiController;
    ChatEmitter chatEmitter;

    void Awake()
    {
        uiController = FindFirstObjectByType<UIController>();
    }

    void Start()
    {
        chatEmitter = Instantiate(chatEmitterPrefab, uiController.canvas.transform);
        chatEmitter.Bind(transform, positionoffset);
    }

    public void Display(string text)
    {
        chatEmitter.SetChat(text);
        uiController.chatPanel.SetMessageToUI("assistant", text);
    }
    
    public void DisplayToChatEmitter(string text) => chatEmitter.SetChat(text);

    public void DisplayToChatPannel(string text) => uiController.chatPanel.SetMessageToUI("assistant", text);
}
