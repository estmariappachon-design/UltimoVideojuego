/// <summary>
/// Tipo de meta (condición de victoria) de un nivel.
/// </summary>
public enum GoalType
{
    PassObstacles,   // Pasar N postes
    CollectCrumbs,   // Recoger N migas de pan
    SurviveTime      // Sobrevivir N segundos
}

/// <summary>
/// Parámetros con los que el generador procedural construye cada nivel.
/// </summary>
public class LevelConfig
{
    public string title;
    public string description;

    // Meta / condición de victoria
    public GoalType goal;
    public int goalAmount;

    // Velocidad y dificultad progresiva
    public float startSpeed;      // unidades/seg al empezar
    public float speedRamp;       // cuánto aumenta la velocidad por segundo
    public float maxSpeed;

    // Postes (obstáculos dobles con hueco)
    public float spacing;         // distancia entre obstáculos (unidades)
    public float gapSize;         // tamaño del hueco por donde pasa la paloma
    public float maxGapJump;      // cuánto puede cambiar la altura del hueco entre un poste y el siguiente
    public float movingChance;    // probabilidad de que el poste se mueva arriba/abajo
    public float moveAmplitude;   // amplitud del movimiento

    // Otros elementos procedurales
    public float hawkChance;      // probabilidad de que salga un halcón en vez de un poste
    public float crumbChance;     // probabilidad de que aparezca una miga de pan
}

/// <summary>
/// Tabla con la configuración de los 10 niveles.
/// Cambia aquí los números para balancear la dificultad.
/// </summary>
public static class LevelSettings
{
    private static readonly LevelConfig[] levels =
    {
        // 1
        new LevelConfig { title = "Primer vuelo", description = "Pasa 5 postes",
            goal = GoalType.PassObstacles, goalAmount = 5,
            startSpeed = 3f, speedRamp = 0f, maxSpeed = 3f,
            spacing = 6.5f, gapSize = 3.8f, maxGapJump = 1.5f, movingChance = 0f, moveAmplitude = 0f,
            hawkChance = 0f, crumbChance = 0.2f },
        // 2
        new LevelConfig { title = "La plaza", description = "Pasa 8 postes",
            goal = GoalType.PassObstacles, goalAmount = 8,
            startSpeed = 3.2f, speedRamp = 0.02f, maxSpeed = 3.6f,
            spacing = 6.2f, gapSize = 3.5f, maxGapJump = 1.8f, movingChance = 0f, moveAmplitude = 0f,
            hawkChance = 0f, crumbChance = 0.3f },
        // 3
        new LevelConfig { title = "Hora del pan", description = "Recoge 6 migas de pan",
            goal = GoalType.CollectCrumbs, goalAmount = 6,
            startSpeed = 3.2f, speedRamp = 0.02f, maxSpeed = 3.6f,
            spacing = 6.2f, gapSize = 3.5f, maxGapJump = 1.8f, movingChance = 0f, moveAmplitude = 0f,
            hawkChance = 0f, crumbChance = 0.8f },
        // 4
        new LevelConfig { title = "Postes inquietos", description = "Pasa 10 postes (algunos se mueven)",
            goal = GoalType.PassObstacles, goalAmount = 10,
            startSpeed = 3.4f, speedRamp = 0.03f, maxSpeed = 4f,
            spacing = 6f, gapSize = 3.4f, maxGapJump = 2f, movingChance = 0.4f, moveAmplitude = 0.8f,
            hawkChance = 0f, crumbChance = 0.3f },
        // 5
        new LevelConfig { title = "Cielo de halcones", description = "Sobrevive 30 segundos",
            goal = GoalType.SurviveTime, goalAmount = 30,
            startSpeed = 3.4f, speedRamp = 0.02f, maxSpeed = 4f,
            spacing = 6f, gapSize = 3.4f, maxGapJump = 2f, movingChance = 0.2f, moveAmplitude = 0.8f,
            hawkChance = 0.5f, crumbChance = 0.3f },
        // 6
        new LevelConfig { title = "Tejados", description = "Pasa 12 postes",
            goal = GoalType.PassObstacles, goalAmount = 12,
            startSpeed = 3.8f, speedRamp = 0.04f, maxSpeed = 4.6f,
            spacing = 5.8f, gapSize = 3.1f, maxGapJump = 2.2f, movingChance = 0.5f, moveAmplitude = 1f,
            hawkChance = 0.15f, crumbChance = 0.3f },
        // 7
        new LevelConfig { title = "Festín", description = "Recoge 12 migas de pan",
            goal = GoalType.CollectCrumbs, goalAmount = 12,
            startSpeed = 3.8f, speedRamp = 0.03f, maxSpeed = 4.6f,
            spacing = 5.8f, gapSize = 3.1f, maxGapJump = 2.2f, movingChance = 0.3f, moveAmplitude = 1f,
            hawkChance = 0.3f, crumbChance = 0.85f },
        // 8
        new LevelConfig { title = "Tormenta", description = "Sobrevive 40 segundos",
            goal = GoalType.SurviveTime, goalAmount = 40,
            startSpeed = 4f, speedRamp = 0.03f, maxSpeed = 5f,
            spacing = 5.6f, gapSize = 3f, maxGapJump = 2.2f, movingChance = 0.5f, moveAmplitude = 1.1f,
            hawkChance = 0.4f, crumbChance = 0.3f },
        // 9
        new LevelConfig { title = "La catedral", description = "Pasa 18 postes",
            goal = GoalType.PassObstacles, goalAmount = 18,
            startSpeed = 4.3f, speedRamp = 0.05f, maxSpeed = 5.5f,
            spacing = 5.6f, gapSize = 2.9f, maxGapJump = 2.4f, movingChance = 0.6f, moveAmplitude = 1.2f,
            hawkChance = 0.3f, crumbChance = 0.3f },
        // 10
        new LevelConfig { title = "Gran migración", description = "Sobrevive 60 segundos",
            goal = GoalType.SurviveTime, goalAmount = 60,
            startSpeed = 4.3f, speedRamp = 0.06f, maxSpeed = 6.5f,
            spacing = 5.6f, gapSize = 2.9f, maxGapJump = 2.4f, movingChance = 0.6f, moveAmplitude = 1.2f,
            hawkChance = 0.45f, crumbChance = 0.35f },
    };

    public static int Count => levels.Length;

    /// <summary>Configuración del nivel (1 a 10).</summary>
    public static LevelConfig Get(int levelNumber)
    {
        int index = UnityEngine.Mathf.Clamp(levelNumber - 1, 0, levels.Length - 1);
        return levels[index];
    }
}
