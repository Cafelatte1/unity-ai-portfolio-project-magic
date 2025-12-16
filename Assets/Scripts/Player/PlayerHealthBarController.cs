using UnityEngine;
using UnityEngine.UI;

public class PlayerHealthBarController : MonoBehaviour
{
    UIController uiController;
    Slider hpBar;
    HealthSystem playerHealthSystem;

    void Awake()
    {
        playerHealthSystem = GetComponent<HealthSystem>();
    }

    void Start()
    {
        uiController = GetComponentInParent<ChatDisposer>()?.uiController;
        if (uiController != null)
            hpBar = uiController.UIPlayerHealthBar;
    }

    void OnEnable()
    {
        EventLoading();
    }

    void OnDisable()
    {
        EventUnloading();
    }

    void EventLoading()
    {
        if (playerHealthSystem != null)
            playerHealthSystem.OnHealthChanged.AddListener(OnHealthChanged);
    }

    void EventUnloading()
    {
        if (playerHealthSystem != null)
            playerHealthSystem.OnHealthChanged.RemoveListener(OnHealthChanged);
    }

    void OnHealthChanged(float maxHealth, float beforeHealth, float currentHealth)
    {
        if (maxHealth <= 0) return;
        hpBar.value = Mathf.Clamp01(currentHealth / maxHealth);
    }
}
