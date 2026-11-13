using System;
using UnityEngine;

/// <summary>Retos extra que puede pedir un cliente además de los pisos (opcionales).</summary>
public enum ChallengeType
{
    None,           // solo los sabores en orden
    PerfectDrops,   // "Al menos N capas perfectas"
    MinFinalWidth,  // "El pastel debe quedar al menos X% de ancho"
    NoMistakes      // "Sin errores en este pedido"
}

/// <summary>
/// CONFIGURACIÓN DEL JUEGO: Assets/Resources/LevelCatalog.asset
/// Selecciónalo y edita en el Inspector:
///  - Sabores (imagen de cada capa)          - Pastel quemado
///  - Clientes / animales (3 caras cada uno)  - Los 10 niveles (fondo, música, retos y dificultad)
/// En los números de retos: 0 = apagado.
/// </summary>
[CreateAssetMenu(fileName = "LevelCatalog", menuName = "Torre de Pasteles/Catálogo")]
public class LevelCatalog : ScriptableObject
{
    [Serializable]
    public class Flavor
    {
        public string name = "Fresa";
        [Tooltip("Imagen de la capa (horizontal; los 70 px de cada lado no se estiran)")]
        public Sprite layerSprite;
        public Color color = Color.white;
    }

    [Serializable]
    public class Customer
    {
        public string name = "Conejita";
        public Sprite normal;
        public Sprite happy;
        public Sprite angry;
        [TextArea] public string greeting = "¡Hola! ¿Me preparas este pastel?";
    }

    [Serializable]
    public class Entry
    {
        [Header("Nivel")]
        public string title = "Nivel";
        [Tooltip("Texto corto para el mapa de niveles")]
        [TextArea] public string description = "";
        [Tooltip("RETO del nivel: se muestra arriba durante TODA la partida")]
        [TextArea] public string challengeDescription = "¡Apila los pasteles!";
        public Sprite thumbnail;
        public Sprite background;
        [Tooltip("Vacío = música por defecto del AudioManager")]
        public AudioClip music;

        [Header("Pedidos")]
        [Min(1)] public int ordersToWin = 3;
        [Min(1)] public int floorsPerOrder = 8;
        [Min(1)] public int hearts = 3;
        [Tooltip("Cuántos sabores distintos usan los pedidos")]
        [Range(2, 8)] public int flavorCount = 3;

        [Header("Capas")]
        public float baseWidth = 3.4f;
        public float startSpeed = 2.4f;
        public float speedPerFloor = 0.07f;
        public float speedPerOrder = 0.25f;
        public float maxSpeed = 8f;
        [Tooltip("Qué tan cerca hay que soltarla para que sea PERFECTA")]
        public float perfectTolerance = 0.13f;

        [Header("Brisa / viento (se lleva los pasteles mientras caen)")]
        public float windStrength = 0f;
        public float windChangeEvery = 4f;

        [Header("Medidor de precisión (barra con zona VERDE y una línea que se mueve)")]
        [Tooltip("Si tocas cuando la línea está en el verde, el pastel cae PERFECTO en su lugar")]
        public bool timingMeter = false;
        [Tooltip("Tamaño de la zona verde (0.2 = 20% de la barra)")]
        [Range(0.05f, 0.6f)] public float meterGreenSize = 0.22f;
        [Tooltip("Qué tan rápido va la línea (recorridos por segundo)")]
        public float meterSpeed = 0.8f;

        [Header("Pasteles quemados y sabores que NO están en el pedido")]
        [Tooltip("Probabilidad de que la capa que sale sea una que NO se debe poner")]
        [Range(0f, 0.6f)] public float decoyChance = 0f;
        [Tooltip("De esas, qué parte son quemadas (el resto son sabores que no están en el pedido)")]
        [Range(0f, 1f)] public float burntRatio = 1f;
        [Tooltip("Cuántas veces cruza la pantalla antes de irse sola")]
        [Min(1)] public int decoyPasses = 2;

        [Header("Ventilador (ráfaga muy fuerte que mueve la torre)")]
        [Tooltip("Cada cuántos segundos se enciende (0 = apagado)")]
        public float fanEvery = 0f;
        public float fanForce = 5f;
        public float fanDuration = 2.6f;

        [Header("Moscas")]
        [Tooltip("Cada cuántos segundos llega una mosca (0 = no hay)")]
        public float flyEvery = 0f;
        public float flySpeed = 1.6f;
        [Min(1)] public int maxFlies = 1;
        [Tooltip("0 = llegan desde los lados de la pantalla. Más de 0 = aparecen CERCA del pastel, a esta distancia")]
        public float flySpawnDistance = 0f;

        [Header("Temblor de la mesa")]
        public float tremorEvery = 0f;
        public float tremorDuration = 4f;
        [Tooltip("Qué tan rápido se inclina la torre (grados por segundo)")]
        public float tremorStrength = 5f;

        [Header("Apagón (se van las luces)")]
        public float blackoutEvery = 0f;
        public float blackoutDuration = 4f;
        [Tooltip("Segundos de juego antes del PRIMER apagón de cada pedido (0 = al azar)")]
        public float blackoutFirstDelay = 0f;
        [Tooltip("Qué tan oscuro queda también el círculo de luz (0 = luz normal, 0.7 = luz muy tenue)")]
        [Range(0f, 0.9f)] public float blackoutLightDim = 0f;

        [Header("Nube de harina")]
        public float flourEvery = 0f;
        public float flourDuration = 3f;

        [Header("Superficie que se mueve")]
        public float movingPlateSpeed = 0f;

        [Header("El cliente cambia el pedido a mitad de camino")]
        [Range(0f, 1f)] public float orderChangeChance = 0f;
        [Tooltip("Cuántos pisos YA PUESTOS puede cambiar el cliente (hay que tocarlos para quitarlos y rehacerlos)")]
        [Min(0)] public int orderChangeFloors = 0;
        [Tooltip("Tocar un pastel que NO va mientras se mueve lo bota (en vez de esperar a que pase)")]
        public bool tapToDiscard = false;

        [Header("Opcionales")]
        [Tooltip("Cada cuántos segundos la capa cambia de sabor mientras se mueve")]
        public float flavorShiftInterval = 0f;
        [Tooltip("CRONÓMETRO: segundos para entregar cada pedido (0 = sin cronómetro)")]
        public float patienceSeconds = 0f;
        [Range(0, 1)] public float challengeChance = 0f;
        public ChallengeType[] challenges = new ChallengeType[0];
    }

    [Header("Escena del juego")]
    public string gameScene = "GameScene";

    [Header("Sabores")]
    public Flavor[] flavors;

    [Header("Pastel quemado")]
    public Sprite burntLayerSprite;
    public Sprite burntFaceSprite;

    [Header("Clientes (animales)")]
    public Customer[] customers;

    [Header("Niveles")]
    public Entry[] levels;

    private static LevelCatalog cached;

    public static LevelCatalog Instance
    {
        get
        {
            if (cached == null) cached = Resources.Load<LevelCatalog>("LevelCatalog");
            return cached;
        }
    }

    public static int Count => Instance != null && Instance.levels != null ? Instance.levels.Length : 0;

    public static Entry Get(int levelNumber)
    {
        if (Count == 0) return null;
        return Instance.levels[Mathf.Clamp(levelNumber - 1, 0, Count - 1)];
    }

    public static string SceneFor(int levelNumber) => Instance != null ? Instance.gameScene : "GameScene";

    public static string TitleFor(int levelNumber)
    {
        Entry e = Get(levelNumber);
        return e != null ? e.title : "Nivel " + levelNumber;
    }
}
