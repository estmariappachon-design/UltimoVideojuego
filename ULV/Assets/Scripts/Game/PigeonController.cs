using System;
using UnityEngine;

/// <summary>
/// La paloma: salta al tocar la pantalla, cae por gravedad, se inclina según su velocidad
/// y muere al tocar cualquier objeto con el tag "Obstacle".
/// Requiere: Rigidbody2D (Dynamic) + CircleCollider2D (o CapsuleCollider2D).
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PigeonController : MonoBehaviour
{
    [Header("Salto")]
    public float jumpVelocity = 6.5f;
    public float maxFallSpeed = -10f;
    public float maxY = 4.6f;            // techo invisible

    [Header("Inclinación")]
    public float tiltUp = 25f;
    public float tiltDown = -70f;
    public float tiltSmooth = 8f;

    [Header("Opcional")]
    public Animator animator;            // trigger "Flap" al saltar
    public AudioSource flapSound;
    public AudioSource hitSound;

    public event Action OnDied;
    public bool IsAlive { get; private set; } = true;

    private Rigidbody2D rb;
    private bool canControl;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.simulated = false; // quieta hasta que empiece el nivel
    }

    /// <summary>Lo llama el LevelManager con el primer toque.</summary>
    public void Begin()
    {
        rb.simulated = true;
        canControl = true;
        Jump();
    }

    /// <summary>Congela la paloma (al ganar).</summary>
    public void Freeze()
    {
        canControl = false;
        rb.simulated = false;
    }

    private void Update()
    {
        if (!canControl || Time.timeScale == 0f) return; // muerta, ganó o en pausa

        if (TapInput.WasTapped()) Jump();

        Vector2 v = GetVelocity();

        // Velocidad máxima de caída
        if (v.y < maxFallSpeed) v.y = maxFallSpeed;

        // Techo
        if (transform.position.y > maxY)
        {
            transform.position = new Vector3(transform.position.x, maxY, transform.position.z);
            if (v.y > 0) v.y = 0;
        }
        SetVelocity(v);

        // Inclinación según la velocidad vertical
        float targetAngle = Mathf.Lerp(tiltDown, tiltUp, Mathf.InverseLerp(maxFallSpeed, jumpVelocity, v.y));
        Quaternion target = Quaternion.Euler(0, 0, targetAngle);
        transform.rotation = Quaternion.Lerp(transform.rotation, target, tiltSmooth * Time.deltaTime);
    }

    private void Jump()
    {
        SetVelocity(new Vector2(0, jumpVelocity));
        if (animator != null) animator.SetTrigger("Flap");
        if (flapSound != null) flapSound.Play();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsAlive) return;

        if (other.CompareTag("Obstacle"))
        {
            Die();
            return;
        }

        Collectible crumb = other.GetComponent<Collectible>();
        if (crumb != null)
        {
            crumb.Collect();
            return;
        }

        ScoreZone zone = other.GetComponent<ScoreZone>();
        if (zone != null) zone.Pass();
    }

    private void Die()
    {
        IsAlive = false;
        canControl = false;
        if (hitSound != null) hitSound.Play();
        SetVelocity(new Vector2(0, 2f)); // pequeño rebote antes de caer
        OnDied?.Invoke();
    }

    // Unity 6 renombró velocity -> linearVelocity
    private Vector2 GetVelocity()
    {
#if UNITY_6000_0_OR_NEWER
        return rb.linearVelocity;
#else
        return rb.velocity;
#endif
    }

    private void SetVelocity(Vector2 v)
    {
#if UNITY_6000_0_OR_NEWER
        rb.linearVelocity = v;
#else
        rb.velocity = v;
#endif
    }
}
