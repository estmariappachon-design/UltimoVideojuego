using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Firebase;
using Firebase.Auth;
using Firebase.Database;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Administrador único de Firebase (Singleton). Vive en LoginScene y sobrevive entre escenas.
/// Se encarga de: inicializar Firebase, registrar, iniciar sesión, cargar/guardar progreso,
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
    public FirebaseUser CurrentUser => auth != null ? auth.CurrentUser : null;
    public PlayerProgress Progress { get; private set; }

    /// <summary>Nivel que el jugador eligió en el menú (lo lee el LevelManager).</summary>
    public int SelectedLevel { get; set; } = 1;

    private FirebaseAuth auth;
    private DatabaseReference db;

    private async void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        await InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        try
        {
            DependencyStatus status = await FirebaseApp.CheckAndFixDependenciesAsync();
            if (status != DependencyStatus.Available)
            {
                InitError = "No se pudieron resolver las dependencias de Firebase: " + status;
                Debug.LogError(InitError);
                return;
            }

            // Sin caché local: así siempre leemos el dato real del servidor.
            // Debe llamarse ANTES de cualquier otro uso de la base de datos.
            FirebaseDatabase.DefaultInstance.SetPersistenceEnabled(false);

            auth = FirebaseAuth.DefaultInstance;
            db = FirebaseDatabase.DefaultInstance.RootReference;
            IsReady = true;
            Debug.Log("Firebase inicializado correctamente.");
        }
        catch (Exception e)
        {
            InitError = "Error inicializando Firebase: " + e.Message;
            Debug.LogError(InitError);
        }
    }

    // =====================================================================
    //  AUTENTICACIÓN
    // =====================================================================

    /// <summary>Registra un usuario. Devuelve null si todo salió bien, o el mensaje de error.</summary>
    public async Task<string> RegisterAsync(string username, string email, string password)
    {
        if (!IsReady) return "Firebase todavía no está listo.";

        try
        {
            AuthResult result = await auth.CreateUserWithEmailAndPasswordAsync(email, password);

            // Guardamos el nombre visible también en el perfil de Authentication
            await result.User.UpdateUserProfileAsync(new UserProfile { DisplayName = username });

            // Registro inicial: nivel 1 desbloqueado, el resto bloqueado, puntaje 0
            Progress = PlayerProgress.CreateNew(username, email);
            await SaveProgressAsync();
            return null;
        }
        catch (Exception e)
        {
            return TranslateError(e);
        }
    }

    /// <summary>Inicia sesión y carga el progreso. Devuelve null si todo salió bien.</summary>
    public async Task<string> LoginAsync(string email, string password)
    {
        if (!IsReady) return "Firebase todavía no está listo.";

        try
        {
            await auth.SignInWithEmailAndPasswordAsync(email, password);
            await LoadProgressAsync();
            return null;
        }
        catch (Exception e)
        {
            return TranslateError(e);
        }
    }

    /// <summary>Si quedó una sesión abierta en el dispositivo, la reutiliza.</summary>
    public async Task<bool> TryAutoLoginAsync()
    {
        if (!IsReady || CurrentUser == null) return false;
        try
        {
            await LoadProgressAsync();
            return true;
        }
        catch (Exception e)
        {
            Debug.LogWarning("Auto-login falló: " + e.Message);
            return false;
        }
    }

    public void Logout()
    {
        if (auth != null) auth.SignOut();
        Progress = null;
        SceneManager.LoadScene(loginScene);
    }

    // =====================================================================
    //  PROGRESO
    // =====================================================================

    /// <summary>Lee players/{uid} y lo convierte en PlayerProgress.</summary>
    public async Task LoadProgressAsync()
    {
        if (CurrentUser == null) throw new Exception("No hay usuario autenticado.");

        string uid = CurrentUser.UserId;
        DataSnapshot snap = await db.Child("players").Child(uid).GetValueAsync();

        if (snap.Exists)
        {
            Progress = PlayerProgress.FromSnapshot(snap, CurrentUser.Email);
        }
        else
        {
            // La cuenta existe en Auth pero no tiene datos (p. ej. se borró la BD): se crea de nuevo
            string name = !string.IsNullOrEmpty(CurrentUser.DisplayName)
                ? CurrentUser.DisplayName
                : CurrentUser.Email.Split('@')[0];
            Progress = PlayerProgress.CreateNew(name, CurrentUser.Email);
            await SaveProgressAsync();
        }

        Debug.Log($"Progreso cargado: {Progress.username} | total {Progress.totalScore} | nivel máx {Progress.maxUnlockedLevel}");
    }

    /// <summary>
    /// Guarda en UNA sola escritura atómica el nodo privado (players/uid)
    /// y el nodo público del ranking (ranking/uid).
    /// </summary>
    public async Task SaveProgressAsync()
    {
        if (CurrentUser == null || Progress == null) return;

        string uid = CurrentUser.UserId;
        var updates = new Dictionary<string, object>
        {
            { "players/" + uid, Progress.ToPlayerDictionary() },
            { "ranking/" + uid, Progress.ToRankingDictionary() }
        };
        await db.UpdateChildrenAsync(updates);
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
        if (!IsReady) return list;

        // Firebase ordena ascendente; LimitToLast trae los N con más puntos
        DataSnapshot snap = await db.Child("ranking")
                                    .OrderByChild("totalScore")
                                    .LimitToLast(limit)
                                    .GetValueAsync();

        foreach (DataSnapshot child in snap.Children)
        {
            list.Add(new RankingEntry
            {
                uid = child.Key,
                username = PlayerProgress.GetString(child, "username", "???"),
                totalScore = PlayerProgress.GetInt(child, "totalScore", 0),
                maxUnlockedLevel = PlayerProgress.GetInt(child, "maxUnlockedLevel", 1)
            });
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

    private static string TranslateError(Exception e)
    {
        Exception baseEx = e.GetBaseException();
        if (baseEx is FirebaseException fe)
        {
            switch ((AuthError)fe.ErrorCode)
            {
                case AuthError.EmailAlreadyInUse: return "Ese correo ya está registrado.";
                case AuthError.WeakPassword: return "La contraseña debe tener mínimo 6 caracteres.";
                case AuthError.InvalidEmail: return "El correo no es válido.";
                case AuthError.MissingEmail: return "Escribe tu correo.";
                case AuthError.MissingPassword: return "Escribe tu contraseña.";
                case AuthError.WrongPassword:
                case AuthError.UserNotFound: return "Correo o contraseña incorrectos.";
                case AuthError.NetworkRequestFailed: return "Sin conexión a internet.";
                case AuthError.TooManyRequests: return "Demasiados intentos. Espera un momento.";
            }
            return "Error: " + fe.Message;
        }
        return "Error: " + baseEx.Message;
    }
}
