using UnityEngine;

/// <summary>
/// Mueve el objeto hacia la izquierda a la velocidad actual del nivel
/// y lo destruye cuando sale de la pantalla.
/// </summary>
public class Scroller : MonoBehaviour
{
    public float destroyX = -14f;
    [Tooltip("Multiplicador sobre la velocidad del nivel (1 = igual al mundo)")]
    public float speedMultiplier = 1f;
    [Tooltip("Velocidad extra propia (ej. halcones)")]
    public float extraSpeed = 0f;

    private void Update()
    {
        float speed = LevelManager.Instance != null ? LevelManager.Instance.CurrentSpeed : 0f;
        if (speed <= 0f) return; // juego detenido

        transform.position += Vector3.left * (speed * speedMultiplier + extraSpeed) * Time.deltaTime;

        if (transform.position.x < destroyX)
            Destroy(gameObject);
    }
}
