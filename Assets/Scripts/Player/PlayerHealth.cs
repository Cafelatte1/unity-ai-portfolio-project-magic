using UnityEngine;

public class PlayerHealth : HealthSystem
{
    APCRouter apcRouter;
    
    protected override void Start()
    {
        base.Start();

        apcRouter = FindFirstObjectByType<APCRouter>();
        apcRouter.EventShield.AddListener(ListenerActiveShield);
    }

    void ListenerActiveShield(float duration)
    {
        invincibleTimer = Mathf.Max(duration, invincibleTimer);
    }
}
