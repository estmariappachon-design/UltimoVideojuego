using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Zona invisible que cubre la pantalla: TOCAR = soltar la capa, espantar moscas o sujetar la torre.
/// Los botones (pausa, etc.) quedan encima y no sueltan capas.
/// </summary>
public class TapZone : MonoBehaviour, IPointerDownHandler
{
    public void OnPointerDown(PointerEventData eventData)
    {
        if (StackGame.Instance != null) StackGame.Instance.OnTap(eventData.position);
    }
}
