using UnityEngine;

/// <summary>
/// VENTILADOR (nivel 4): objeto de la escena. Muévelo a donde quieras; sopla hacia el pastel.
/// Lo controla StackGame: apagado → aviso (gira y tiembla) → ráfaga.
/// </summary>
public class FanHazard : MonoBehaviour
{
    public enum Mode { Idle, Warning, Blowing }

    public Transform blades;
    public float idleSpin = 40f;
    public float warningSpin = 500f;
    public float blowingSpin = 1500f;

    public Mode CurrentMode { get; private set; }

    private float spin;
    private Vector3 home;

    private void Awake()
    {
        home = transform.localPosition;
    }

    public void SetMode(Mode mode)
    {
        CurrentMode = mode;
        if (mode == Mode.Idle) transform.localPosition = home;
    }

    /// <summary>Hacia dónde sopla: -1 = izquierda, 1 = derecha.</summary>
    public float Direction(float towerX) => transform.position.x > towerX ? -1f : 1f;

    private void Update()
    {
        float target = CurrentMode == Mode.Blowing ? blowingSpin : CurrentMode == Mode.Warning ? warningSpin : idleSpin;
        spin = Mathf.MoveTowards(spin, target, 1800f * Time.deltaTime);
        if (blades != null) blades.Rotate(0f, 0f, -spin * Time.deltaTime);

        if (CurrentMode == Mode.Warning)
            transform.localPosition = home + (Vector3)Random.insideUnitCircle * 0.04f;
        else if (CurrentMode == Mode.Blowing)
            transform.localPosition = home + new Vector3(0f, Mathf.Sin(Time.time * 40f) * 0.02f, 0f);
    }
}
