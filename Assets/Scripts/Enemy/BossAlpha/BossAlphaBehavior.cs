using System;
using System.Collections.Generic;
using UnityEngine;

public class BossAlphaBehavior : MonoBehaviour
{
    [Header("Stats")]
    [SerializeField] float patrolRangeDistance;
    [SerializeField] Vector2 wallBoxSize;
    [SerializeField] Vector2 groundBoxSize;
    [SerializeField] float wallBoxDistance;
    [SerializeField] float groundDistance;
    [SerializeField] float YPosAdjust;
    [SerializeField] float groundDetectionYPosAdjust;
    [SerializeField] float flipCooldown;
    [SerializeField] LayerMask layerGround;
    [SerializeField] LayerMask layerPlayer;
    [SerializeField] GameObject colliders;
    [SerializeField] BoxCollider2D colliderGround;
    [SerializeField] float hitRecoveryCooldown;
    [SerializeField] float raycastDetectionMargin;
    CharacterStats stats;
    Rigidbody2D rb;
    AnimationController animController;
    HealthSystem healthSystem;
    AttackSystem attackSystem;
    SelectorNode selectorRoot;
    Dictionary<EnemyBehaviorState, SequenceNode> sequenceContainer;
    BossAlphaFirstContainer firstSkillContainer;
    Vector2 _startPos;
    public bool IsAlive { get; private set; }
    float _flipTimer;
    Vector2 _hitForce;
    float _hitRecoveryTimer;
    float _attackRecoveryTimer;
    bool _isHit;
    Transform _target;

    void Awake()
    {
        stats = GetComponentInChildren<StatsContainer>().stats;
        rb = GetComponentInChildren<Rigidbody2D>();
        healthSystem = GetComponentInChildren<HealthSystem>();
        firstSkillContainer = GetComponentInChildren<BossAlphaFirstContainer>();
        animController = GetComponent<AnimationController>();
        attackSystem = GetComponent<AttackSystem>();
        BuildStateController();
    }

    void Start()
    {
        IsAlive = true;
        _startPos = transform.position;
        healthSystem.EventHit.AddListener(ListenerHit);
        healthSystem.EventDeath.AddListener(ListenerDeath);
        attackSystem.EventAttack.AddListener(ListenerAttack);
    }

    void FixedUpdate()
    {
        var (selectorResult, actionPath) = selectorRoot.Evaluate();
        Logger.Write($"selectorResult={selectorResult}, actionPath={actionPath}");
    }

    void Update()
    {
        TickUpdate();
    }

    void TickUpdate()
    {
        if (_flipTimer > 0) _flipTimer -= Time.deltaTime;
        if (_hitRecoveryTimer > 0) _hitRecoveryTimer -= Time.deltaTime;
        if (_attackRecoveryTimer > 0) _attackRecoveryTimer -= Time.deltaTime;
    }

    // event listeners
    void ListenerHit(Vector2 knockback)
    {
        if (_hitRecoveryTimer > 0) return;
        _hitForce = knockback;
        _isHit = true;
    }
    
    void ListenerDeath()
    {
        if (!IsAlive) return;
        IsAlive = false;
        _isHit = false;
        colliders.layer = LayerMask.NameToLayer(LayerName.Corpse.ToString());
    }

    void ListenerAttack(int attackIndex)
    {
    }

    bool CheckGrounded()
    {
        return colliderGround.IsTouchingLayers(layerGround.value);
    }

    void BuildStateController()
    {
        sequenceContainer = new Dictionary<EnemyBehaviorState, SequenceNode>();

        // death sequence
        sequenceContainer[EnemyBehaviorState.DEATH] = new SequenceNode
        (
            new List<ActionNode>
            {
                new ActionDeath(this),
            },
            EnemyBehaviorState.DEATH.ToString()
        );

        // damaged sequence
        sequenceContainer[EnemyBehaviorState.DAMAGED] = new SequenceNode
        (
            new List<ActionNode>
            {
                new ActionDamaged(this),
            },
            EnemyBehaviorState.DAMAGED.ToString()
        );

        // detection sequence
        sequenceContainer[EnemyBehaviorState.DETECTION] = new SequenceNode
        (
            new List<ActionNode>
            {
                new ActionDetection(this),
            },
            EnemyBehaviorState.DETECTION.ToString()
        );

        // chase sequence
        sequenceContainer[EnemyBehaviorState.CHASE] = new SequenceNode
        (
            new List<ActionNode>
            {
                new ActionChase(this),
            },
            EnemyBehaviorState.CHASE.ToString()
        );

        // attack sequence
        sequenceContainer[EnemyBehaviorState.ATTACK] = new SequenceNode
        (
            new List<ActionNode>
            {
                new ActionAttack(this),
            },
            EnemyBehaviorState.ATTACK.ToString()
        );

        // patrol sequence
        sequenceContainer[EnemyBehaviorState.PATROL] = new SequenceNode
        (
            new List<ActionNode>
            {
                new ActionPatrol(this),
            },
            EnemyBehaviorState.PATROL.ToString()
        );

        // idle sequence
        sequenceContainer[EnemyBehaviorState.IDLE] = new SequenceNode
        (
            new List<ActionNode>
            {
                new ActionIDLE(this),
            },
            EnemyBehaviorState.IDLE.ToString()
        );

        selectorRoot = new SelectorNode(
            new List<SequenceNode>()
            {
                sequenceContainer[EnemyBehaviorState.DEATH],
                sequenceContainer[EnemyBehaviorState.DAMAGED],
                sequenceContainer[EnemyBehaviorState.DETECTION],
                sequenceContainer[EnemyBehaviorState.CHASE],
                sequenceContainer[EnemyBehaviorState.ATTACK],
                sequenceContainer[EnemyBehaviorState.PATROL],
                sequenceContainer[EnemyBehaviorState.IDLE],
            },
            "Root"            
        );
    }

    class ActionDeath : ActionNode
    {
        BossAlphaBehavior ctx;
        bool _isAnimated = false;
        public ActionDeath(BossAlphaBehavior ctx) { this.ctx = ctx; }

        public override EvaluateResult Evaluate()
        {
            if (ctx.IsAlive) return new EvaluateResult(BTNode.State.Failure, this.GetType().ToString());

            Logger.Write($"ActionDeath execute / object={ctx.gameObject.name}");
            if (!_isAnimated)
            {   
                ctx.animController.PlayStateAnimation(PlayerState.DEATH);
                _isAnimated = true;
            }
            return new EvaluateResult(BTNode.State.Success, this.GetType().ToString());
        }
    }

    class ActionDamaged : ActionNode
    {
        BossAlphaBehavior ctx;
        public ActionDamaged(BossAlphaBehavior ctx) { this.ctx = ctx; }

        public override EvaluateResult Evaluate()
        {
            if (!ctx.IsAlive || !ctx._isHit) return new EvaluateResult(BTNode.State.Failure, this.GetType().ToString());

            Logger.Write($"ActionDamaged execute / object={ctx.gameObject.name}");
            ctx.rb.linearVelocity = Vector2.zero;
            ctx.rb.AddForce(ctx._hitForce, ForceMode2D.Impulse);
            ctx._hitRecoveryTimer = ctx.animController.PlayStateAnimation(PlayerState.DAMAGED, rebind: true);
            ctx._isHit = false;
            return new EvaluateResult(BTNode.State.Success, this.GetType().ToString());
        }
    }

    class ActionDetection : ActionNode
    {
        BossAlphaBehavior ctx;
        public ActionDetection(BossAlphaBehavior ctx) { this.ctx = ctx; }

        public override EvaluateResult Evaluate()
        {
            if (ctx._target == null)
            {
                var direction = new Vector2(-ctx.transform.localScale.x, 0f);
                var origin = ctx.transform.position;
                origin.y += ctx.YPosAdjust;

                var hitbox = ctx.wallBoxSize;
                hitbox.y *= ctx.transform.localScale.y;
                RaycastHit2D hitArea = Physics2D.BoxCast(
                    origin,
                    hitbox,
                    0f,
                    direction,
                    ctx.patrolRangeDistance,
                    ctx.layerPlayer
                );
                if (hitArea)
                {
                    ctx._target = hitArea.collider.transform;
                    Logger.Write("detection; raycast hit player");
                }
            }
            return new EvaluateResult(BTNode.State.Failure, this.GetType().ToString());
        }
    }

    class ActionChase : ActionNode
    {
        BossAlphaBehavior ctx;
        public ActionChase(BossAlphaBehavior ctx) { this.ctx = ctx; }

        public override EvaluateResult Evaluate()
        {
            if (ctx._target == null) return new EvaluateResult(BTNode.State.Failure, this.GetType().ToString());
            if (ctx.firstSkillContainer.IsSpawning) return new EvaluateResult(BTNode.State.Running, this.GetType().ToString());

            var direction = Mathf.Sign(ctx._target.position.x - ctx.transform.position.x);
            Logger.Write($"before flip executed / direction={direction}, localScaleX={ctx.transform.localScale.x}");
            if (direction == Mathf.Sign(ctx.transform.localScale.x))
            {
                Logger.Write($"flip executed / direction={direction}, localScaleX={ctx.transform.localScale.x}");
                FlipDirection();
            }
            if (!CheckAdjacentTarget())
            {
                if (ctx._hitRecoveryTimer <= 0) UpdateVelocity();
                ctx.animController.PlayStateAnimation(PlayerState.MOVE);
                return new EvaluateResult(BTNode.State.Running, this.GetType().ToString());
            }
            else
            {
                ctx.animController.PlayStateAnimation(PlayerState.IDLE);
                return new EvaluateResult(BTNode.State.Failure, this.GetType().ToString());
            }
        }
        
        private bool CheckAdjacentTarget()
        {
            return Mathf.Abs(ctx._target.position.x - ctx.transform.position.x) <= ctx.stats.attackRange + ctx.raycastDetectionMargin;
        }

        private void FlipDirection()
        {
            Vector3 scale = ctx.transform.localScale;
            scale.x = -scale.x;
            ctx.transform.localScale = scale;
            ctx._flipTimer = ctx.flipCooldown;
        }
    
        private void UpdateVelocity()
        {
            Vector2 velocity = ctx.rb.linearVelocity;
            velocity.x = -ctx.transform.localScale.x * ctx.stats.moveSpeed;
            ctx.rb.linearVelocity = velocity;   
        }
    }

    class ActionAttack : ActionNode
    {
        BossAlphaBehavior ctx;
        public ActionAttack(BossAlphaBehavior ctx) { this.ctx = ctx; }

        public override EvaluateResult Evaluate()
        {
            if (ctx._target == null) return new EvaluateResult(BTNode.State.Failure, this.GetType().ToString());
            
            var rndIdx = UnityEngine.Random.Range(1, ctx.stats.skillDatas.Length + 1);
            Logger.Write($"random selected skill / rndIdx={rndIdx}, numSkills={ctx.stats.skillDatas.Length}");
            var attackRes = ctx.attackSystem.ExecuteAttack(rndIdx, ctx.transform, ctx.layerPlayer);
            if (!attackRes || ctx._attackRecoveryTimer > 0) return new EvaluateResult(BTNode.State.Running, this.GetType().ToString());
            ctx._attackRecoveryTimer = ctx.animController.PlayStateAnimation(PlayerState.ATTACK, rebind: true);
            return new EvaluateResult(BTNode.State.Success, this.GetType().ToString());
        }
    }

    class ActionPatrol : ActionNode
    {
        BossAlphaBehavior ctx;
        public ActionPatrol(BossAlphaBehavior ctx) { this.ctx = ctx; }

        public override EvaluateResult Evaluate()
        {
            if (!ctx.CheckGrounded()) return new EvaluateResult(BTNode.State.Failure, this.GetType().ToString());
            if (ctx._hitRecoveryTimer > 0) return new EvaluateResult(BTNode.State.Failure, this.GetType().ToString());
            
            CheckFlipDirection();
            if (ctx._hitRecoveryTimer <= 0) UpdateVelocity();
            ctx.animController.PlayStateAnimation(PlayerState.MOVE);
            return new EvaluateResult(BTNode.State.Success, this.GetType().ToString());
        }

        private bool CheckFlipDirection()
        {
            if (ctx._flipTimer > 0) return false;

            // condition1: 패트롤 범위를 벗어났을 때
            if (Mathf.Abs(ctx._startPos.x - ctx.transform.position.x) > ctx.patrolRangeDistance)
            {
                FlipDirection();
                return true;
            }

            float direction = -Mathf.Sign(ctx.transform.localScale.x);
            // condition2: 벽에 닿았을 때
            // wall check
            var pos = ctx.transform.position;
            pos.y += ctx.YPosAdjust;
            pos.x += direction * ctx.wallBoxDistance;
            Collider2D hitWall = Physics2D.OverlapBox(
                pos,                    // 끝 위치
                ctx.wallBoxSize,        // 박스 크기
                0f,                     // 회전
                ctx.layerGround         // 레이어
            );
            if (hitWall != null)
            {
                FlipDirection();
                return true;
            }

            // condition3: 더 이상 갈 수 없는 곳일  때
            // ground check
            // var boxSize = ctx.groundBoxSize;
            pos = ctx.transform.position;
            pos.y -= ctx.groundDetectionYPosAdjust;
            pos.x += direction * ctx.groundDistance;
            Collider2D hitGround = Physics2D.OverlapBox(
                pos,                    // 끝 위치
                ctx.groundBoxSize,        // 박스 크기
                0f,                     // 회전
                ctx.layerGround         // 레이어
            );
            if (hitGround == null)
            {
                FlipDirection();
                return true;
            }

            return false;
        }

        private void FlipDirection()
        {
            Vector3 scale = ctx.transform.localScale;
            scale.x = -scale.x;
            ctx.transform.localScale = scale;
            ctx._flipTimer = ctx.flipCooldown;
        }
    
        private void UpdateVelocity()
        {
            Vector2 velocity = ctx.rb.linearVelocity;
            velocity.x = -ctx.transform.localScale.x * ctx.stats.moveSpeed;
            ctx.rb.linearVelocity = velocity;   
        }
    }

    class ActionIDLE : ActionNode
    {
        BossAlphaBehavior ctx;
        public ActionIDLE(BossAlphaBehavior ctx) { this.ctx = ctx; }

        public override EvaluateResult Evaluate()
        {
            ctx.animController.PlayStateAnimation(PlayerState.IDLE);
            return new EvaluateResult(BTNode.State.Success, this.GetType().ToString());
        }
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        // hit wall
        Gizmos.color = Color.blue;
        var pos = transform.position;
        pos.y += YPosAdjust;
        foreach (var dir in new int[] { 1, -1 })
        {
            var p = pos;
            p.x += dir * wallBoxDistance;
            Gizmos.DrawWireCube(p, wallBoxSize);
        }

        // hit ground
        Gizmos.color = Color.blue;
        pos = transform.position;
        pos.y -= groundDetectionYPosAdjust;
        foreach (var dir in new int[] { 1, -1 })
        {
            var p = pos;
            p.x += dir * groundDistance;
            Gizmos.DrawWireCube(p, groundBoxSize);
        }

        // player detection raycast
        Gizmos.color = Color.cyan;
        pos = transform.position;
        pos.y += YPosAdjust;
        var hitbox = wallBoxSize;
        hitbox.y *= transform.localScale.y;
        foreach (var dir in new int[] { 1, -1 })
        {
            Vector3 p = pos;
            p.x += dir * patrolRangeDistance;
            Gizmos.DrawWireCube(p, hitbox);
        }
    }
#endif
}
