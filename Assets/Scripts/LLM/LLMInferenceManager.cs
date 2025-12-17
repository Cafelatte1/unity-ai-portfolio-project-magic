using System;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.Events;


public class LLMInferenceManager : Singleton<LLMInferenceManager>
{
    [Tooltip("Model artifacts must be in Assets/Models/")]
    [SerializeField] string model_id;
    [Tooltip("Only supported device; CPU")]
    [SerializeField] string device = "CPU";
    private readonly ConcurrentQueue<Action> _mainThreadActions = new ConcurrentQueue<Action>();
    private IntPtr _pipeline = IntPtr.Zero;
    private LLMRequestQueue _requestQueue;
    private LLMWorkerThread _worker;
    public bool IsInitializing { get; private set; }
    public bool IsModelReady => (_pipeline != IntPtr.Zero) && (_worker != null);
    public UnityEvent EventModelReady;

    [DllImport("kernel32", CharSet = CharSet.Unicode)]
    public static extern bool SetDllDirectory(string lpPathName);

    [DllImport("project-magic-localai-unity", CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr OV_LoadModel(string modelPath, string device);

    [DllImport("project-magic-localai-unity", CallingConvention = CallingConvention.Cdecl)]
    private static extern void OV_Release(IntPtr pipelinePtr);

    protected override void Awake()
    {
        base.Awake();

        string dllDir = Application.dataPath + "/Plugins/x86_64";
        SetDllDirectory(dllDir);
        Logger.Write($"set DLL folder path / dllDir={dllDir}");
        IsInitializing = true;
    }

    async void Start()
    {
        try
        {
            await Setup();
            EventModelReady?.Invoke();
        }
        catch (Exception e)
        {
            Logger.Write($"llm inference manager setup failed / msg={e}", "ERROR");
        }
    }

    void LateUpdate()
    {
        // 워커 스레드에서 등록한 작업들을 메인 스레드에서 실행
        // OnWorkerCompleted 함수 실행
        while (_mainThreadActions.TryDequeue(out var action))
        {
            try { action?.Invoke(); }
            catch (Exception e) { Logger.Write($"error invoke function from _mainThreadActions / msg={e}", "ERROR"); }
        }
    }

    async Task Setup()
    {
        try
        {
            _pipeline = await Task.Run(() => InitInferencePipeline());
            InitWorkerThread(_pipeline);
            Logger.Write($"success to setup llm inference pipeline and worker thread");
        }
        catch (Exception e)
        {
            Logger.Write($"failed to setup llm inference pipeline and worker thread / msg={e}", "ERROR");
        }
        finally
        {
            IsInitializing = false;
        }
    }

    IntPtr InitInferencePipeline()
    {
        string modelPath = Application.dataPath + $"/Models/{model_id}/";
        Logger.Write($"try to load model / modelPath={modelPath}");

        Stopwatch sw;
        if (Logger.DEBUG) sw = new Stopwatch();
        else sw = null;
# if UNITY_EDITOR
        sw = new Stopwatch();
# endif

        sw?.Start();
        var pipeline = OV_LoadModel(modelPath, device != "CPU" ? "CPU" : device);
        sw?.Stop();
        if (sw != null)
            Logger.Write($"execute dll; OV_LoadModel / elapsed(sec)={MathUtils.Ceil(sw.ElapsedMilliseconds / 1000, 2)}");
# if UNITY_EDITOR
        UnityEngine.Debug.Log($"execute dll; OV_LoadModel / elapsed(sec)={MathUtils.Ceil(sw.ElapsedMilliseconds / 1000, 2)}");
# endif

        if (pipeline == IntPtr.Zero)
            Logger.Write("Failed to execute OV_LoadModel", "ERROR");
        
        return pipeline;
    }

    void InitWorkerThread(IntPtr _pipeline)
    {
        if (_pipeline == IntPtr.Zero) return;

        // worker-thread queue 생성
        _requestQueue = new LLMRequestQueue();

        // worker thread 실행
        _worker = new LLMWorkerThread(
            _requestQueue, _pipeline,
            dispatchResponse: (req, output) =>
            {
                _mainThreadActions.Enqueue(() =>
                {
                    OnWorkerCompleted(req, output);
                });
            }
        );
    }

    public void RequestInference(LLMRequest request) => _requestQueue.Enqueue(request);

    public void OnWorkerCompleted(LLMRequest request, LLMOutput output)
    {
        Logger.Write("received request and result from worker");
        var response = new LLMResponse(request, output);
        request.onCompleted?.Invoke(response);
    }

    void OnDestroy()
    {
        _worker?.Stop();
        if (_pipeline != IntPtr.Zero)
        {
            OV_Release(_pipeline);
            _pipeline = IntPtr.Zero;
        }
    }
}
