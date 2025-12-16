using UnityEngine;

public abstract class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T _instance;
    public static T Instance
    {
        get
        {
            // 이미 존재하면 반환
            if (_instance != null)
                return _instance;

            // 씬에서 검색 (1회)
            _instance = FindFirstObjectByType<T>();
            if (_instance != null)
                return _instance;

            // 자동 생성 (선택적)
            var obj = new GameObject(typeof(T).Name);
            _instance = obj.AddComponent<T>();
            DontDestroyOnLoad(obj);
            return _instance;
        }
    }

    protected virtual void Awake()
    {
        // 중복 방지
        if (_instance == null)
        {
            _instance = this as T;
            DontDestroyOnLoad(gameObject);
        }
        else if (_instance != this)
        {
            gameObject.SetActive(false);
            Destroy(gameObject);
        }
    }

}
