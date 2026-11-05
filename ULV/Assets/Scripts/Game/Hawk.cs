using UnityEngine;

/// <summary>
/// Enemigo halcón: vuela hacia la izquierda más rápido que el escenario
/// y ondula arriba/abajo. Su velocidad y ondulación son procedurales.
///
/// Prefab: SpriteRenderer + CircleCollider2D (isTrigger) + Scroller + Hawk, tag "Obstacle".
/// </summary>
[RequireComponent(typeof(Scroller))]
public class Hawk : MonoBehaviour
{
    private float baseY;
    private float amplitude;
    private float frequency;
    private float time;

    public void Setup(float y, float extraSpeed, float waveAmplitude, float waveFrequency)
    {
        baseY = y;
        amplitude = waveAmplitude;
        frequency = waveFrequency;
        GetComponent<Scroller>().extraSpeed = extraSpeed;
        transform.position = new Vector3(transform.position.x, y, transform.position.z);
    }

    private void Update()
    {
        if (LevelManager.Instance == null || LevelManager.Instance.CurrentSpeed <= 0f) return;

        time += Time.deltaTime;
        Vector3 p = transform.position;
        p.y = baseY + Mathf.Sin(time * frequency) * amplitude;
        transform.position = p;
    }
}
