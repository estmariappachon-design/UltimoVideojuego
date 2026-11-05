using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Detecta un toque en pantalla (celular), clic (editor) o barra espaciadora,
/// ignorando los toques que caen sobre botones de la UI (ej. el botón de pausa).
/// Usa el Input Manager clásico: en Player Settings pon Active Input Handling = Both.
/// </summary>
public static class TapInput
{
    public static bool WasTapped()
    {
        // Celular
        if (Input.touchCount > 0)
        {
            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch t = Input.GetTouch(i);
                if (t.phase == TouchPhase.Began && !IsOverUI(t.fingerId))
                    return true;
            }
            return false;
        }

        // Editor / PC
        if (Input.GetMouseButtonDown(0) && !IsOverUI(-1)) return true;
        return Input.GetKeyDown(KeyCode.Space);
    }

    private static bool IsOverUI(int pointerId)
    {
        if (EventSystem.current == null) return false;
        return pointerId < 0
            ? EventSystem.current.IsPointerOverGameObject()
            : EventSystem.current.IsPointerOverGameObject(pointerId);
    }
}
