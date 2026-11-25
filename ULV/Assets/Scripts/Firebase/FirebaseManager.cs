using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Usuario que inició sesión (mismos nombres que usaba el SDK de Firebase).</summary>
public class FirebaseUserInfo
{
    public string UserId;
    public string Email;
    public string DisplayName;
}

/// <summary>
/// Administrador único de Firebase (Singleton). Vive en LoginScene y sobrevive entre escenas.
/// Usa la API REST de Firebase (Authentication + Realtime Database) con UnityWebRequest,
/// así funciona en WEBGL (navegador), Android, PC y en el editor, sin el SDK de Firebase.
/// Se encarga de: registrar, iniciar sesión, mantener la sesión, cargar/guardar progreso,
/// reportar resultados de nivel y leer el ranking.
/// </summary>
public class FirebaseManager : MonoBehaviour
{
    public static FirebaseManager Instance { get; private set; }

    [Header("Nombres de escenas (deben estar en Build Settings)")]
    public string loginScene = "LoginScene";
    public string menuScene = "MainMenu";
    // La escena del juego se lee de Resources/LevelCatalog

    public bool IsReady { get; private set; }
    public string InitError { get; private set; }
    public FirebaseUserInfo CurrentUser { get; private set; }
    public PlayerProgress Progress { get; private set; }

    /// <summary>Nivel que el jugador eligió en el menú.</summary>
    public int SelectedLevel { get; set; } = 1;

    private const string AuthUrl = "https://identitytoolkit.googleapis.com/v1/accounts:";
    private const string TokenUrl = "https://securetoken.googleapis.com/v1/token?key=";
    private const string PrefRefresh = "sugaru_fb_refresh";
    private const string PrefUid = "sugaru_fb_uid";
    private const string PrefEmail = "sugaru_fb_email";
    private const string PrefName = "sugaru_fb_name";

    private string apiKey;
    private string dbUrl;
    private string idToken;
    private string refreshToken;
    private float tokenExpiresAt;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        FirebaseConfig cfg = FirebaseConfig.Load();
        if (cfg == null || !cfg.IsValid)
        {
            InitError = "Falta la configuración de Firebase. En Unity usa: Torre de Pasteles → 9. Configurar Firebase.";
            Debug.LogError(InitError);
            return;
        }
        apiKey = cfg.apiKey.Trim();
        dbUrl = cfg.databaseUrl.Trim().TrimEnd('/');
        IsReady = true;
        Debug.Log("Firebase (REST) listo: " + dbUrl);
    }

    // =====================================================================
    //  AUTENTICACIÓN
    // =====================================================================

    /// <summary>Registra un usuario. Devuelve null si todo salió bien, o el mensaje de error.</summary>
    public async Task<string> RegisterAsync(string username, string email, string password)
    {
        if (!IsReady) return InitError ?? "Firebase todavía no está listo.";
        try
        {
            var body = new Dictionary<string, object> { { "email", email }, { "password", password }, { "returnSecureToken", true } };
            FirebaseRest.Response r = await FirebaseRest.Send("POST", AuthUrl + "signUp?key=" + apiKey, SugaruJson.Serialize(body));
            if (!r.Ok) return TranslateError(r);
            ReadAuthResponse(r.text, username);

            // Nombre visible también en el perfil de Authentication
            var profile = new Dictionary<string, object> { { "idToken", idToken }, { "displayName", username }, { "returnSecureToken", false } };
            await FirebaseRest.Send("POST", AuthUrl + "update?key=" + apiKey, SugaruJson.Serialize(profile));

            // Registro inicial: nivel 1 desbloqueado, el resto bloqueado, puntaje 0
            Progress = PlayerProgress.CreateNew(username, email);
            await SaveProgressAsync();
            return null;
        }
        catch (Exception e)
        {
            return "Error: " + e.Message;
        }
    }

    /// <summary>Inicia sesión y carga el progreso. Devuelve null si todo salió bien.</summary>
    public async Task<string> LoginAsync(string email, string password)
    {
        if (!IsReady) return InitError ?? "Firebase todavía no está listo.";
        try
        {
            var body = new Dictionary<string, object> { { "email", email }, { "password", password }, { "returnSecureToken", true } };
            FirebaseRest.Response r = await FirebaseRest.Send("POST", AuthUrl + "signInWithPassword?key=" + apiKey, SugaruJson.Serialize(body));
            if (!r.Ok) return TranslateError(r);
            ReadAuthResponse(r.text, null);
            await LoadProgressAsync();
            return null;
        }
        catch (Exception e)
        {
            return "Error: " + e.Message;
        }
    }

    /// <summary>Si quedó una sesión guardada en el dispositivo/navegador, la reutiliza.</summary>
    public async Task<bool> TryAutoLoginAsync()
    {
        if (!IsReady) return false;
        refreshToken = PlayerPrefs.GetString(PrefRefresh, "");
        if (string.IsNullOrEmpty(refreshToken)) return false;
        CurrentUser = new FirebaseUserInfo
        {
            UserId = PlayerPrefs.GetString(PrefUid, ""),
            Email = PlayerPrefs.GetString(PrefEmail, ""),
            DisplayName = PlayerPrefs.GetString(PrefName, "")
        };
        try
        {
            if (!await RefreshTokenAsync()) { ClearSession(); return false; }
            await LoadProgressAsync();
            return true;
        }
        catch (Exception e)
        {
            Debug.LogWarning("Auto-login falló: " + e.Message);
            ClearSession();
            return false;
        }
    }

    public void Logout()
    {
        ClearSession();
        Progress = null;
        SceneManager.LoadScene(loginScene);
    }

    private void ReadAuthResponse(string json, string displayName)
    {
        var d = SugaruJson.Parse(json) as Dictionary<string, object>;
        if (d == null) throw new Exception("Respuesta inválida de Firebase.");
        idToken = Str(d, "idToken");
        refreshToken = Str(d, "refreshToken");
        if (!int.TryParse(Str(d, "expiresIn"), out int expires)) expires = 3600;
        tokenExpiresAt = Time.realtimeSinceStartup + Mathf.Max(60, expires - 120);
        CurrentUser = new FirebaseUserInfo
        {
            UserId = Str(d, "localId"),
            Email = Str(d, "email"),
            DisplayName = displayName ?? Str(d, "displayName")
        };
        SaveSession();
    }

    private async Task<bool> RefreshTokenAsync()
    {
        if (string.IsNullOrEmpty(refreshToken)) return false;
        string form = "grant_type=refresh_token&refresh_token=" + FirebaseRest.Escape(refreshToken);
        FirebaseRest.Response r = await FirebaseRest.Send("POST", TokenUrl + apiKey, form, "application/x-www-form-urlencoded");
        if (!r.Ok) return false;
        var d = SugaruJson.Parse(r.text) as Dictionary<string, object>;
        if (d == null) return false;
        idToken = Str(d, "id_token");
        refreshToken = Str(d, "refresh_token");
        if (!int.TryParse(Str(d, "expires_in"), out int expires)) expires = 3600;
        tokenExpiresAt = Time.realtimeSinceStartup + Mathf.Max(60, expires - 120);
        string uid = Str(d, "user_id");
        if (CurrentUser == null) CurrentUser = new FirebaseUserInfo();
        if (!string.IsNullOrEmpty(uid)) CurrentUser.UserId = uid;
        SaveSession();
        return !string.IsNullOrEmpty(idToken);
    }

    /// <summary>Token válido para la base de datos (lo renueva si ya va a vencer).</summary>
    private async Task<string> TokenAsync()
    {
        if (string.IsNullOrEmpty(idToken) || Time.realtimeSinceStartup > tokenExpiresAt)
        {
            if (!await RefreshTokenAsync()) throw new Exception("La sesión expiró. Vuelve a iniciar sesión.");
        }
        return idToken;
    }

    private void SaveSession()
    {
        PlayerPrefs.SetString(PrefRefresh, refreshToken ?? "");
        if (CurrentUser != null)
        {
            PlayerPrefs.SetString(PrefUid, CurrentUser.UserId ?? "");
            PlayerPrefs.SetString(PrefEmail, CurrentUser.Email ?? "");
            PlayerPrefs.SetString(PrefName, CurrentUser.DisplayName ?? "");
        }
        PlayerPrefs.Save();
    }

    private void ClearSession()
    {
        idToken = null;
        refreshToken = null;
        CurrentUser = null;
        PlayerPrefs.DeleteKey(PrefRefresh);
        PlayerPrefs.DeleteKey(PrefUid);
        PlayerPrefs.DeleteKey(PrefEmail);
        PlayerPrefs.DeleteKey(PrefName);
        PlayerPrefs.Save();
    }

    // =====================================================================
    //  PROGRESO
    // =====================================================================

    /// <summary>Lee players/{uid} y lo convierte en PlayerProgress.</summary>
    public async Task LoadProgressAsync()
    {
        if (CurrentUser == null) throw new Exception("No hay usuario autenticado.");
        string uid = CurrentUser.UserId;
        string token = await TokenAsync();

        FirebaseRest.Response r = await FirebaseRest.Send("GET", $"{dbUrl}/players/{uid}.json?auth={token}");
        if (!r.Ok) throw new Exception(DbError(r));

        if (SugaruJson.Parse(r.text) is Dictionary<string, object> data)
        {
            Progress = PlayerProgress.FromDictionary(data, CurrentUser.Email);
            if (string.IsNullOrEmpty(CurrentUser.DisplayName)) CurrentUser.DisplayName = Progress.username;
        }
        else
        {
            // La cuenta existe en Auth pero no tiene datos (p. ej. se borró la BD): se crea de nuevo
            string name = !string.IsNullOrEmpty(CurrentUser.DisplayName)
                ? CurrentUser.DisplayName
                : (CurrentUser.Email ?? "jugador").Split('@')[0];
            Progress = PlayerProgress.CreateNew(name, CurrentUser.Email);
            await SaveProgressAsync();
        }

        Debug.Log($"Progreso cargado: {Progress.username} | total {Progress.totalScore} | nivel máx {Progress.maxUnlockedLevel}");
    }

    /// <summary>
    /// Guarda en UNA sola escritura atómica (PATCH multi-ruta) el nodo privado (players/uid)
    /// y el nodo público del ranking (ranking/uid).
    /// </summary>
    public async Task SaveProgressAsync()
    {
        if (CurrentUser == null || Progress == null) return;
        string uid = CurrentUser.UserId;
        string token = await TokenAsync();
        var updates = new Dictionary<string, object>
        {
            { "players/" + uid, Progress.ToPlayerDictionary() },
            { "ranking/" + uid, Progress.ToRankingDictionary() }
        };
        FirebaseRest.Response r = await FirebaseRest.Send("PATCH", $"{dbUrl}/.json?auth={token}", SugaruJson.Serialize(updates));
        if (!r.Ok) throw new Exception(DbError(r));
        Debug.Log("Progreso guardado en Firebase.");
    }

    /// <summary>
    /// Se llama al terminar un nivel (ganado o perdido).
    /// Gana: marca completed, suma al total, guarda bestScore y desbloquea el siguiente.
    /// Pierde: solo cuenta el intento y guarda el puntaje de la partida.
    /// Devuelve true si se desbloqueó un nivel nuevo.
    /// </summary>
    public async Task<bool> ReportLevelResultAsync(int levelNumber, int score, bool won, int stars = 0)
    {
        if (Progress == null) return false;

        bool unlockedNew = false;
        LevelData level = Progress.GetLevel(levelNumber);
        level.timesPlayed++;
        Progress.currentRunScore = score;

        if (won)
        {
            level.state = LevelState.Completed;
            if (score > level.bestScore) level.bestScore = score;
            if (stars > level.stars) level.stars = stars;

            Progress.totalScore += score;
            Progress.lastCompletedLevel = Math.Max(Progress.lastCompletedLevel, levelNumber);

            if (levelNumber < PlayerProgress.TotalLevels)
            {
                LevelData next = Progress.GetLevel(levelNumber + 1);
                if (next.state == LevelState.Locked)
                {
                    next.state = LevelState.Unlocked;
                    unlockedNew = true;
                }
                Progress.maxUnlockedLevel = Math.Max(Progress.maxUnlockedLevel, levelNumber + 1);
            }
        }

        try
        {
            await SaveProgressAsync();
        }
        catch (Exception e)
        {
            Debug.LogError("No se pudo guardar el resultado: " + e.Message);
        }
        return unlockedNew;
    }

    // =====================================================================
    //  RANKING
    // =====================================================================

    /// <summary>Devuelve los mejores jugadores ordenados de mayor a menor puntaje total.</summary>
    public async Task<List<RankingEntry>> GetRankingAsync(int limit = 20)
    {
        var list = new List<RankingEntry>();
        if (!IsReady || CurrentUser == null) return list;
        string token = await TokenAsync();

        // Firebase ordena ascendente; limitToLast trae los N con más puntos
        string orderBy = FirebaseRest.Escape("\"totalScore\"");
        string url = dbUrl + "/ranking.json?orderBy=" + orderBy + "&limitToLast=" + limit + "&auth=" + token;
        FirebaseRest.Response r = await FirebaseRest.Send("GET", url);
        if (!r.Ok) throw new Exception(DbError(r));

        if (SugaruJson.Parse(r.text) is Dictionary<string, object> all)
        {
            foreach (var kv in all)
            {
                if (!(kv.Value is Dictionary<string, object> row)) continue;
                list.Add(new RankingEntry
                {
                    uid = kv.Key,
                    username = PlayerProgress.GetString(row, "username", "???"),
                    totalScore = PlayerProgress.GetInt(row, "totalScore", 0),
                    maxUnlockedLevel = PlayerProgress.GetInt(row, "maxUnlockedLevel", 1)
                });
            }
        }
        list.Sort((a, b) => b.totalScore.CompareTo(a.totalScore));
        return list;
    }

    // =====================================================================
    //  NAVEGACIÓN
    // =====================================================================

    public void GoToMenu() => SceneManager.LoadScene(menuScene);

    public void PlayLevel(int levelNumber)
    {
        if (Progress != null && !Progress.IsUnlocked(levelNumber))
        {
            Debug.LogWarning("El nivel " + levelNumber + " está bloqueado.");
            return;
        }
        SelectedLevel = levelNumber;
        SceneManager.LoadScene(LevelCatalog.SceneFor(levelNumber));
    }

    // =====================================================================
    //  ERRORES EN ESPAÑOL
    // =====================================================================

    private static string TranslateError(FirebaseRest.Response r)
    {
        if (r.networkError) return "Sin conexión a internet.";
        string code = FirebaseRest.AuthErrorCode(r);
        if (code.StartsWith("EMAIL_EXISTS")) return "Ese correo ya está registrado.";
        if (code.StartsWith("WEAK_PASSWORD")) return "La contraseña debe tener mínimo 6 caracteres.";
        if (code.StartsWith("INVALID_EMAIL")) return "El correo no es válido.";
        if (code.StartsWith("MISSING_EMAIL")) return "Escribe tu correo.";
        if (code.StartsWith("MISSING_PASSWORD")) return "Escribe tu contraseña.";
        if (code.StartsWith("INVALID_LOGIN_CREDENTIALS") || code.StartsWith("EMAIL_NOT_FOUND") || code.StartsWith("INVALID_PASSWORD"))
            return "Correo o contraseña incorrectos.";
        if (code.StartsWith("USER_DISABLED")) return "Esta cuenta está deshabilitada.";
        if (code.StartsWith("TOO_MANY_ATTEMPTS")) return "Demasiados intentos. Espera un momento.";
        if (code.StartsWith("OPERATION_NOT_ALLOWED")) return "Activa 'Correo/contraseña' en Firebase → Authentication.";
        if (code.Contains("API key not valid") || code.StartsWith("API_KEY")) return "La clave de Firebase no es válida (revisa FirebaseConfig).";
        return "Error: " + code;
    }

    private static string DbError(FirebaseRest.Response r)
    {
        if (r.networkError) return "Sin conexión a internet.";
        if (r.code == 401 || r.code == 403) return "Permiso denegado en la base de datos (revisa las reglas).";
        return "Error de la base de datos (" + r.code + "): " + r.text;
    }

    private static string Str(Dictionary<string, object> d, string key)
        => d != null && d.TryGetValue(key, out object v) && v != null ? v.ToString() : "";
}
