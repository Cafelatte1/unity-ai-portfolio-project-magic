using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class APCRouter : MonoBehaviour
{
    PlayerBehavior playerBehavior;
    APCContextAnalyzer apcContext;
    APCEventTrigger apcEvent;
    public UnityEvent<float> EventShield;

    void Awake()
    {
        apcContext = GetComponent<APCContextAnalyzer>();
        apcEvent = GetComponent<APCEventTrigger>();
    }

    void Start()
    {
        playerBehavior = FindFirstObjectByType<PlayerBehavior>();
    }

    void Update()
    {
        if (!playerBehavior.IsAlive) return;

        foreach (var eventTrigger in apcEvent.triggerContainer.Values)
        {
            if (!eventTrigger.Evaluate()) continue;

            Logger.Write("event trigger is true; action event");
            eventTrigger.ApplyCooldown();
            switch (eventTrigger.state)
            {
                case APCState.SHIELD:
                    EventShield?.Invoke(eventTrigger.data.duration);
                    break;
                default:
                    break;
            }

        }
    }
}