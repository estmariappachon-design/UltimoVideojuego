using UnityEngine;

/// <summary>
/// Marca un panel de la interfaz (mapa, ranking, pausa, ¡ganaste!, avisos, plantillas...).
///
/// En el EDITOR puedes dejarlo visible o escondido como quieras para editarlo
/// (usa la ventana "Torre de Pasteles → Paneles (ver y editar)").
/// Al darle PLAY, el componente "PanelesAlEmpezar" del Canvas lo pone como dice "Visible Al Jugar",
/// así el juego siempre empieza bien aunque hayas dejado todo a la vista.
/// </summary>
public class PanelInicio : MonoBehaviour
{
    public enum Grupo
    {
        [Tooltip("Pantallas del menú: solo se ve una a la vez")] Pantalla,
        [Tooltip("Ventanas que salen encima (pausa, ¡ganaste!, nivel del mapa, pedido)")] Ventana,
        [Tooltip("Versiones de una ventana (victoria / derrota)")] Variante,
        [Tooltip("Avisos y efectos que salen durante el juego")] Aviso,
        [Tooltip("Filas/objetos de ejemplo que el juego copia (filas del ticket, estadísticas...)")] Plantilla
    }

    [Tooltip("Nombre que se ve en la ventana de Paneles")]
    public string nombre = "Panel";
    public Grupo grupo = Grupo.Ventana;
    [Tooltip("¿Debe estar visible cuando empieza el juego?")]
    public bool visibleAlJugar = false;
}
