using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Diagnostics;

public class LLMWorkerThread
{
    private Thread _thread;
    private bool _running;
    private readonly LLMRequestQueue _queue;
    private readonly IntPtr _pipeline;
    private readonly Action<LLMRequest, LLMOutput> _dispatchResponse;

    public LLMWorkerThread(LLMRequestQueue queue, IntPtr pipeline, Action<LLMRequest, LLMOutput> dispatchResponse)
    {
        _queue = queue;
        _pipeline = pipeline;
        _dispatchResponse = dispatchResponse;
        _thread = new Thread(ThreadLoop);
        _thread.IsBackground = true;
        _thread.Start();
        _running = true;
    }

    [DllImport("project-magic-localai-unity", CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr OV_Inference(IntPtr pipeline, string messagesJson, string toolsJson);

    [DllImport("project-magic-localai-unity", CallingConvention = CallingConvention.Cdecl)]
    private static extern void OV_FreeString(IntPtr strPtr);

    private void ThreadLoop()
    {
        Logger.Write("LLM worker start");

        while (_running)
        {
            _queue.WaitForNewItem();

            while (_queue.TryDequeue(out var req))
            {
                if (req == null) continue;

                try
                {
                    Stopwatch sw;
                    if (Logger.DEBUG) sw = new Stopwatch();
                    else sw = null;
# if UNITY_EDITOR
                    sw = new Stopwatch();
# endif

                    Logger.Write($"run inference / reqId={req.requestId}");
                    // run inference
                    sw?.Start();
                    IntPtr resultPtr = OV_Inference(_pipeline, req.messagesJson, req.toolsJson);
                    sw?.Stop();
                    // get generated result
                    if (resultPtr == IntPtr.Zero) continue;
                    string result = Marshal.PtrToStringUTF8(resultPtr);
                    var output = new LLMOutput(result, null, (sw != null) ? (sw.ElapsedMilliseconds / 1000f) : 0f);
# if UNITY_EDITOR
                    UnityEngine.Debug.Log($"success to inference / elapsed(sec)={MathUtils.Ceil(sw.ElapsedMilliseconds / 1000, 2)}");
# endif
                    _dispatchResponse(req, output);
                    // free string in Cpp heap
                    OV_FreeString(resultPtr);
                    Logger.Write($"success to inference / reqId={req.requestId}");
                }
                catch (Exception e)
                {
                    Logger.Write($"failed to inference / msg={e}", "ERROR");
                }
            }
        }

        Logger.Write("LLM worker stopped");
    }

    public void Stop()
    {
        _running = false;
        _queue.Enqueue(null);
    }

}
