using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerBehavior : MonoBehaviour
{
    [Header("Stats")]
    [SerializeField] float landingVelocityMargin;
    [SerializeField] LayerMask layerGround;
    [SerializeField] LayerMask layerEnemy;
    [SerializeField] BoxCollider2D colliderGround;
    [SerializeField] float hitRecoveryCooldown;
    CharacterStats stats;
    Rigidbody2D rb;
    AnimationController animController;
    HealthSystem healthSystem;
    public bool IsAlive
    {
        get { return healthSystem != null && healthSystem.IsAlive; }
    }
    AttackSystem attackSystem;
    Dictionary<PlayerState, FSMNode> stateContainer;
    PlayerState currentXState;
    PlayerState currentYState;
    Queue<PlayerState> bufferXState = new Queue<PlayerState>();
    Queue<PlayerState> bufferYState = new Queue<PlayerState>();
    UIController uiController;
    float _moveInput;
    bool _jumpInput;
    float _snapShotMoveInput;
    float _snapShotjumpInput;
    Vector2 _hitForce;
    float _hitRecoveryTimer;
    float _attackRecoveryTimer;
    bool _attackRes;

    void Awake()
    {
        stats = GetComponentInChildren<StatsContainer>().stats;
        rb = GetComponentInChildren<Rigidbody2D>();
        healthSystem = GetComponentInChildren<HealthSystem>();
        animController = GetComponent<AnimationController>();
        attackSystem = GetComponent<AttackSystem>();
        BuildStateController();
    }

    void Start()
    {
        uiController = GetComponent<ChatDisposer>()?.uiController;
    }

    void OnEnable()
    {
        EventLoading();
    }

    void OnDisable()
    {
        EventUnloading();
    }

    void FixedUpdate()
    {
        _snapShotMoveInput = _moveInput;
        _snapShotjumpInput = _jumpInput ? 1.0f : 0.0f;
        UpdateYState();
        UpdateXState();
    }

    void Update()
    {
        if (!IsAlive) return;

        TickUpdate();
    }

    void TickUpdate()
    {
        if (_hitRecoveryTimer > 0) _hitRecoveryTimer -= Time.deltaTime;
        if (_attackRecoveryTimer > 0) _attackRecoveryTimer -= Time.deltaTime;
    }

    // Apply X state changes
    void UpdateXState()
    {
        // first handle buffer states
        while (bufferXState.Count > 0)
        {
            if (currentXState == PlayerState.DEATH) return;
            var newState = bufferXState.Dequeue();
            ChangeState(ref currentXState, newState);
        }
        var fromXState = currentXState;
        switch (currentXState)
        {
            case PlayerState.DEATH:
                break;
            case PlayerState.DAMAGED:
                ChangeState(ref currentXState, PlayerState.IDLE);
                break;
            case PlayerState.ATTACK:
                if (_attackRecoveryTimer <= 0)
                    ChangeState(ref currentXState, PlayerState.IDLE);
                break;
            case PlayerState.MOVE:
                if (Mathf.Abs(_snapShotMoveInput) <= Mathf.Epsilon)
                    ChangeState(ref currentXState, PlayerState.IDLE);
                break;
            case PlayerState.IDLE:
                if (Mathf.Abs(_snapShotMoveInput) > Mathf.Epsilon)
                    ChangeState(ref currentXState, PlayerState.MOVE);
                break;
            default:
                break;
        }
        // run update logic
        stateContainer[currentXState].Update();
#if UNITY_EDITOR
        if (Logger.DEBUG)
        {
            if (fromXState != currentXState)
                Logger.Write($"XState changed from={fromXState.ToString()} to={currentXState.ToString()}");
        }
#endif
    }

    // Apply Y state changes
    void UpdateYState()
    {
        // first handle buffer states
        while (bufferYState.Count > 0)
        {
            if (currentYState == PlayerState.DEATH) return;
            var newState = bufferYState.Dequeue();
            ChangeState(ref currentYState, newState);
        }
        var fromYState = currentYState;
        switch (currentYState)
        {
            case PlayerState.DEATH:
                break;
            case PlayerState.InFlight:
                if ((rb.linearVelocity.y <= (Mathf.Epsilon + landingVelocityMargin)) && CheckGrounded())
                    ChangeState(ref currentYState, PlayerState.ReadyToJump);
                break;
            case PlayerState.ReadyToJump:
                if (_jumpInput && CheckGrounded())
                    ChangeState(ref currentYState, PlayerState.InFlight);
                break;
            default:
                break;
        }
        // run update logic
        stateContainer[currentYState].Update();
#if UNITY_EDITOR
        if (Logger.DEBUG)
        {
            if (fromYState.ToString() != currentYState.ToString())
                Logger.Write($"YState changed from={fromYState.ToString()} to={currentYState.ToString()}");
        }
#endif
    }

    void ChangeState(ref PlayerState currentState, PlayerState newState)
    {
        if (currentState == newState) return;
        
        stateContainer[currentState].OnExit();
        currentState = newState;
        stateContainer[currentState].OnEnter();
    }

    void ListenerHit(Vector2 knockback)
    {
        if (_hitRecoveryTimer > 0) return;

        _hitForce = knockback;
        bufferXState.Enqueue(PlayerState.DAMAGED);
    }
    
    void ListenerDeath()
    {
        bufferXState.Enqueue(PlayerState.DEATH);
        bufferYState.Enqueue(PlayerState.DEATH);  
    }

    void ListenerAttack(int attackIndex)
    {
        if (_attackRecoveryTimer > 0) return;
        if (uiController.chatPanel.IsInteracting) return;

        switch (attackIndex)
        {
            default:
                bufferXState.Enqueue(PlayerState.ATTACK);
                return;
        }
    }

    void EventLoading()
    {
        healthSystem.EventHit.AddListener(ListenerHit);
        healthSystem.EventDeath.AddListener(ListenerDeath);
        attackSystem.EventAttack.AddListener(ListenerAttack);     
    }

    void EventUnloading()
    {
        healthSystem.EventHit.RemoveListener(ListenerHit);
        healthSystem.EventDeath.RemoveListener(ListenerDeath);
        attackSystem.EventAttack.RemoveListener(ListenerAttack);
    }
    
    bool CheckGrounded()
    {
        return colliderGround.IsTouchingLayers(layerGround.value);
    }

    // Inputs
    public void OnMove(InputValue value)
    {
        if (!IsAlive) return;

        _moveInput = value.Get<float>();
    }

    public void OnJump(InputValue value)
    { 
        if (!CanControll()) return;
        
        if (value.isPressed && currentYState != PlayerState.InFlight)
            _jumpInput = true;
    }

    public void OnAttack(InputValue value)
    {
        if (!CanControll()) return;

        _attackRes = attackSystem.ExecuteAttack(0, this.transform, layerEnemy);
    }

    public void OnSkillW(InputValue value)
    {
        if (!CanControll()) return;

        _attackRes = attackSystem.ExecuteAttack(1, this.transform, layerEnemy);
    }
    
    public void OnSkillE(InputValue value)
    {
        if (!CanControll()) return;
        
        _attackRes = attackSystem.ExecuteAttack(2, this.transform, layerEnemy);
    }

    bool CanControll()
    {
        if (!IsAlive) return false;
        if (uiController.chatPanel.IsInteracting) return false;
        return true;
    }
    
    void BuildStateController()
    {
        stateContainer = new Dictionary<PlayerState, FSMNode>
        {
            // X movement
            {PlayerState.DEATH, new StateDeath(this)},
            {PlayerState.DAMAGED, new StateHit(this)},
            {PlayerState.ATTACK, new StateAttack(this)},
            {PlayerState.MOVE, new StateMove(this)},
            {PlayerState.IDLE, new StateIdle(this)},
            // Y movement
            {PlayerState.ReadyToJump, new StateReadyToJump(this)},
            {PlayerState.InFlight, new StateInFlight(this)}
        };
        currentXState = PlayerState.IDLE;
        currentYState = PlayerState.ReadyToJump;
        stateContainer[currentXState].OnEnter();
        stateContainer[currentYState].OnEnter();
    }

    class StateDeath : FSMNode
    {
        PlayerBehavior ctx;

        public StateDeath(PlayerBehavior ctx) {
            this.ctx = ctx;
        }

        public override void OnEnter()
        {
            ctx.rb.linearVelocity = Vector2.zero;
            ctx._jumpInput = false;
            ctx.animController.PlayStateAnimation(PlayerState.DEATH);
        }

        public override void Update()
        {
        }

        public override void OnExit()
        {
        }
    }

    class StateHit : FSMNode
    {
        PlayerBehavior ctx;

        public StateHit(PlayerBehavior ctx) {
            this.ctx = ctx;
        }

        public override void OnEnter()
        {
            ctx.rb.linearVelocity = Vector2.zero;
            ctx.rb.AddForce(ctx._hitForce, ForceMode2D.Impulse);
            ctx._hitRecoveryTimer = ctx.animController.PlayStateAnimation(PlayerState.DAMAGED, rebind: true);
        }

        public override void Update()
        {
        }

        public override void OnExit()
        {
        }
    }

    class StateAttack : FSMNode
    {
        PlayerBehavior ctx;

        public StateAttack(PlayerBehavior ctx) {
            this.ctx = ctx;
        }

        public override void OnEnter()
        {
            if (!ctx._attackRes) return;

            var clipTime = ctx.animController.PlayStateAnimation(PlayerState.ATTACK, rebind: true);
            ctx._attackRecoveryTimer = clipTime;
        }

        public override void Update()
        {
        }

        public override void OnExit()
        {
        }
    }

    class StateMove : FSMNode
    {
        PlayerBehavior ctx;

        public StateMove(PlayerBehavior ctx) {
            this.ctx = ctx;
        }

        public override void OnEnter()
        {
            ctx.animController.PlayStateAnimation(PlayerState.MOVE);
        }

        public override void Update()
        {
            FlipDirection();
            if (ctx._hitRecoveryTimer <= 0) UpdateVelocity();
        }

        public override void OnExit()
        {
        }

        private void FlipDirection()
        {
            Vector3 scale = ctx.transform.localScale;
            scale.x = ctx._snapShotMoveInput < 0 ? Mathf.Abs(scale.x) : -Mathf.Abs(scale.x);
            ctx.transform.localScale = scale;  
        }

        private void UpdateVelocity()
        {
            Vector2 velocity = ctx.rb.linearVelocity;
            velocity.x = ctx._snapShotMoveInput * ctx.stats.moveSpeed;
            ctx.rb.linearVelocity = velocity;   
        }
    }

    class StateIdle : FSMNode
    {
        PlayerBehavior ctx;

        public StateIdle(PlayerBehavior ctx) {
            this.ctx = ctx;
        }

        public override void OnEnter()
        {
            ctx.animController.PlayStateAnimation(PlayerState.IDLE);
        }

        public override void Update()
        {
        }

        public override void OnExit()
        {
        }
    }

    class StateInFlight : FSMNode
    {
        PlayerBehavior ctx;

        public StateInFlight(PlayerBehavior ctx) {
            this.ctx = ctx;
        }

        public override void OnEnter()
        {
            ctx._jumpInput = false;
            Vector2 velocity = ctx.rb.linearVelocity;
            velocity.y = ctx._snapShotjumpInput * ctx.stats.jumpForce;
            ctx.rb.linearVelocity = velocity;   
        }

        public override void Update()
        {
        }

        public override void OnExit()
        {
        }
    }

    class StateReadyToJump : FSMNode
    {
        PlayerBehavior ctx;

        public StateReadyToJump(PlayerBehavior ctx) {
            this.ctx = ctx;
        }

        public override void OnEnter()
        {
        }

        public override void Update()
        {
        }

        public override void OnExit()
        {
        }
    }
}
