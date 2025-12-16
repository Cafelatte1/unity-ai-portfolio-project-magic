using System.Collections;
using UnityEngine;

public static class Waiter
{
    public static IEnumerator DelayedAction(System.Action action, float seconds, bool realtime=true)
    {
        if (realtime)
        {
            yield return new WaitForSecondsRealtime(seconds);
        }
        else
        {
            yield return new WaitForSeconds(seconds);
        }
        action?.Invoke();
    }

    public static void DestroyObject(GameObject go, bool deactive=true)
    {
        if (deactive) go.SetActive(false);
        UnityEngine.Object.Destroy(go);
    }
}
