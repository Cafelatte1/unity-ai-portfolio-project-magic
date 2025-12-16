using System.Collections.Generic;
using UnityEngine;

public class ContainerController : Singleton<ContainerController>
{
    Dictionary<string, Transform> containerMapper;

    protected override void Awake()
    {
        base.Awake();
        containerMapper = new Dictionary<string, Transform>();
    }

    // 내부: Transform 컨테이너 생성
    private Transform CreateContainer(string containerName)
    {
        GameObject go = new GameObject(containerName);
        return go.transform;
    }

    // 외부에서 호출: 컨테이너 읽기 (없으면 자동 생성)
    public Transform GetContainer(string containerName)
    {
        if (!containerMapper.TryGetValue(containerName, out Transform container))
        {
            Logger.Write($"Container not found → Auto create / name={containerName}", "WARNING");

            container = CreateContainer(containerName);
            containerMapper[containerName] = container;
        }

        return container;
    }
}
