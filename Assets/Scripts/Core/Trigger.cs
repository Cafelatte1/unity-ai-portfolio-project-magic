using System;
using System.Collections.Generic;
using UnityEngine;

public abstract class Trigger
{
    public APCState state;
    public ActionData data;
    public abstract bool Evaluate();
    public abstract void TickUpdate(float deltaTime);
    public abstract void ApplyCooldown();
}

public abstract class HealthPointTrigger<T> : Trigger
{
    protected readonly T ctx;

    protected HealthPointTrigger(T ctx, APCState state, ActionData data)
    {
        this.ctx = ctx;
        this.state = state;
        this.data = data;
    }
}
