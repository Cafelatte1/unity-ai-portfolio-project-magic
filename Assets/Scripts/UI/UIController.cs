using UnityEngine;
using UnityEngine.UI;

public class UIController : MonoBehaviour
{
    [SerializeField] public Transform canvas;
    [SerializeField] public Slider UIPlayerHealthBar;
    [SerializeField] public ChatPanel chatPanel;
    public PlayerChatManager playerChatMgr;
    public APCEventTrigger apcEventTrigger;

    void Start()
    {
        playerChatMgr = FindFirstObjectByType<PlayerChatManager>();
        apcEventTrigger = FindFirstObjectByType<APCEventTrigger>();    
    }
}
