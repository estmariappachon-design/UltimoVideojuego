using UnityEngine;

/// <summary>
/// (Opcional) Fondo procedural: nubes/edificios con tamaño, altura,
/// velocidad (parallax) y separación aleatorios. Solo decorativo.
/// Prefab de la decoración: SpriteRenderer + Scroller (sin collider).
/// </summary>
public class BackgroundDecorSpawner : MonoBehaviour
{
    public Scroller[] decorPrefabs;     // ej. 3 tipos de nube
    public float spawnX = 12f;
    public float minY = 1f, maxY = 4.5f;
    public float minScale = 0.6f, maxScale = 1.6f;
    public float minParallax = 0.2f, maxParallax = 0.6f; // más lento = más lejos
    public float minDistance = 3f, maxDistance = 7f;
    public int prewarmCount = 4;        // decoraciones ya visibles al empezar

    private float distanceToNext;

    private void Start()
    {
        for (int i = 0; i < prewarmCount; i++)
            Spawn(Random.Range(-10f, spawnX));
    }

    private void Update()
    {
        if (LevelManager.Instance == null || decorPrefabs.Length == 0) return;
        float speed = LevelManager.Instance.CurrentSpeed;
        if (speed <= 0f) return;

        distanceToNext -= speed * 0.4f * Time.deltaTime;
        if (distanceToNext <= 0f)
        {
            Spawn(spawnX);
            distanceToNext = Random.Range(minDistance, maxDistance);
        }
    }

    private void Spawn(float x)
    {
        Scroller prefab = decorPrefabs[Random.Range(0, decorPrefabs.Length)];
        Scroller s = Instantiate(prefab, new Vector3(x, Random.Range(minY, maxY), 5f), Quaternion.identity, transform);
        s.transform.localScale *= Random.Range(minScale, maxScale);
        s.speedMultiplier = Random.Range(minParallax, maxParallax);
    }
}
