using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Firebase para WebGL:
///  • "9. Configurar Firebase": lee Assets/google-services.json y crea Assets/Resources/FirebaseConfig
///    (clave web + URL de la base de datos). El juego se conecta por la API REST de Firebase,
///    que funciona en WebGL, Android y PC.
///  • "10. Preparar WebGL": cambia la plataforma a WebGL y deja listas las opciones para subirlo a internet.
/// </summary>
public static class FirebaseConfigTool
{
    private const string AssetPath = "Assets/Resources/FirebaseConfig.asset";

    [MenuItem("Torre de Pasteles/9. Configurar Firebase (WebGL y Android)", priority = 9)]
    public static void ConfigureMenu()
    {
        string msg = Configure(out bool ok);
        EditorUtility.DisplayDialog("Firebase", msg, "OK");
        if (ok) Selection.activeObject = AssetDatabase.LoadAssetAtPath<FirebaseConfig>(AssetPath);
    }

    /// <summary>Crea o actualiza FirebaseConfig desde google-services.json (lo usa también CREAR TODO).</summary>
    public static string Configure(out bool ok)
    {
        ok = false;
        string json = Path.Combine(Application.dataPath, "google-services.json");
        FirebaseConfig fromFile = FirebaseConfig.FromGoogleServices(json);

        if (!AssetDatabase.IsValidFolder("Assets/Resources")) AssetDatabase.CreateFolder("Assets", "Resources");
        FirebaseConfig cfg = AssetDatabase.LoadAssetAtPath<FirebaseConfig>(AssetPath);
        if (cfg == null)
        {
            cfg = ScriptableObject.CreateInstance<FirebaseConfig>();
            AssetDatabase.CreateAsset(cfg, AssetPath);
        }

        if (fromFile != null)
        {
            cfg.apiKey = fromFile.apiKey;
            cfg.databaseUrl = fromFile.databaseUrl;
        }
        EditorUtility.SetDirty(cfg);
        AssetDatabase.SaveAssets();

        if (cfg.IsValid)
        {
            ok = true;
            return "Listo: Firebase configurado.\n\nBase de datos: " + cfg.databaseUrl +
                   "\n\nEl juego se conecta por internet (API REST), así que funciona en WebGL, Android y en el editor.";
        }
        return "No encontré los datos en Assets/google-services.json.\n\n" +
               "Llena a mano Assets/Resources/FirebaseConfig:\n" +
               "• Api Key: Firebase → Configuración del proyecto → General → Clave de API web\n" +
               "• Database Url: Firebase → Realtime Database (la URL de arriba, termina en firebaseio.com o firebasedatabase.app)";
    }

    [MenuItem("Torre de Pasteles/10. Preparar WebGL", priority = 10)]
    public static void PrepareWebGL()
    {
        Configure(out bool ok);
        if (!ok)
        {
            EditorUtility.DisplayDialog("WebGL", "Primero hay que configurar Firebase (menú 9).", "OK");
            return;
        }

        // Funciona en cualquier página (GitHub Pages, itch.io, Firebase Hosting...) sin configurar el servidor
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
        PlayerSettings.WebGL.decompressionFallback = true;
        PlayerSettings.WebGL.dataCaching = true;
        PlayerSettings.defaultWebScreenWidth = 1280;
        PlayerSettings.defaultWebScreenHeight = 720;
        PlayerSettings.runInBackground = true;
        PlayerSettings.productName = "Sugaru";

        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.WebGL)
        {
            if (EditorUtility.DisplayDialog("WebGL", "¿Cambiar la plataforma a WebGL ahora? (Unity va a reimportar, puede tardar unos minutos)", "Sí", "Después"))
                EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.WebGL, BuildTarget.WebGL);
        }

        EditorUtility.DisplayDialog("WebGL",
            "Listo. Ahora:\n\n1. File → Build Profiles → Web (debe estar activa).\n" +
            "2. Revisa que estén las 3 escenas: LoginScene, MainMenu, GameScene.\n" +
            "3. Dale Build y elige una carpeta vacía.\n\n" +
            "Para probarlo usa 'Build And Run' (abrir el index.html con doble clic NO funciona).", "OK");
    }

    /// <summary>Si falta la configuración, la crea sola al abrir el proyecto.</summary>
    [InitializeOnLoadMethod]
    private static void AutoConfigure()
    {
        EditorApplication.delayCall += () =>
        {
            if (AssetDatabase.LoadAssetAtPath<FirebaseConfig>(AssetPath) != null) return;
            if (!File.Exists(Path.Combine(Application.dataPath, "google-services.json"))) return;
            Configure(out bool _);
            Debug.Log("Sugaru: FirebaseConfig creado automáticamente desde google-services.json.");
        };
    }
}
