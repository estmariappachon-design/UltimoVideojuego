using System;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// DIAGNÓSTICO: guarda los ERRORES ROJOS de la Console en el archivo "Diagnostico_errores.txt"
/// (en la carpeta del proyecto, al lado de Assets). Así se pueden revisar desde GitHub.
///  • Se guardan solos los errores que salgan al dar Play o al hacer un Build.
///  • Menú "Torre de Pasteles → Diagnóstico: guardar errores de la Console" copia los que ya están en la Console.
/// </summary>
[InitializeOnLoad]
public static class Diagnostico
{
    private static readonly string FilePath = Path.Combine(Path.GetDirectoryName(Application.dataPath), "Diagnostico_errores.txt");
    private static int written;

    static Diagnostico()
    {
        Application.logMessageReceivedThreaded -= OnLog;
        Application.logMessageReceivedThreaded += OnLog;
    }

    private static void OnLog(string message, string stackTrace, LogType type)
    {
        if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert) return;
        if (written > 300) return;   // no llenar el disco si un error se repite en cada frame
        written++;
        try
        {
            File.AppendAllText(FilePath,
                $"[{DateTime.Now:HH:mm:ss}] {type} ({(Application.isPlaying ? "jugando" : "editor")})\n{message}\n{FirstLines(stackTrace, 6)}\n----\n");
        }
        catch (Exception) { }
    }

    [MenuItem("Torre de Pasteles/Diagnóstico: guardar errores de la Console", priority = 30)]
    public static void SaveConsoleErrors()
    {
        var sb = new StringBuilder();
        sb.AppendLine("=== Errores de la Console (" + DateTime.Now + ") ===");
        sb.AppendLine("Unity " + Application.unityVersion + " | Plataforma activa: " + EditorUserBuildSettings.activeBuildTarget);
        int count = 0;
        try
        {
            // API interna de la Console (se lee por reflexión)
            Type entries = Type.GetType("UnityEditor.LogEntries, UnityEditor.dll");
            Type entryType = Type.GetType("UnityEditor.LogEntry, UnityEditor.dll");
            object entry = Activator.CreateInstance(entryType);
            MethodInfo start = entries.GetMethod("StartGettingEntries", BindingFlags.Static | BindingFlags.Public);
            MethodInfo end = entries.GetMethod("EndGettingEntries", BindingFlags.Static | BindingFlags.Public);
            MethodInfo get = entries.GetMethod("GetEntryInternal", BindingFlags.Static | BindingFlags.Public);
            FieldInfo msgF = entryType.GetField("message", BindingFlags.Instance | BindingFlags.Public);
            FieldInfo modeF = entryType.GetField("mode", BindingFlags.Instance | BindingFlags.Public);
            int total = (int)start.Invoke(null, null);
            for (int i = 0; i < total && count < 200; i++)
            {
                get.Invoke(null, new object[] { i, entry });
                int mode = Convert.ToInt32(modeF.GetValue(entry));
                string msg = (string)msgF.GetValue(entry);
                // bits de error: Error, Assert, Fatal, ScriptingError, ScriptCompileError, ScriptingException, ScriptingAssertion
                bool isError = (mode & (1 | 2 | 16 | 64 | 256 | 2048 | 131072 | 2097152 | 8388608)) != 0;
                bool isWarning = (mode & (128 | 512 | 4096)) != 0 && !isError;
                if (!isError || isWarning) continue;
                sb.AppendLine(FirstLines(msg, 8));
                sb.AppendLine("----");
                count++;
            }
            end.Invoke(null, null);
        }
        catch (Exception e)
        {
            sb.AppendLine("(No se pudo leer la Console: " + e.Message + ")");
        }
        sb.AppendLine("Total de errores copiados: " + count);
        File.WriteAllText(FilePath, sb.ToString());
        EditorUtility.DisplayDialog("Diagnóstico",
            $"Guardé {count} errores en:\n{FilePath}\n\nHaz commit y push para que se puedan revisar desde GitHub.", "OK");
        EditorUtility.RevealInFinder(FilePath);
    }

    [MenuItem("Torre de Pasteles/Diagnóstico: borrar archivo de errores", priority = 31)]
    public static void Clear()
    {
        if (File.Exists(FilePath)) File.Delete(FilePath);
        written = 0;
        Debug.Log("Diagnóstico: archivo de errores borrado.");
    }

    private static string FirstLines(string s, int n)
    {
        if (string.IsNullOrEmpty(s)) return "";
        string[] lines = s.Split('\n');
        return string.Join("\n", lines, 0, Math.Min(n, lines.Length));
    }
}
