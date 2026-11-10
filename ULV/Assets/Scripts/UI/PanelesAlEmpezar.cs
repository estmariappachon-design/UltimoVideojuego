using UnityEngine;

/// <summary>
/// Va en el Canvas. Al empezar el juego (antes que cualquier otro script) muestra u oculta cada
/// PanelInicio según su casilla "Visible Al Jugar". Gracias a esto puedes dejar todos los paneles
/// visibles en la escena mientras los editas y el juego igual arranca correctamente.
/// </summary>
[DefaultExecutionOrder(-1000)]
public class PanelesAlEmpezar : MonoBehaviour
{
    private void Awake()
    {
        foreach (PanelInicio p in GetComponentsInChildren<PanelInicio>(true))
            if (p.gameObject.activeSelf != p.visibleAlJugar) p.gameObject.SetActive(p.visibleAlJugar);
    }
}
