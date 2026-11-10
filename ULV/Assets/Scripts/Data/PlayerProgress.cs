using System;
using System.Collections.Generic;
using Firebase.Database;

/// <summary>
/// Estados posibles de un nivel (tal como pide el enunciado).
/// </summary>
public static class LevelState
{
    public const string Locked = "locked";
    public const string Unlocked = "unlocked";
    public const string Completed = "completed";
}

/// <summary>
/// Datos de un nivel individual guardados en Firebase:
/// players/{uid}/levels/levelXX
/// </summary>
[Serializable]
public class LevelData
{
    public string state = LevelState.Locked;
    public int bestScore;
    public int timesPlayed;
    public int stars;          // mejor cantidad de estrellas (0-3)

    public Dictionary<string, object> ToDictionary()
    {
        return new Dictionary<string, object>
        {
            { "state", state },
            { "bestScore", bestScore },
            { "timesPlayed", timesPlayed },
            { "stars", stars }
        };
    }
}

/// <summary>
/// Una fila del ranking público: ranking/{uid}
/// </summary>
[Serializable]
public class RankingEntry
{
    public string uid;
    public string username;
    public int totalScore;
    public int maxUnlockedLevel;
}

/// <summary>
/// Progreso completo del jugador. Se convierte manualmente a diccionarios porque
/// JsonUtility no sabe serializar diccionarios (el nodo "levels" lo necesita).
/// </summary>
[Serializable]
public class PlayerProgress
{
    public const int TotalLevels = 10;

    public string username;
    public string email;
    public int totalScore;          // Suma acumulada de puntos (se usa en el ranking)
    public int currentRunScore;     // Puntaje de la última partida jugada
    public int lastCompletedLevel;  // Último nivel completado
    public int maxUnlockedLevel = 1;
    public LevelData[] levels = new LevelData[TotalLevels];

    /// <summary>1 -> "level01", 10 -> "level10"</summary>
    public static string LevelKey(int levelNumber) => "level" + levelNumber.ToString("00");

    public static PlayerProgress CreateNew(string username, string email)
    {
        var p = new PlayerProgress
        {
            username = username,
            email = email,
            totalScore = 0,
            currentRunScore = 0,
            lastCompletedLevel = 0,
            maxUnlockedLevel = 1
        };

        for (int i = 0; i < TotalLevels; i++)
        {
            p.levels[i] = new LevelData
            {
                // Solo el nivel 1 está disponible al inicio
                state = i == 0 ? LevelState.Unlocked : LevelState.Locked
            };
        }
        return p;
    }

    public LevelData GetLevel(int levelNumber) => levels[levelNumber - 1];

    public bool IsUnlocked(int levelNumber)
    {
        return levelNumber >= 1 && levelNumber <= TotalLevels
               && GetLevel(levelNumber).state != LevelState.Locked;
    }

    // ---------- Conversión a Firebase ----------

    public Dictionary<string, object> ToPlayerDictionary()
    {
        var levelsDict = new Dictionary<string, object>();
        for (int i = 0; i < TotalLevels; i++)
            levelsDict[LevelKey(i + 1)] = levels[i].ToDictionary();

        return new Dictionary<string, object>
        {
            { "username", username },
            { "email", email },
            { "totalScore", totalScore },
            { "currentRunScore", currentRunScore },
            { "lastCompletedLevel", lastCompletedLevel },
            { "maxUnlockedLevel", maxUnlockedLevel },
            { "levels", levelsDict }
        };
    }

    public Dictionary<string, object> ToRankingDictionary()
    {
        return new Dictionary<string, object>
        {
            { "username", username },
            { "totalScore", totalScore },
            { "maxUnlockedLevel", maxUnlockedLevel }
        };
    }

    // ---------- Conversión desde Firebase ----------

    public static PlayerProgress FromSnapshot(DataSnapshot s, string fallbackEmail)
    {
        var p = CreateNew(GetString(s, "username", "jugador"), GetString(s, "email", fallbackEmail));

        p.totalScore = GetInt(s, "totalScore", 0);
        p.currentRunScore = GetInt(s, "currentRunScore", 0);
        p.lastCompletedLevel = GetInt(s, "lastCompletedLevel", 0);
        p.maxUnlockedLevel = Math.Max(1, GetInt(s, "maxUnlockedLevel", 1));

        DataSnapshot levelsSnap = s.Child("levels");
        for (int i = 0; i < TotalLevels; i++)
        {
            DataSnapshot ls = levelsSnap.Child(LevelKey(i + 1));
            if (!ls.Exists) continue;

            p.levels[i].state = GetString(ls, "state", p.levels[i].state);
            p.levels[i].bestScore = GetInt(ls, "bestScore", 0);
            p.levels[i].timesPlayed = GetInt(ls, "timesPlayed", 0);
            p.levels[i].stars = GetInt(ls, "stars", 0);
        }

        // Coherencia: todo nivel <= maxUnlockedLevel debe estar al menos desbloqueado
        for (int i = 0; i < p.maxUnlockedLevel && i < TotalLevels; i++)
        {
            if (p.levels[i].state == LevelState.Locked)
                p.levels[i].state = LevelState.Unlocked;
        }
        return p;
    }

    public static int GetInt(DataSnapshot s, string key, int defaultValue)
    {
        DataSnapshot c = s.Child(key);
        if (!c.Exists || c.Value == null) return defaultValue;
        try { return Convert.ToInt32(c.Value); }   // Firebase devuelve los números como long
        catch { return defaultValue; }
    }

    public static string GetString(DataSnapshot s, string key, string defaultValue)
    {
        DataSnapshot c = s.Child(key);
        return (c.Exists && c.Value != null) ? c.Value.ToString() : defaultValue;
    }
}
