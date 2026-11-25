using System.IO;
using UnityEngine;

/// <summary>
/// Datos para conectarse a TU proyecto de Firebase (Assets/Resources/FirebaseConfig).
/// Se llenan SOLOS desde Assets/google-services.json con el menú
/// "Torre de Pasteles → 9. Configurar Firebase (WebGL y Android)" (CREAR TODO también lo hace).
/// </summary>
[CreateAssetMenu(fileName = "FirebaseConfig", menuName = "Sugaru/Firebase Config")]
public class FirebaseConfig : ScriptableObject
{
    [Tooltip("Clave web (Web API Key) del proyecto. Está en google-services.json → client → api_key → current_key")]
    public string apiKey;
    [Tooltip("URL de la Realtime Database, por ejemplo https://tu-proyecto-default-rtdb.firebaseio.com")]
    public string databaseUrl;

    public bool IsValid => !string.IsNullOrEmpty(apiKey) && !string.IsNullOrEmpty(databaseUrl);

    public static FirebaseConfig Load()
    {
        FirebaseConfig c = Resources.Load<FirebaseConfig>("FirebaseConfig");
#if UNITY_EDITOR
        // En el editor, si falta, se lee directo del google-services.json
        if (c == null || !c.IsValid)
        {
            FirebaseConfig fromFile = FromGoogleServices(Path.Combine(Application.dataPath, "google-services.json"));
            if (fromFile != null) return fromFile;
        }
#endif
        return c;
    }

    /// <summary>Lee apiKey y databaseUrl de un google-services.json.</summary>
    public static FirebaseConfig FromGoogleServices(string path)
    {
        if (!File.Exists(path)) return null;
        var root = SugaruJson.Parse(File.ReadAllText(path)) as System.Collections.Generic.Dictionary<string, object>;
        if (root == null) return null;
        FirebaseConfig c = CreateInstance<FirebaseConfig>();
        if (root.TryGetValue("project_info", out object piObj) && piObj is System.Collections.Generic.Dictionary<string, object> pi
            && pi.TryGetValue("firebase_url", out object url) && url != null)
            c.databaseUrl = url.ToString();
        if (root.TryGetValue("client", out object clientsObj) && clientsObj is System.Collections.Generic.List<object> clients && clients.Count > 0
            && clients[0] is System.Collections.Generic.Dictionary<string, object> client
            && client.TryGetValue("api_key", out object keysObj) && keysObj is System.Collections.Generic.List<object> keys && keys.Count > 0
            && keys[0] is System.Collections.Generic.Dictionary<string, object> key && key.TryGetValue("current_key", out object k) && k != null)
            c.apiKey = k.ToString();
        return c.IsValid ? c : null;
    }
}
