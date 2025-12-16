using System.Diagnostics;

public static class Logger
{
    public static bool DEBUG = true;

    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public static void Write(string log="No Message", string level="INFO")
    {
        if (DEBUG)
        {
            StackTrace trace = new StackTrace();
            StackFrame caller = trace.GetFrame(1);
            var method = caller.GetMethod();
            UnityEngine.Debug.Log($"{level} / {method.DeclaringType.Name}.{method.Name} / {log}");  
        }
    }
}