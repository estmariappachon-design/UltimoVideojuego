using System;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.Networking;

/// <summary>
/// Peticiones HTTP a Firebase (Authentication y Realtime Database) con UnityWebRequest.
/// Funciona en WebGL, Android, iOS, PC y en el editor (no necesita el SDK de Firebase).
/// </summary>
public static class FirebaseRest
{
    public class Response
    {
        public long code;
        public string text;
        public bool networkError;
        public bool Ok => !networkError && code >= 200 && code < 300;
    }

    public static Task<Response> Send(string method, string url, string jsonBody = null, string contentType = "application/json")
    {
        var tcs = new TaskCompletionSource<Response>();
        UnityWebRequest req = new UnityWebRequest(url, method);
        req.downloadHandler = new DownloadHandlerBuffer();
        if (jsonBody != null)
        {
            req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(jsonBody));
            req.SetRequestHeader("Content-Type", contentType);
        }
        req.timeout = 20;
        UnityWebRequestAsyncOperation op = req.SendWebRequest();
        op.completed += _ =>
        {
            var r = new Response
            {
                code = req.responseCode,
                text = req.downloadHandler != null ? req.downloadHandler.text : "",
                networkError = req.result == UnityWebRequest.Result.ConnectionError
            };
            req.Dispose();
            tcs.TrySetResult(r);
        };
        return tcs.Task;
    }

    /// <summary>Saca el código de error de una respuesta de Firebase Auth (por ejemplo "EMAIL_EXISTS").</summary>
    public static string AuthErrorCode(Response r)
    {
        try
        {
            if (SugaruJson.Parse(r.text) is System.Collections.Generic.Dictionary<string, object> root &&
                root.TryGetValue("error", out object e) && e is System.Collections.Generic.Dictionary<string, object> err &&
                err.TryGetValue("message", out object m) && m != null)
                return m.ToString();
        }
        catch (Exception) { }
        return "HTTP_" + r.code;
    }

    public static string Escape(string s) => UnityWebRequest.EscapeURL(s);
}
