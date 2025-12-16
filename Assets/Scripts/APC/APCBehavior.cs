using System;
using System.Collections.Generic;
using UnityEngine;

public enum APCState
{
    IDLE,
    MOVE,
    SHIELD,
    INVINCIBLE,
    DEATH
}

public class APCBehavior : MonoBehaviour
{
    [SerializeField] Transform followTarget;
    [SerializeField] Vector2 followPosAdjust;
    [SerializeField] Vector2 shieldPosAdjust;
    [SerializeField] float minShieldDuration;
    [SerializeField] float shieldFollowSpeedMultiplier;
    [SerializeField] PlayerBehavior playerBehavior;
    [SerializeField] LayerMask layerGround;
    [SerializeField] LayerMask layerEnemy;
    [SerializeField] GameObject VFXShield;
    CharacterStats stats;
    Dictionary<APCState, FSMNode> stateContainer;
    APCState currentState;
    Queue<APCState> bufferState = new Queue<APCState>();
    Animator animator;
    Dictionary<APCState, string> animatorParamMapper;
    APCRouter apcRouter;
    float _hitRecoveryTimer;
    float _shieldDuration;
    Transform _snapShotFollowTarget;

    void Awake()
    {
        stats = GetComponentInChildren<StatsContainer>().stats;
        animator = GetComponentInChildren<Animator>();
        animatorParamMapper = new Dictionary<APCState, string>
        {
            { APCState.MOVE, "1_Move" },
            { APCState.SHIELD, "2_Shield" },
            { APCState.DEATH, "isDeath" },
        };
        apcRouter = GetComponent<APCRouter>();
        BuildStateController();;
    }

    void Start()
    {
        apcRouter.EventShield.AddListener(ListenerActiveShield);
    }

    void Update()
    {
        if (followTarget == null) return;

        TickUpdate();
        // Logger.Write($"set follow target pos / _snapShotFollowTargetPos={_snapShotFollowTargetPos}");        
    }

    void TickUpdate()
    {
        if (_hitRecoveryTimer > 0) _hitRecoveryTimer -= Time.deltaTime;
        if (_shieldDuration > 0) _shieldDuration -= Time.deltaTime;
    }

    void LateUpdate()
    {
        if (followTarget == null) return;

        _snapShotFollowTarget = followTarget.transform;
        UpdateState();
    }

    // Apply X state changes
    void UpdateState()
    {
        // first handle buffer states
        while (bufferState.Count > 0)
        {
            var newState = bufferState.Dequeue();
            ChangeState(ref currentState, newState);
            if (currentState == APCState.DEATH) return;
        }
        var fromState = currentState;
        switch (currentState)
        {
            case APCState.DEATH:
                break;
            case APCState.SHIELD:
                if (_shieldDuration <= 0)
                    ChangeState(ref currentState, APCState.IDLE);
                break;
            case APCState.MOVE:
                if (Vector3.Distance(GetFollowTargetPos(_snapShotFollowTarget), transform.position) <= 1e-1)
                    ChangeState(ref currentState, APCState.IDLE);
                break;
            case APCState.IDLE:
                if (Vector3.Distance(GetFollowTargetPos(_snapShotFollowTarget), transform.position) > 1e-1)
                    ChangeState(ref currentState, APCState.MOVE);
                break;
            default:
                break;
        }
        // run update logic
        stateContainer[currentState].Update();
#if UNITY_EDITOR
        if (Logger.DEBUG)
        {
            if (fromState != currentState)
                Logger.Write($"APC State changed from={fromState.ToString()} to={currentState.ToString()}");
        }
#endif
    }

    void ChangeState(ref APCState currentState, APCState newState)
    {
        if (currentState == newState) return;
        
        stateContainer[currentState].OnExit();
        currentState = newState;
        stateContainer[currentState].OnEnter();
    }

    Vector3 GetFollowTargetPos(Transform target)
    {
        Vector3 targetPos = target.transform.position;
        targetPos.x += followPosAdjust.x * Mathf.Sign(target.localScale.x);
        targetPos.y += followPosAdjust.y;
        return targetPos;
    }
    
    void ListenerActiveShield(float duration)
    {
        bufferState.Enqueue(APCState.SHIELD);
        _shieldDuration = Mathf.Max(duration, minShieldDuration);
    }

    void BuildStateController()
    {
        stateContainer = new Dictionary<APCState, FSMNode>
        {
            {APCState.SHIELD, new StateShield(this)},
            {APCState.MOVE, new StateMove(this)},
            {APCState.IDLE, new StateIdle(this)},
        };
        currentState = APCState.IDLE;
        stateContainer[currentState].OnEnter();
    }

    class StateShield : FSMNode
    {
        APCBehavior ctx;
        Vector3 followPos;

        public StateShield(APCBehavior ctx) {
            this.ctx = ctx;
        }

        public override void OnEnter()
        {
            ctx.VFXShield.SetActive(true);
            ctx.animator.SetBool(ctx.animatorParamMapper[APCState.SHIELD], true);
            ctx.animator.Update(0f);
        }

        public override void Update()
        {
            followPos = ctx._snapShotFollowTarget.position;
            followPos.x += ctx.shieldPosAdjust.x * Mathf.Sign(ctx._snapShotFollowTarget.localScale.x);
            followPos.y += ctx.shieldPosAdjust.y;
            FlipDirection();
            UpdatePosition();
        }

        public override void OnExit()
        {
            ctx.VFXShield.SetActive(false);
            ctx.animator.SetBool(ctx.animatorParamMapper[APCState.SHIELD], false);
        }

        private void FlipDirection()
        {
            Vector3 scale = ctx.transform.localScale;
            scale.x = Mathf.Sign(ctx.followTarget.localScale.x) != Mathf.Sign(ctx.transform.localScale.x) ?
                -ctx.transform.localScale.x :
                ctx.transform.localScale.x;
            ctx.transform.localScale = scale;
        }

        private void UpdatePosition()
        {
            ctx.transform.position = Vector3.Lerp(
                ctx.transform.position, followPos,
                ctx.stats.moveSpeed * ctx.shieldFollowSpeedMultiplier * Time.deltaTime
            );
        }
    }

    class StateMove : FSMNode
    {
        APCBehavior ctx;
        Vector3 followPos;

        public StateMove(APCBehavior ctx) {
            this.ctx = ctx;
        }

        public override void OnEnter()
        {
            ctx.animator.SetBool(ctx.animatorParamMapper[APCState.MOVE], true);
        }

        public override void Update()
        {
            followPos = ctx._snapShotFollowTarget.position;
            followPos.x += ctx.followPosAdjust.x * Mathf.Sign(ctx._snapShotFollowTarget.localScale.x);
            followPos.y += ctx.followPosAdjust.y;
            FlipDirection();
            if (ctx._hitRecoveryTimer <= 0) UpdatePosition();
        }

        public override void OnExit()
        {
        }

        private void FlipDirection()
        {
            Vector3 scale = ctx.transform.localScale;
            scale.x = (followPos.x - ctx.transform.position.x) < 0 ?
                Mathf.Abs(scale.x) :
                -Mathf.Abs(scale.x);
            ctx.transform.localScale = scale;
        }

        private void UpdatePosition()
        {
            ctx.transform.position = Vector3.Lerp(
                ctx.transform.position, followPos,
                ctx.stats.moveSpeed * Time.deltaTime
            );
        }
    }

    class StateIdle : FSMNode
    {
        APCBehavior ctx;

        public StateIdle(APCBehavior ctx) {
            this.ctx = ctx;
        }

        public override void OnEnter()
        {  
            FlipDirection();
            ctx.animator.SetBool(ctx.animatorParamMapper[APCState.MOVE], false);
        }

        public override void Update()
        {
        }

        public override void OnExit()
        {
        }

        private void FlipDirection()
        {
            Vector3 scale = ctx.transform.localScale;
            scale.x = Mathf.Sign(ctx.followTarget.localScale.x) != Mathf.Sign(ctx.transform.localScale.x) ?
                -ctx.transform.localScale.x :
                ctx.transform.localScale.x;
            ctx.transform.localScale = scale;
        }
    }
}
