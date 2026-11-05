using UnityEngine;

/// <summary>
/// Miga de pan que la paloma recoge para ganar puntos.
/// Prefab: SpriteRenderer + CircleCollider2D (isTrigger) + Scroller + Collectible. SIN tag.
/// </summary>
[RequireComponent(typeof(Scroller))]
public class Collectible : MonoBehaviour
{
    public int points = 25;
    public float bobAmplitude = 0.15f;
    public float bobSpeed = 4f;
    public AudioClip pickupSound;

    private float baseY;
    private float t;
    private bool collected;

    private void Start()
    {
        baseY = transform.position.y;
        t = Random.value * 10f;
    }

    private void Update()
    {
        // Pequeño movimiento de flotación
        t += Time.deltaTime * bobSpeed;
        Vector3 p = transform.position;
        p.y = baseY + Mathf.Sin(t) * bobAmplitude;
        transform.position = p;
    }

    public void Collect()
    {
        if (collected) return;
        collected = true;

        if (pickupSound != null) AudioSource.PlayClipAtPoint(pickupSound, transform.position);
        if (LevelManager.Instance != null) LevelManager.Instance.CrumbCollected(points);
        Destroy(gameObject);
    }
}
