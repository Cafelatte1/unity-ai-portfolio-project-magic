using UnityEngine;

[CreateAssetMenu(fileName = "New Action Data", menuName = "Game/Action Data")]
public class ActionData : ScriptableObject
{
    [Header("Action Type")]
    [SerializeField] public APCState state;
    [SerializeField] public float colldown;
    [SerializeField] public float duration;
    [SerializeField] public float adjustNoiseMean;
    [SerializeField] public float adjustNoiseStd;
}
