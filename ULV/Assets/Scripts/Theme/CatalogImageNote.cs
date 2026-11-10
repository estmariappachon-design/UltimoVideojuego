using UnityEngine;

/// <summary>
/// Nota para objetos cuya imagen la pone el JUEGO desde el LevelCatalog al jugar
/// (fondo de cada nivel, animales, sabores de pastel, miniaturas).
/// No hace nada al jugar: solo avisa en el Inspector dónde cambiar esa imagen.
/// </summary>
public class CatalogImageNote : MonoBehaviour
{
    public enum Source { FondoDeNivel, Cliente, SaborDePastel, MiniaturaDeNivel }
    public Source source;
}
