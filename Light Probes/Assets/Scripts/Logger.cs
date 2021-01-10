using System;

public class LumiLogger
{
    private static LumiLogger logger = new LumiLogger();
    public static LumiLogger Logger {
        get { return logger; }
    }

    public void Log(String msg) {
        if (System.Diagnostics.Debugger.IsAttached) {
            System.Diagnostics.Debug.Write("Log [INFO]: " + msg);
        }
        UnityEngine.Debug.Log(msg);
    }
    public void LogWarning(String msg) {
        if (System.Diagnostics.Debugger.IsAttached) {
            System.Diagnostics.Debug.Write("Log [WARN]: " + msg);
        }
        UnityEngine.Debug.LogWarning(msg);
    }
    public void LogError(String msg) {
        if (System.Diagnostics.Debugger.IsAttached) {
            System.Diagnostics.Debug.Write("Log [ERRO]: " + msg);
        }
        UnityEngine.Debug.LogError(msg);
    }
}
