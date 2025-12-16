using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 전체 게임의 씬 로딩 및 전환을 관리하는 싱글톤 컨트롤러.
/// - 현재 활성화된 씬 추적
/// - 씬 이름 목록 관리
/// - 씬 전환 요청(다음, 이전, 첫, 마지막)
/// - 중복 요청 방지 (debounce)
/// [Usage]
/// scene_manager.StartCoroutine(Waiter.DelayedAction(() => {
///     scene_manager.ReloadLevel();
/// }, 3f, realtime: true));
/// </summary>
public class SceneController : Singleton<SceneController>
{
    int scene_count;
    string[] scene_names;
    bool isLoading;
    Scene active_scene;
    [SerializeField] float debounceSeconds = 0.1f;
    float _lastRequestTime = -1f;

    protected override void Awake()
    {
        base.Awake();

        scene_count = SceneManager.sceneCountInBuildSettings;
        scene_names = new string[scene_count];
        for (int i = 0; i < scene_count; i++)
        {
            string path = SceneUtility.GetScenePathByBuildIndex(i);
            scene_names[i] = Path.GetFileNameWithoutExtension(path);
        }

        active_scene = SceneManager.GetActiveScene();
        isLoading = false;
        SceneManager.activeSceneChanged += OnActiveSceneChanged;
    }

    void OnActiveSceneChanged(Scene prev, Scene next)
    {
        Logger.Write(level: "SCENE", log: $"Active Scene Changed / from={prev.name}, to={next.name}");
        active_scene = next;
        isLoading = false;
    }

    void OnDestroy()
    {
        SceneManager.activeSceneChanged -= OnActiveSceneChanged;
    }

    public Scene GetActiveScene() => active_scene;
    public T GetSceneInfo<T>()
    {
        if (typeof(T) == typeof(int))
        {
            // int -> return build index
            object value = active_scene.buildIndex;
            return (T)value;
        }
        else if (typeof(T) == typeof(string))
        {
            // string -> return scene name
            object value = active_scene.name;
            return (T)value;
        }
        else
        {
            throw new System.InvalidOperationException(
                $"Unsupported type {typeof(T).Name}. Only int or string are allowed."
            );
        }
    }
    public bool IsLoading() => isLoading;
    public bool IsValidNextScene() => (active_scene.buildIndex + 1 >= 0) && (active_scene.buildIndex + 1 < scene_count);

    public bool LoadActiveScene()
    {
        Logger.Write(level: "SCENE", log: $"LoadScene (active) / name={active_scene.name}, index={active_scene.buildIndex}");
        return LoadScene(active_scene.buildIndex);
    }

    public bool LoadNextScene()
    {
        int next = active_scene.buildIndex + 1;
        Logger.Write(level: "SCENE", log: $"LoadScene (next) / index={next}");
        return LoadScene(next);
    }

    public bool LoadFirstScene()
    {
        Logger.Write(level: "SCENE", log: "LoadScene (first) / index=0");
        return LoadScene(0);
    }

    public bool LoadLastScene()
    {
        int last = scene_count - 1;
        Logger.Write(level: "SCENE", log: $"LoadScene (last) / index={last}");
        return LoadScene(last);
    }

    public bool LoadScene(int index)
    {
        if (!AcceptRequest()) { Logger.Write(level: "WARNING", log: $"Ignore load(int) / index={index}"); return false; }

        if (index == -1) { QuitApp(); return true; }

        if (index >= 0 && index < scene_count)
        {
            isLoading = true;
            SceneManager.LoadScene(index);
            return true;
        }
        else
        {
            Logger.Write(level: "SCENE", log: $"Invalid index. count={scene_count}, index={index}");
            return false;
        }
    }

    public bool LoadScene(string name)
    {
        if (!AcceptRequest()) { Logger.Write(level: "WARNING", log: $"Ignore load(string) / name={name}"); return false; }

        if (name == "Quit") { QuitApp(); return true; }

        if (scene_names.Contains(name))
        {
            isLoading = true;
            SceneManager.LoadScene(name);
            return true;
        }
        else
        {
            Logger.Write(level: "SCENE", log: $"Invalid name. names=[{string.Join(",", scene_names)}]");
            return false;
        }
    }

    bool AcceptRequest()
    {
        if (isLoading) return false;
        if (Time.unscaledTime - _lastRequestTime < debounceSeconds) return false;
        _lastRequestTime = Time.unscaledTime;
        return true;
    }

    void QuitApp()
    {
        Logger.Write(level: "SCENE", log: "Application.Quit()");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
