using UnityEngine;

/// <summary>
/// GENERACIÓN PROCEDURAL del nivel.
/// Nada de los obstáculos está puesto a mano: este script decide, mientras se juega,
///  - qué sale (poste o halcón)            -> probabilidad hawkChance
///  - la altura del hueco                  -> regla: no puede saltar más de maxGapJump respecto al anterior
///                                            (así siempre es posible pasar)
///  - el tamaño del hueco                  -> gapSize ± variación, y se encoge con el tiempo
///  - si el poste se mueve y cuánto        -> movingChance, moveAmplitude
///  - velocidad y ondulación de halcones   -> aleatorio dentro de rangos
///  - dónde aparecen las migas de pan      -> crumbChance, dentro del hueco o entre postes
///  - cada cuánto aparece algo             -> distancia fija / velocidad actual, con variación
/// Usa una semilla (seed): con la misma semilla se genera exactamente el mismo nivel.
/// </summary>
public class ProceduralSpawner : MonoBehaviour
{
    [Header("Prefabs")]
    public ObstaclePair obstaclePrefab;
    public Hawk hawkPrefab;
    public Collectible crumbPrefab;

    [Header("Límites del mundo (unidades)")]
    public float spawnX = 11f;
    public float minGapCenter = -1.8f;
    public float maxGapCenter = 2.4f;
    public float hawkMinY = -2.5f;
    public float hawkMaxY = 3.8f;
    public float minGapSize = 2.4f;

    [Header("Semilla (0 = aleatoria cada partida)")]
    public int fixedSeed = 0;

    public int Seed { get; private set; }

    private LevelConfig cfg;
    private System.Random rng;
    private bool running;
    private float distanceToNext;
    private float lastGapCenter;
    private float elapsed;

    public void Begin(LevelConfig config, int levelNumber)
    {
        cfg = config;
        Seed = fixedSeed != 0 ? fixedSeed : System.Environment.TickCount ^ (levelNumber * 7919);
        rng = new System.Random(Seed);
        lastGapCenter = (minGapCenter + maxGapCenter) / 2f;
        distanceToNext = 0f;   // el primer obstáculo sale de inmediato (en spawnX, fuera de cámara)
        elapsed = 0f;
        running = true;
        Debug.Log($"Nivel {levelNumber} generado con semilla {Seed}");
    }

    public void Stop()
    {
        running = false;
    }

    private void Update()
    {
        if (!running || LevelManager.Instance == null) return;

        float speed = LevelManager.Instance.CurrentSpeed;
        if (speed <= 0f) return;

        elapsed += Time.deltaTime;

        // Se mide en distancia recorrida, no en tiempo: así el espacio entre
        // obstáculos es justo aunque la velocidad aumente.
        distanceToNext -= speed * Time.deltaTime;
        if (distanceToNext <= 0f)
        {
            SpawnNext();
            distanceToNext = cfg.spacing * Range(0.85f, 1.15f);
        }
    }

    private void SpawnNext()
    {
        if (hawkPrefab != null && Chance(cfg.hawkChance))
            SpawnHawk();
        else
            SpawnObstacle();
    }

    private void SpawnObstacle()
    {
        // Regla: el nuevo hueco no puede estar demasiado lejos del anterior
        float center = lastGapCenter + Range(-cfg.maxGapJump, cfg.maxGapJump);
        center = Mathf.Clamp(center, minGapCenter, maxGapCenter);
        lastGapCenter = center;

        // El hueco se cierra un poco a medida que avanza el nivel (dificultad progresiva)
        float shrink = Mathf.Min(elapsed * 0.005f, 0.4f);
        float gap = Mathf.Max(minGapSize, cfg.gapSize + Range(-0.25f, 0.25f) - shrink);

        bool moving = Chance(cfg.movingChance);
        float amp = moving ? cfg.moveAmplitude * Range(0.6f, 1f) : 0f;
        // Que al moverse no se salga de los límites
        if (moving) amp = Mathf.Min(amp, Mathf.Min(center - minGapCenter, maxGapCenter - center) + 0.5f);

        ObstaclePair pair = Instantiate(obstaclePrefab, new Vector3(spawnX, center, 0f), Quaternion.identity);
        pair.Setup(center, gap, amp, Range(1.2f, 2.2f), Range(0f, Mathf.PI * 2f));

        // Miga de pan: dentro del hueco (fácil) o entre este poste y el siguiente (arriesgada)
        if (crumbPrefab != null && Chance(cfg.crumbChance))
        {
            bool between = Chance(0.5f);
            float x = between ? spawnX + cfg.spacing * 0.5f : spawnX;
            float y = between
                ? Mathf.Clamp(center + Range(-1.2f, 1.2f), minGapCenter, maxGapCenter)
                : center;
            Instantiate(crumbPrefab, new Vector3(x, y, 0f), Quaternion.identity);
        }
    }

    private void SpawnHawk()
    {
        float y = Range(hawkMinY, hawkMaxY);
        Hawk hawk = Instantiate(hawkPrefab, new Vector3(spawnX, y, 0f), Quaternion.identity);
        hawk.Setup(y, Range(1.2f, 2.8f), Range(0f, 1.3f), Range(1.5f, 3f));

        // A veces deja una miga detrás del halcón
        if (crumbPrefab != null && Chance(cfg.crumbChance * 0.6f))
        {
            float cy = Range(minGapCenter, maxGapCenter);
            Instantiate(crumbPrefab, new Vector3(spawnX + cfg.spacing * 0.5f, cy, 0f), Quaternion.identity);
        }
    }

    // ---- Helpers aleatorios con semilla ----
    private float Range(float min, float max) => min + (float)rng.NextDouble() * (max - min);
    private bool Chance(float probability) => rng.NextDouble() < probability;
}
