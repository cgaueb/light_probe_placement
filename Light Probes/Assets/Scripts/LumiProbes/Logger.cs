using System;
using System.Threading;

public class LumiLogger
{
    private static Mutex mut = new Mutex();
    private static LumiLogger logger = new LumiLogger();
    private string name = "LumiProbes_log.txt";

    public static LumiLogger Logger {
        get { return logger; }
    }

    public void Log(String msg) {
        mut.WaitOne();
        string log_msg = DateTime.Now + " [" + Thread.CurrentThread.ManagedThreadId.ToString("00") + "]: Log [INFO]: " + msg;
        if (System.Diagnostics.Debugger.IsAttached) {
            System.Diagnostics.Debug.Write(log_msg);
        }
        using (System.IO.StreamWriter file = new System.IO.StreamWriter(name, true)) {
            file.WriteLine(log_msg);
        }
        UnityEngine.Debug.Log(msg);
        mut.ReleaseMutex();
    }
    public void LogWarning(String msg) {
        mut.WaitOne();
        string log_msg = DateTime.Now + " [" + Thread.CurrentThread.ManagedThreadId.ToString("00") + "]: Log [WARN]: " + msg;
        if (System.Diagnostics.Debugger.IsAttached) {
            System.Diagnostics.Debug.Write(log_msg);
        }
        using (System.IO.StreamWriter file = new System.IO.StreamWriter(name, true)) {
            file.WriteLine(log_msg);
        }
        UnityEngine.Debug.LogWarning(msg);
        mut.ReleaseMutex();
    }
    public void LogError(String msg) {
        mut.WaitOne();
        string log_msg = DateTime.Now + " [" + Thread.CurrentThread.ManagedThreadId.ToString("00") + "]: Log [ERRO]: " + msg;
        if (System.Diagnostics.Debugger.IsAttached) {
            System.Diagnostics.Debug.Write(log_msg);
        }
        using (System.IO.StreamWriter file = new System.IO.StreamWriter(name, true)) {
            file.WriteLine(log_msg);
        }
        UnityEngine.Debug.LogError(msg);
        mut.ReleaseMutex();
    }
}
