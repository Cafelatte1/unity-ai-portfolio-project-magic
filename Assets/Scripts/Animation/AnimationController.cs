using System;
using System.Collections.Generic;
using UnityEngine;

public enum AnimationSelectType
{
    First,
    Random
}

public class AnimationController : MonoBehaviour
{
    [SerializeField] uint seed = 42;
    [SerializeField] AnimationSelectType selectType = AnimationSelectType.Random;
    public Animator animator;
    SPUM_Prefabs _prefabs;
    Unity.Mathematics.Random rng;
    PlayerState currentState;
    int currentIndex;
    
    void Awake()
    {
        rng = new Unity.Mathematics.Random(seed);
        animator = GetComponentInChildren<Animator>();
        _prefabs = GetComponentInChildren<SPUM_Prefabs>();
    }

    void Start()
    {
        if(!_prefabs.allListsHaveItemsExist()){
            _prefabs.PopulateAnimationLists();
        }   
        _prefabs.OverrideControllerInit();
        currentState = PlayerState.IDLE;
    }

    public float PlayStateAnimation(PlayerState state, bool rebind = false)
    {
        if (state != currentState)
        {
            int index = this.selectType == AnimationSelectType.First ? 0 : GetRandomIndex(state);
            if (rebind) Rebind();
            _prefabs.PlayAnimation(state, index);
            currentState = state;
            currentIndex = index;
            return _prefabs.GetClipTime(state, index);
        }
        return _prefabs.GetClipTime(currentState, currentIndex);
    }

    public float PlayStateAnimation(PlayerState state, int index, bool rebind = false)
    {
        if (state != currentState || index != currentIndex)
        {
            if (rebind) Rebind();
            _prefabs.PlayAnimation(state, index);
            currentState = state;
            currentIndex = index;
            return _prefabs.GetClipTime(state, index);
        }
        return _prefabs.GetClipTime(currentState, currentIndex);
    }

    int GetRandomIndex(PlayerState state)
    {
        var n_clips = _prefabs.GetNumClips(state);
        
        if (n_clips > 0)
        {
            return rng.NextInt(0, n_clips);
        }
        else
        {
            Logger.Write($"not any clips -> return 0 index / state={state.ToString()}", "ERROR");
            return 0;
        }
    }

    public void Rebind()
    {
        animator.Rebind();
        animator.Update(0f);
    }
}
