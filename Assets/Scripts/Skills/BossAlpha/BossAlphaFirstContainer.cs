using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossAlphaFirstContainer : MonoBehaviour
{
    [SerializeField] float rotationSpeed;
    [SerializeField] Transform[] spawnPoints;
    [SerializeField] float spawnCooldown;
    float _spawnTimer;
    Dictionary<int, Queue<SkillLogic>> instanceContainer;
    int _numInstances;
    int _cnt;
    AttackContainer attack;
    Transform origin;
    float spawnYAdjust;
    LayerMask targetLayerMask;
    ObjectPool<SkillLogic> pool;
    HealthSystem targetHealthSystem;
    bool inProgress;
    Coroutine spawnCoroutine;
    public bool IsSpawning {
        get => spawnCoroutine != null;
        private set { }
    }
    
    public void ProcureInstances(
        List<SkillLogic> instances, 
        AttackContainer attack, Transform origin, float spawnYAdjust,
        LayerMask targetLayerMask, ObjectPool<SkillLogic> pool, HealthSystem targetHealthSystem
        )
    {
        this.attack = attack;
        this.origin = origin;
        this.spawnYAdjust = spawnYAdjust;
        this.targetLayerMask = targetLayerMask;
        this.pool = pool;
        this.targetHealthSystem = targetHealthSystem;
        _numInstances = instances.Count;
        instanceContainer = new Dictionary<int, Queue<SkillLogic>>();
        for (int i = 0; i < spawnPoints.Length; i++) instanceContainer[i] = new Queue<SkillLogic>();
        for (int i = 0; i < _numInstances; i++) instanceContainer[i % spawnPoints.Length].Enqueue(instances[i]);
        Logger.Write($"procure instances complete ! / _numInstances={_numInstances}");
        ExecuteSpawnInstances();
    }

    void Update()
    {
        if (inProgress) transform.Rotate(0f, 0f, rotationSpeed * Time.deltaTime);
        if (_spawnTimer > 0) _spawnTimer -= Time.deltaTime;
    }

    public void ExecuteSpawnInstances()
    {
        _spawnTimer = 0f;
        _cnt = 0;
        transform.rotation = Quaternion.identity;
        inProgress = true;
        spawnCoroutine = StartCoroutine(FnSpawnInstances());
    }

    IEnumerator FnSpawnInstances()
    {
        while (_cnt < _numInstances)
        {
            if (_spawnTimer > 0)
            {
                yield return null;
                continue;
            }

            for (int i = 0; i < spawnPoints.Length; i++)
            {
                var instance = instanceContainer[i].Dequeue();
                instance.Init(attack, spawnPoints[i], spawnYAdjust, targetLayerMask, pool, targetHealthSystem);
                _cnt++;
            }
            _spawnTimer = spawnCooldown;
            Logger.Write($"spawn skill instantces ! / cnt={_cnt}");
        }
        inProgress = false;
        spawnCoroutine = null;
    }

    public void StopSpawnCoroutine()
    {
        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
            spawnCoroutine = null;
            inProgress = false;
        }
    }
}
