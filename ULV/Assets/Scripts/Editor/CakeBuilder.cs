using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Menú "Torre de Pasteles" de la barra superior de Unity.
/// Crea el TemaVisual (imágenes de la interfaz), el catálogo (sabores, clientes y 10 niveles con sus retos),
/// los prefabs y las escenas LoginScene, MainMenu y GameScene, todo conectado.
/// Después todo se puede editar a mano en el Inspector o en la escena.
///
/// IMPORTANTE: si ya cambiaste imágenes en el TemaVisual o en el LevelCatalog, volver a ejecutar
/// estos menús NO las borra (solo rellena las casillas vacías). Las escenas sí se REEMPLAZAN.
/// </summary>
public static class CakeBuilder
{
    private const string SP = "Assets/Sprites/";
    private const string PF = "Assets/Prefabs/";
    private const string PFU = "Assets/Prefabs/UI/";
    private const string SC = "Assets/Scenes/";
    private const string SFX = "Assets/Sonidos/";
    private const string ThemePath = "Assets/Resources/TemaVisual.asset";
    private const string CatalogPath = "Assets/Resources/LevelCatalog.asset";

    // Paleta cute
    private static readonly Color Cream = new Color(1f, 0.97f, 0.94f, 0.97f);
    private static readonly Color Brown = new Color(0.47f, 0.27f, 0.33f);
    private static readonly Color Pink = new Color(1f, 0.55f, 0.72f);
    private static readonly Color Green = new Color(0.45f, 0.8f, 0.62f);
    private static readonly Color Dim = new Color(0.35f, 0.18f, 0.28f, 0.6f);
    private static readonly Color SkyPink = new Color(1f, 0.88f, 0.92f);
    private static readonly Color MessagePink = new Color(0.86f, 0.33f, 0.52f);
    private static readonly Color ButtonText = new Color(0.85f, 0.34f, 0.52f);

    private static readonly string[] FlavorFiles = { "fresa", "chocolate", "vainilla", "menta", "arandano", "limon", "algodon", "naranja" };
    private static readonly string[] FlavorNames = { "Fresa", "Chocolate", "Vainilla", "Menta", "Arándano", "Limón", "Algodón", "Naranja" };
    private static readonly string[] CustomerFiles = { "gatito", "conejita", "panda", "perrito" };
    private static readonly string[] CustomerNames = { "Gatito", "Conejita", "Pandita", "Perrito" };
    private static readonly string[] CustomerGreetings =
    {
        "Miau~ ¡quiero un pastel de fresas bien alto!",
        "¡Hola! Es el cumpleaños de mi abuelita, ¿me ayudas?",
        "Tengo mucha hambre... ¡ese pastel se ve delicioso!",
        "¡Guau! Es para una fiesta sorpresa, ¡shhh!"
    };
    // Imágenes de animales que traía el juego (se reemplazan por los personajes Sugaru)
    private static readonly string[] BuiltInCustomerPrefixes = { "conejita_", "gatito_", "osito_", "pollito_", "pinguina_", "panda_", "perrito_" };

    /// <summary>true = "todo con el estilo Sugaru": reemplaza también las imágenes que habías puesto en TemaVisual y LevelCatalog.</summary>
    private static bool resetStyle;
    private static readonly string[] Titles =
    {
        "Pastelería Rosa", "Café Menta", "Horno Travieso", "El Ventilador", "Picnic con Moscas",
        "Mesa Temblorosa", "Apagón Dulce", "El Cliente Exigente", "Nube de Harina", "Tormenta de Pasteles"
    };
    // Títulos que traía el juego antes (si tu nivel tiene uno de estos, se cambia por el nuevo; si le pusiste otro, se respeta)
    private static readonly string[] OldDefaultTitles =
    {
        "Pastelería Rosa", "Café Menta", "Horno Travieso", "El Ventilador", "Picnic con Moscas",
        "Mesa Temblorosa", "Apagón Dulce", "Tormenta de Pasteles", "Nube de Harina", "El Cliente Exigente"
    };

    // =====================================================================
    //  MENÚS
    // =====================================================================

    [MenuItem("Torre de Pasteles/2. Crear prefabs", priority = 2)]
    public static void BuildPrefabs()
    {
        EnsureFolder("Assets", "Prefabs");
        EnsureFolder("Assets/Prefabs", "UI");
        if (Load<UITheme>(ThemePath) == null) BuildTheme();
        BuildLayer();
        BuildFly();
        BuildAudioManager();
        BuildLevelNode();
        BuildRankingRow();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Torre de Pasteles: prefabs creados en Assets/Prefabs.");
    }

    [MenuItem("Torre de Pasteles/3. Crear catálogo y tema visual", priority = 3)]
    public static void BuildCatalogMenu()
    {
        BuildTheme();
        BuildCatalog();
        Selection.activeObject = Load<UITheme>(ThemePath);
        EditorUtility.DisplayDialog("Torre de Pasteles",
            "Listo.\n\n• Assets/Resources/TemaVisual → imágenes de la interfaz (botones, paneles, corazones, mapa, ventilador...)\n" +
            "• Assets/Resources/LevelCatalog → sabores, animales, fondos y retos de cada nivel.\n\n" +
            "Las imágenes que ya habías cambiado se respetaron; los retos y la dificultad se pusieron por defecto.", "OK");
    }

    [MenuItem("Torre de Pasteles/4. Crear escenas", priority = 4)]
    public static void BuildScenesMenu()
    {
        if (!EditorUtility.DisplayDialog("Torre de Pasteles",
                "Esto crea (o REEMPLAZA) LoginScene, MainMenu y GameScene en Assets/Scenes.\n¿Continuar?", "Sí, crear", "Cancelar")) return;
        BuildScenes();
        FontTool.ApplyIfAvailable();
    }

    [MenuItem("Torre de Pasteles/CREAR TODO", priority = 20)]
    public static void BuildAll()
    {
        if (!EditorUtility.DisplayDialog("Torre de Pasteles",
                "Se configurarán las imágenes y se crearán el tema visual, el catálogo, los prefabs y las escenas " +
                "(LoginScene, MainMenu, GameScene). Las escenas y prefabs se REEMPLAZAN; " +
                "las imágenes que hayas puesto en TemaVisual y LevelCatalog se respetan.\n¿Continuar?", "Sí, crear todo", "Cancelar")) return;

        resetStyle = EditorUtility.DisplayDialog("Estilo Sugaru",
            "¿Quieres que TODO el juego quede con el estilo Sugaru (logo, fondos, personajes, botones, paneles)?\n\n" +
            "• Sí: reemplaza también las imágenes que habías puesto antes en TemaVisual y LevelCatalog.\n" +
            "• No: se conservan tus imágenes y solo se usa Sugaru donde no pusiste nada.",
            "Sí, todo Sugaru", "No, conservar mis imágenes");

        EnsureFolder("Assets", "MisImagenes");
        EnsureFolder("Assets", "Fuentes");
        SpriteImageImporter.ConfigureAll();
        FirebaseConfigTool.Configure(out bool _);   // Firebase por API REST (WebGL, Android y editor)
        BuildTheme();
        BuildCatalog();
        BuildPrefabs();
        BuildScenes();
        FontTool.ApplyIfAvailable();
        resetStyle = false;
        EditorSceneManager.OpenScene(SC + "LoginScene.unity");
        EditorUtility.DisplayDialog("Sugaru",
            "¡Listo!\n\nAbre Assets/Scenes/LoginScene y dale Play.\n\n" +
            "Para probar un nivel sin login abre GameScene, elige 'Test Level' en el objeto StackGame y dale Play.\n\n" +
            "Tus imágenes: Assets/Resources/TemaVisual y LevelCatalog.\nTu fuente: ponla en Assets/Fuentes y usa el menú 5.", "OK");
    }

    private static void BuildScenes()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        if (Load<UITheme>(ThemePath) == null) BuildTheme();
        if (Load<LevelCatalog>(CatalogPath) == null) BuildCatalog();
        if (Load<GameObject>(PF + "CapaPastel.prefab") == null || Load<GameObject>(PFU + "NodoNivel.prefab") == null) BuildPrefabs();
        EnsureFolder("Assets", "Scenes");

        BuildLoginScene();
        BuildMainMenuScene();
        BuildGameScene();

        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(SC + "LoginScene.unity", true),
            new EditorBuildSettingsScene(SC + "MainMenu.unity", true),
            new EditorBuildSettingsScene(SC + "GameScene.unity", true),
        };

        // Celular en horizontal
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
        PlayerSettings.allowedAutorotateToLandscapeLeft = true;
        PlayerSettings.allowedAutorotateToLandscapeRight = true;
        PlayerSettings.allowedAutorotateToPortrait = false;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
        PlayerSettings.productName = "Sugaru";

        EditorSceneManager.OpenScene(SC + "LoginScene.unity");
        Debug.Log("Torre de Pasteles: escenas creadas y agregadas a Build Settings.");
    }

    // =====================================================================
    //  TEMA VISUAL (imágenes de la interfaz que tú puedes cambiar)
    // =====================================================================

    private static readonly string[,] ThemeFiles =
    {
        { "logo", "UI/logo.png" }, { "fondoMenu", "UI/fondo_menu.png" },   // fondoLogin vacío = usa el del menú
        { "fondoMapa", "UI/fondo_mapa.png" }, { "boton", "UI/boton.png" }, { "botonVerde", "UI/boton_verde.png" },
        { "botonCerrar", "UI/cerrar.png" }, { "panel", "UI/panel.png" }, { "input", "UI/input.png" },
        { "ticket", "UI/ticket.png" }, { "cinta", "UI/cinta.png" }, { "burbujaDialogo", "UI/burbuja_dialogo.png" },
        { "burbujaCola", "UI/burbuja_cola.png" }, { "rayos", "UI/rayos.png" }, { "corazonLleno", "UI/corazon_lleno.png" },
        { "corazonVacio", "UI/corazon_vacio.png" }, { "estrella", "UI/estrella.png" }, { "estrellaVacia", "UI/estrella_vacia.png" },
        { "nuevoRecord", "UI/nuevo_record.png" }, { "pausa", "UI/btn_pausa.png" }, { "candado", "UI/candado.png" },
        { "check", "UI/check.png" }, { "flecha", "UI/flecha.png" }, { "manoTocar", "UI/mano_tocar.png" },
        { "manoSujetar", "UI/mano_sujetar.png" }, { "viento", "UI/viento.png" }, { "iconoPedido", "UI/icono_pedido.png" },
        { "iconoPerfecto", "UI/icono_perfecto.png" }, { "iconoMosca", "UI/icono_mosca.png" }, { "iconoQuemado", "UI/icono_quemado.png" },
        { "avatar", "UI/avatar.png" }, { "nodoNivel", "UI/nodo_nivel.png" },
        { "nodoBloqueado", "UI/nodo_bloqueado.png" }, { "nodoCompletado", "UI/nodo_completado.png" },
        { "puntoCamino", "UI/punto_camino.png" }, { "marcadorJugador", "UI/marcador.png" }, { "plato", "Escenario/plato.png" },
        { "mostrador", "Escenario/mostrador.png" }, { "ventiladorBase", "Escenario/ventilador_base.png" },
        { "ventiladorAspas", "Escenario/ventilador_aspas.png" }, { "oscuridad", "UI/oscuridad.png" }, { "harina", "Efectos/harina.png" },
    };

    private static void BuildTheme()
    {
        EnsureFolder("Assets", "Resources");
        UITheme theme = Load<UITheme>(ThemePath);
        if (theme == null)
        {
            theme = ScriptableObject.CreateInstance<UITheme>();
            AssetDatabase.CreateAsset(theme, ThemePath);
        }
        SerializedObject so = new SerializedObject(theme);
        for (int i = 0; i < ThemeFiles.GetLength(0); i++)
        {
            SerializedProperty p = so.FindProperty(ThemeFiles[i, 0]);
            if (p == null) { Debug.LogWarning("TemaVisual: no existe la casilla " + ThemeFiles[i, 0]); continue; }
            if (p.objectReferenceValue == null || resetStyle) p.objectReferenceValue = Spr(ThemeFiles[i, 1]);   // solo si está vacía (o si pediste todo Sugaru)
        }
        if (resetStyle)
        {
            SerializedProperty fl = so.FindProperty("fondoLogin");      // vacío = usa el fondo del menú
            if (fl != null) fl.objectReferenceValue = null;
            SerializedProperty ic = so.FindProperty("iconoCorazon");
            if (ic != null) ic.objectReferenceValue = null;
        }
        SerializedProperty flies = so.FindProperty("mosca");
        if (flies != null && flies.arraySize == 0)
        {
            Sprite[] frames = { Spr("Efectos/mosca_1.png"), Spr("Efectos/mosca_2.png") };
            flies.arraySize = frames.Length;
            for (int i = 0; i < frames.Length; i++) flies.GetArrayElementAtIndex(i).objectReferenceValue = frames[i];
        }
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(theme);
        AssetDatabase.SaveAssets();
    }

    // =====================================================================
    //  CATÁLOGO: sabores, clientes y niveles (con los 10 retos)
    // =====================================================================

    internal static void BuildCatalog()
    {
        EnsureFolder("Assets", "Resources");
        LevelCatalog c = Load<LevelCatalog>(CatalogPath);
        if (c == null)
        {
            c = ScriptableObject.CreateInstance<LevelCatalog>();
            AssetDatabase.CreateAsset(c, CatalogPath);
        }

        Color[] flavorColors =
        {
            new Color(1f, 0.67f, 0.75f), new Color(0.6f, 0.4f, 0.3f), new Color(1f, 0.92f, 0.75f), new Color(0.6f, 0.9f, 0.78f),
            new Color(0.62f, 0.58f, 0.92f), new Color(1f, 0.93f, 0.5f), new Color(0.88f, 0.68f, 0.93f), new Color(1f, 0.72f, 0.5f)
        };

        // Sabores (se respetan nombre/imagen/color que ya hayas cambiado)
        var oldFlavors = c.flavors;
        int fCount = Mathf.Max(FlavorFiles.Length, oldFlavors != null ? oldFlavors.Length : 0);
        c.flavors = new LevelCatalog.Flavor[fCount];
        for (int i = 0; i < fCount; i++)
        {
            var old = oldFlavors != null && i < oldFlavors.Length ? oldFlavors[i] : null;
            if (old != null && old.layerSprite != null && !resetStyle) { c.flavors[i] = old; continue; }
            c.flavors[i] = new LevelCatalog.Flavor
            {
                name = i < FlavorNames.Length ? FlavorNames[i] : (old != null ? old.name : "Sabor"),
                layerSprite = i < FlavorFiles.Length ? Spr("Sabores/capa_" + FlavorFiles[i] + ".png") : null,
                color = i < flavorColors.Length ? flavorColors[i] : Color.white
            };
        }

        // Clientes: los 4 personajes Sugaru + los animales que TÚ hayas agregado con tus imágenes
        var oldCustomers = c.customers;
        var customers = new System.Collections.Generic.List<LevelCatalog.Customer>();
        for (int i = 0; i < CustomerFiles.Length; i++)
        {
            customers.Add(new LevelCatalog.Customer
            {
                name = CustomerNames[i],
                normal = Spr("Clientes/" + CustomerFiles[i] + "_normal.png"),
                happy = Spr("Clientes/" + CustomerFiles[i] + "_feliz.png"),
                angry = Spr("Clientes/" + CustomerFiles[i] + "_enojado.png"),
                greeting = CustomerGreetings[i]
            });
        }
        if (oldCustomers != null && !resetStyle)
            foreach (var old in oldCustomers)
                if (old != null && old.normal != null && !BuiltInCustomerPrefixes.Any(px => old.normal.name.ToLowerInvariant().StartsWith(px)))
                    customers.Add(old);   // animal tuyo: se conserva
        c.customers = customers.ToArray();

        if (c.burntLayerSprite == null || resetStyle) c.burntLayerSprite = Spr("Sabores/capa_quemada.png");
        if (c.burntFaceSprite == null || resetStyle) c.burntFaceSprite = Spr("Sabores/cara_quemada.png");

        // Niveles: dificultad y retos por defecto; se respetan fondo, miniatura, música y título que hayas cambiado
        var oldLevels = c.levels;
        c.levels = new LevelCatalog.Entry[10];
        for (int i = 1; i <= 10; i++)
        {
            var e = Preset(i);
            var old = oldLevels != null && i - 1 < oldLevels.Length ? oldLevels[i - 1] : null;
            if (old != null && !resetStyle)
            {
                if (old.background != null && !old.background.name.StartsWith("fondo_nivel_")) e.background = old.background;
                if (old.thumbnail != null && !old.thumbnail.name.StartsWith("miniatura_nivel_")) e.thumbnail = old.thumbnail;
                if (old.music != null) e.music = old.music;
                if (!string.IsNullOrEmpty(old.title) && System.Array.IndexOf(OldDefaultTitles, old.title) < 0) e.title = old.title;
            }
            c.levels[i - 1] = e;
        }
        c.gameScene = "GameScene";

        EditorUtility.SetDirty(c);
        AssetDatabase.SaveAssets();
    }

    /// <summary>Los 10 niveles con sus retos (luego se cambian en el Inspector del LevelCatalog).</summary>
    private static LevelCatalog.Entry Preset(int level)
    {
        var e = new LevelCatalog.Entry
        {
            title = Titles[level - 1],
            thumbnail = Spr($"Escenario/miniatura_nivel_{level:00}.png"),
            background = Spr($"Escenario/fondo_nivel_{level:00}.png"),
            music = Clip($"musica_nivel_{level:00}"),
            ordersToWin = 3,
            floorsPerOrder = 8,
            hearts = 3,
            speedPerFloor = 0.07f,
            speedPerOrder = 0.25f,
            maxSpeed = 8f,
            windChangeEvery = 4f,
        };

        switch (level)
        {
            case 1:
                e.description = "¡Aprende a apilar!";
                e.challengeDescription = "Fácil: apila los 8 pisos en el orden del pedido";
                e.flavorCount = 3; e.baseWidth = 3.4f; e.startSpeed = 2.3f; e.perfectTolerance = 0.15f;
                break;
            case 2:
                e.description = "Una brisa suave";
                e.challengeDescription = "Brisa suave: toca cuando la línea esté en el VERDE y el pastel cae perfecto";
                e.flavorCount = 3; e.baseWidth = 3.3f; e.startSpeed = 2.5f; e.perfectTolerance = 0.14f;
                e.windStrength = 0.55f; e.windChangeEvery = 5f;
                e.timingMeter = true; e.meterGreenSize = 0.24f; e.meterSpeed = 0.75f;
                break;
            case 3:
                e.description = "Pasteles quemados";
                e.challengeDescription = "¡No sueltes los pasteles QUEMADOS! Déjalos pasar o pierdes un corazón";
                e.flavorCount = 4; e.baseWidth = 3.2f; e.startSpeed = 2.8f; e.perfectTolerance = 0.13f;
                e.decoyChance = 0.3f; e.burntRatio = 1f; e.decoyPasses = 2;
                break;
            case 4:
                e.description = "El ventilador";
                e.challengeDescription = "Cuando el ventilador se enciende, ¡espera a que pare antes de soltar!";
                e.flavorCount = 4; e.baseWidth = 3.2f; e.startSpeed = 3f; e.perfectTolerance = 0.13f;
                e.fanEvery = 9f; e.fanForce = 5.5f; e.fanDuration = 2.6f;
                break;
            case 5:
                e.description = "Moscas golosas";
                e.challengeDescription = "Toca las MOSCAS para espantarlas antes de que dañen un pastel";
                e.flavorCount = 5; e.baseWidth = 3.1f; e.startSpeed = 3.1f; e.perfectTolerance = 0.12f;
                e.flyEvery = 6f; e.flySpeed = 1.3f; e.maxFlies = 2; e.flySpawnDistance = 2.2f;   // aparecen cerca del pastel
                break;
            case 6:
                e.description = "La mesa tiembla";
                e.challengeDescription = "Cuando la mesa tiemble, ¡toca rápido para sujetar la torre!";
                e.flavorCount = 5; e.baseWidth = 3.1f; e.startSpeed = 3.2f; e.perfectTolerance = 0.12f;
                e.tremorEvery = 11f; e.tremorDuration = 4f; e.tremorStrength = 5f;
                break;
            case 7:
                e.description = "Apagón";
                e.challengeDescription = "¡Se va la luz! A los 5 segundos todo queda muy oscuro: construye con una luz tenue";
                e.flavorCount = 5; e.baseWidth = 3.1f; e.startSpeed = 3.1f; e.perfectTolerance = 0.12f;
                e.blackoutFirstDelay = 5f; e.blackoutEvery = 13f; e.blackoutDuration = 9f; e.blackoutLightDim = 0.55f;
                break;
            case 8:
                e.description = "¡El cliente exigente!";
                e.challengeDescription = "¡Contra reloj! El cliente cambia de opinión: toca los pasteles que ya no van y rehazlos";
                e.flavorCount = 6; e.baseWidth = 3.1f; e.startSpeed = 3.1f; e.perfectTolerance = 0.12f;
                e.orderChangeChance = 1f; e.orderChangeFloors = 2;
                e.decoyChance = 0.22f; e.burntRatio = 0f; e.tapToDiscard = true;
                e.patienceSeconds = 75f;
                break;
            case 9:
                e.description = "Nube de harina";
                e.challengeDescription = "Una nube de harina tapa la pantalla y ¡el plato se mueve!";
                e.flavorCount = 6; e.baseWidth = 3f; e.startSpeed = 3.5f; e.perfectTolerance = 0.11f;
                e.flourEvery = 10f; e.flourDuration = 3f; e.movingPlateSpeed = 1f;
                break;
            default:
                e.description = "¡La gran tormenta!";
                e.challengeDescription = "Brisa suave + MUCHAS moscas + temblores + pasteles quemados + el cliente cambia de opinión";
                e.flavorCount = 7; e.baseWidth = 3f; e.startSpeed = 3.4f; e.perfectTolerance = 0.1f; e.speedPerFloor = 0.08f;
                e.windStrength = 0.6f; e.windChangeEvery = 4.5f;
                e.flyEvery = 3.2f; e.flySpeed = 1.5f; e.maxFlies = 3; e.flySpawnDistance = 2.6f;
                e.tremorEvery = 12f; e.tremorDuration = 3.5f; e.tremorStrength = 5f;
                e.decoyChance = 0.28f; e.burntRatio = 0.6f; e.tapToDiscard = true;
                e.orderChangeChance = 1f; e.orderChangeFloors = 2;
                break;
        }
        return e;
    }

    // =====================================================================
    //  PREFABS
    // =====================================================================

    private static void BuildLayer()
    {
        GameObject go = new GameObject("CapaPastel");
        GameObject b = new GameObject("Cuerpo");
        b.transform.SetParent(go.transform, false);
        SpriteRenderer body = b.AddComponent<SpriteRenderer>();
        body.sprite = Spr("Sabores/capa_fresa.png");
        body.sortingOrder = 0;
        b.AddComponent<CatalogImageNote>().source = CatalogImageNote.Source.SaborDePastel;

        GameObject f = new GameObject("Cara");
        f.transform.SetParent(go.transform, false);
        f.transform.localPosition = new Vector3(0f, -0.06f, 0f);
        SpriteRenderer face = f.AddComponent<SpriteRenderer>();
        face.sprite = Spr("Sabores/cara_kawaii.png");
        face.sortingOrder = 1;
        if (face.sprite != null)
        {
            float k = 0.36f / face.sprite.bounds.size.y;
            f.transform.localScale = new Vector3(k, k, 1f);
        }

        CakeLayer layer = go.AddComponent<CakeLayer>();
        layer.body = body;
        layer.face = face;
        layer.scaredFace = Spr("Sabores/cara_asustada.png");
        layer.height = 0.62f;
        layer.SetWidth(3f);
        Save(go, PF + "CapaPastel");
    }

    private static void BuildFly()
    {
        GameObject go = new GameObject("Mosca");
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = Spr("Efectos/mosca_1.png");
        sr.sortingOrder = 30;
        if (sr.sprite != null)
        {
            float k = 0.75f / sr.sprite.bounds.size.x;
            go.transform.localScale = new Vector3(k, k, 1f);
        }
        Fly fly = go.AddComponent<Fly>();
        fly.sprite = sr;
        fly.frames = new[] { Spr("Efectos/mosca_1.png"), Spr("Efectos/mosca_2.png") }.Where(s => s != null).ToArray();
        fly.tapRadius = 0.9f;
        Save(go, PF + "Mosca");
    }

    private static void BuildAudioManager()
    {
        GameObject go = new GameObject("AudioManager");
        AudioSource music = go.AddComponent<AudioSource>();
        music.playOnAwake = false;
        music.loop = true;
        AudioSource sfx = go.AddComponent<AudioSource>();
        sfx.playOnAwake = false;
        AudioSource pitched = go.AddComponent<AudioSource>();
        pitched.playOnAwake = false;

        AudioManager am = go.AddComponent<AudioManager>();
        am.musicSource = music;
        am.sfxSource = sfx;
        am.pitchedSource = pitched;
        am.menuMusic = Clip("musica_menu");
        am.defaultLevelMusic = Clip("musica_nivel");
        am.winJingle = Clip("victoria");
        am.loseJingle = Clip("derrota");
        am.drop = Clip("plop");
        am.perfect = Clip("perfecto");
        am.cut = Clip("cortar");
        am.miss = Clip("caer");
        am.wrongFlavor = Clip("sabor_mal");
        am.orderDone = Clip("pedido_listo");
        am.orderFailed = Clip("pedido_fallido");
        am.click = Clip("click");
        am.wind = Clip("viento");
        am.tick = Clip("tic");
        am.whoosh = Clip("whoosh");
        am.flyBuzz = Clip("zumbido");
        am.shoo = Clip("espantar");
        am.fan = Clip("ventilador");
        am.tremor = Clip("temblor");
        am.stabilize = Clip("sujetar");
        am.lightsOff = Clip("apagon");
        am.flour = Clip("harina");
        am.alert = Clip("alerta");
        am.decoyAvoided = Clip("bien");
        am.orderChange = Clip("cambio");
        am.starPop = Clip("estrella");
        am.count = Clip("contar");
        am.newRecord = Clip("record");
        Save(go, PF + "AudioManager");
    }

    /// <summary>Un nivel del mapa (macaron con número, título, estrellas y candado).</summary>
    private static void BuildLevelNode()
    {
        RectTransform root = NewUI("NodoNivel", null);
        root.sizeDelta = new Vector2(200, 200);

        Image glow = NewImage(root, "Aro", Spr("UI/aro_nivel.png"), new Color(1f, 0.95f, 0.6f));
        glow.raycastTarget = false;
        Place(glow.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(280, 280));

        Image baseImg = Themed(NewImage(root, "Base", UITheme.Get(ThemeKey.NodoNivel) ?? Spr("UI/nodo_nivel.png"), Pink), ThemeKey.NodoNivel);
        baseImg.preserveAspect = true;
        Stretch(baseImg.rectTransform);
        Button button = root.gameObject.AddComponent<Button>();
        button.targetGraphic = baseImg;

        TextMeshProUGUI number = Label(root, "Numero", "1", 76, TextAlignmentOptions.Center, Color.white);
        number.fontStyle = FontStyles.Bold;
        number.outlineWidth = 0.25f;
        number.outlineColor = new Color32(200, 90, 130, 255);
        number.fontSize = 60;
        Place(number.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-4, -34), new Vector2(160, 80));   // sobre la cara de la porción

        TextMeshProUGUI title = Label(root, "Titulo", "Nivel", 26, TextAlignmentOptions.Center, Brown);
        title.fontStyle = FontStyles.Bold;
        Place(title.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 1f), new Vector2(0, -2), new Vector2(300, 44));

        Image lockIcon = Themed(NewImage(root, "Candado", Spr("UI/candado.png"), Brown), ThemeKey.Candado);
        lockIcon.preserveAspect = true;
        lockIcon.raycastTarget = false;
        Place(lockIcon.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-4, -30), new Vector2(72, 72));

        RectTransform starRow = NewUI("Estrellas", root);
        Place(starRow, new Vector2(0.5f, 1f), new Vector2(0.5f, 0f), new Vector2(0, -16), new Vector2(200, 64));
        Image[] stars = new Image[3];
        for (int i = 0; i < 3; i++)
        {
            Image s = NewImage(starRow, "Estrella" + (i + 1), Spr("UI/estrella.png"), new Color(1f, 0.85f, 0.35f));
            s.preserveAspect = true;
            s.raycastTarget = false;
            float size = i == 1 ? 64 : 52;
            Place(s.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2((i - 1) * 60, i == 1 ? 10 : 0), new Vector2(size, size));
            stars[i] = s;
        }

        LevelNodeUI ui = root.gameObject.AddComponent<LevelNodeUI>();
        ui.button = button;
        ui.baseImage = baseImg;
        ui.numberText = number;
        ui.titleText = title;
        ui.stars = stars;
        ui.lockIcon = lockIcon.gameObject;
        ui.glow = glow.rectTransform;
        ui.lockedTint = Color.white;   // la porción bloqueada ya es gris
        Save(root.gameObject, PFU + "NodoNivel");
    }

    private static void BuildRankingRow()
    {
        RectTransform root = NewUI("RankingRow", null);
        root.sizeDelta = new Vector2(1100, 76);
        Image bg = root.gameObject.AddComponent<Image>();
        bg.color = new Color(1f, 1f, 1f, 0.1f);
        root.gameObject.AddComponent<LayoutElement>().preferredHeight = 80;
        HorizontalLayoutGroup h = root.gameObject.AddComponent<HorizontalLayoutGroup>();
        h.padding = new RectOffset(20, 20, 0, 0);
        h.childControlWidth = h.childControlHeight = true;
        h.childForceExpandWidth = false;
        h.childForceExpandHeight = true;

        RankingRowUI ui = root.gameObject.AddComponent<RankingRowUI>();
        ui.positionText = RowCell(root, "Position", "1", 0.12f, FontStyles.Bold);
        ui.usernameText = RowCell(root, "Username", "jugador", 0.46f, FontStyles.Normal);
        ui.scoreText = RowCell(root, "Score", "0", 0.24f, FontStyles.Bold);
        ui.levelText = RowCell(root, "Level", "Nv 1", 0.18f, FontStyles.Normal);
        ui.background = bg;
        ui.normalColor = new Color(0.47f, 0.27f, 0.33f, 0.08f);
        ui.myRowColor = new Color(1f, 0.55f, 0.72f, 0.45f);
        Save(root.gameObject, PFU + "RankingRow");
    }

    private static TextMeshProUGUI RowCell(Transform parent, string name, string text, float flex, FontStyles style)
    {
        TextMeshProUGUI t = Label(parent, name, text, 34, TextAlignmentOptions.MidlineLeft, Brown);
        t.fontStyle = style;
        LayoutElement le = t.gameObject.AddComponent<LayoutElement>();
        le.flexibleWidth = flex;
        le.minWidth = 0;
        return t;
    }

    // =====================================================================
    //  ESCENA DEL JUEGO
    //  Distribución (mundo, cámara ortográfica de 4.8):
    //   cliente a la IZQUIERDA (x -6.3) · torre en el CENTRO-DERECHA (x 0.5) · ventilador a la derecha (x 7.4)
    //   el ticket del pedido sigue arriba a la derecha y el reto arriba al centro.
    // =====================================================================

    private const float TowerX = 0.5f;
    private const float CounterTop = -3.6f;

    private static void BuildGameScene()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        Camera cam = MakeCamera(SkyPink);
        cam.orthographicSize = 4.8f;

        // Fondo (StackGame lo cambia por el fondo de cada nivel del LevelCatalog)
        GameObject bg = new GameObject("Fondo");
        bg.transform.SetParent(cam.transform, false);
        bg.transform.localPosition = new Vector3(0f, 0f, 10f);
        SpriteRenderer bgSr = bg.AddComponent<SpriteRenderer>();
        bgSr.sprite = Spr("Escenario/fondo_nivel_01.png");
        bgSr.sortingOrder = -100;
        bg.AddComponent<BackgroundFit>();
        bg.AddComponent<CatalogImageNote>().source = CatalogImageNote.Source.FondoDeNivel;

        MakeEventSystem();
        Instantiate(PF + "AudioManager.prefab");

        // ---------- Mostrador (mesa) ----------
        Sprite counterSprite = UITheme.Get(ThemeKey.Mostrador) ?? Spr("Escenario/mostrador.png");
        if (counterSprite != null)
        {
            GameObject counter = new GameObject("Mostrador");
            SpriteRenderer csr = counter.AddComponent<SpriteRenderer>();
            csr.sprite = counterSprite;
            csr.drawMode = SpriteDrawMode.Tiled;
            csr.sortingOrder = -5;
            float h = 1.6f;
            float k = h / counterSprite.bounds.size.y;
            counter.transform.localScale = new Vector3(k, k, 1f);
            csr.size = new Vector2(40f / k, counterSprite.bounds.size.y);
            counter.transform.position = new Vector3(0f, CounterTop - h / 2f + 0.05f, 0f);
            counter.AddComponent<ThemedImage>().key = ThemeKey.Mostrador;
        }

        // ---------- Torre: el plato queda justo encima de la mesa ----------
        GameObject tower = new GameObject("Torre");
        tower.transform.position = new Vector3(TowerX, CounterTop + 0.12f, 0f);
        GameObject plate = new GameObject("Plato");
        plate.transform.SetParent(tower.transform, false);
        SpriteRenderer psr = plate.AddComponent<SpriteRenderer>();
        psr.sprite = UITheme.Get(ThemeKey.Plato) ?? Spr("Escenario/plato.png");
        psr.sortingOrder = -1;
        float plateH = 0.7f;
        if (psr.sprite != null)
        {
            float k = plateH / psr.sprite.bounds.size.y;
            plate.transform.localScale = new Vector3(k, k, 1f);
        }
        plate.transform.localPosition = new Vector3(0f, -plateH / 2f + 0.06f, 0f);
        ThemedImage pt = plate.AddComponent<ThemedImage>();
        pt.key = ThemeKey.Plato;
        pt.fitHeight = plateH;

        // ---------- Cliente (a la izquierda, entra desde la izquierda) ----------
        GameObject customerGO = new GameObject("Cliente");
        customerGO.transform.position = new Vector3(-6.3f, -2.15f, 0f);
        GameObject cs = new GameObject("Imagen");
        cs.transform.SetParent(customerGO.transform, false);
        SpriteRenderer customerSr = cs.AddComponent<SpriteRenderer>();
        customerSr.sprite = Spr("Clientes/conejita_normal.png");
        customerSr.sortingOrder = -6;   // detrás de la mesa
        cs.AddComponent<CatalogImageNote>().source = CatalogImageNote.Source.Cliente;
        if (customerSr.sprite != null)
        {
            float k = 3.4f / customerSr.sprite.bounds.size.y;
            cs.transform.localScale = new Vector3(k, k, 1f);
        }
        CustomerView customer = customerGO.AddComponent<CustomerView>();
        customer.sprite = customerSr;
        customer.enterDistance = -6f;

        // ---------- Ventilador (nivel 4) ----------
        GameObject fanGO = new GameObject("Ventilador");
        fanGO.transform.position = new Vector3(7.4f, CounterTop + 1.0f, 0f);
        SpriteRenderer fanBase = fanGO.AddComponent<SpriteRenderer>();
        fanBase.sprite = UITheme.Get(ThemeKey.VentiladorBase) ?? Spr("Escenario/ventilador_base.png");
        fanBase.sortingOrder = 2;
        fanBase.flipX = true;
        float fanH = 2.1f;
        float fanScale = fanBase.sprite != null ? fanH / fanBase.sprite.bounds.size.y : 1f;
        fanGO.transform.localScale = new Vector3(fanScale, fanScale, 1f);
        ThemedImage ft = fanGO.AddComponent<ThemedImage>();
        ft.key = ThemeKey.VentiladorBase;
        ft.fitHeight = fanH;
        GameObject blades = new GameObject("Aspas");
        blades.transform.SetParent(fanGO.transform, false);
        // el centro del círculo del ventilador está un poco arriba del centro de la imagen
        float spriteH = fanBase.sprite != null ? fanBase.sprite.bounds.size.y : 4.2f;
        blades.transform.localPosition = new Vector3(0f, spriteH * (50f / 420f), 0f);
        SpriteRenderer bladeSr = blades.AddComponent<SpriteRenderer>();
        bladeSr.sprite = UITheme.Get(ThemeKey.VentiladorAspas) ?? Spr("Escenario/ventilador_aspas.png");
        bladeSr.sortingOrder = 3;
        if (bladeSr.sprite != null)
        {
            float wanted = spriteH * (250f / 420f);
            float k = wanted / bladeSr.sprite.bounds.size.x;
            blades.transform.localScale = new Vector3(k, k, 1f);
        }
        blades.AddComponent<ThemedImage>().key = ThemeKey.VentiladorAspas;
        FanHazard fan = fanGO.AddComponent<FanHazard>();
        fan.blades = blades.transform;

        // ---------- Efectos del mundo ----------
        WindEffect wind = new GameObject("EfectoViento").AddComponent<WindEffect>();
        wind.particleSprites = new[] { Spr("Efectos/petalo.png"), Spr("Efectos/chispa.png") }.Where(s => s != null).ToArray();
        Confetti confetti = new GameObject("Confeti").AddComponent<Confetti>();
        confetti.sprites = new[] { Spr("Efectos/confeti.png"), Spr("Efectos/brillo.png"), Spr("Efectos/corazon_mini.png") }.Where(s => s != null).ToArray();

        // ---------- Interfaz ----------
        RectTransform safe = MakeCanvas("Canvas", null, ThemeKey.FondoMenu);
        Transform canvasRoot = safe.parent;

        // Zona para tocar: toda la pantalla, detrás de todo
        Image tap = NewImage(safe, "ZonaTocar", null, new Color(1f, 1f, 1f, 0f));
        Stretch(tap.rectTransform);
        tap.gameObject.AddComponent<TapZone>();
        tap.transform.SetAsFirstSibling();

        ScreenEffects effects = BuildScreenEffects(canvasRoot, safe);
        GameHud hud = BuildGameHud(canvasRoot, safe);
        OrderIntroUI intro = BuildOrderIntro(safe);
        BuildPausePanel(safe, hud);
        ResultScreen results = BuildResultScreen(safe);

        // ---------- Lógica ----------
        StackGame game = new GameObject("StackGame").AddComponent<StackGame>();
        game.cam = cam;
        game.backgroundRenderer = bgSr;
        game.towerRoot = tower.transform;
        game.layerPrefab = LoadC<CakeLayer>(PF + "CapaPastel.prefab");
        game.flyPrefab = LoadC<Fly>(PF + "Mosca.prefab");
        game.customer = customer;
        game.hud = hud;
        game.orderIntro = intro;
        game.results = results;
        game.effects = effects;
        game.windEffect = wind;
        game.confetti = confetti;
        game.fan = fan;
        game.plateTopY = 0f;
        game.slideLeft = 3.4f;
        game.slideRight = 2.8f;
        game.minWorldWidth = 17f;

        PanelTools.SetupOpenScene();   // paneles editables + estado correcto al jugar
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), SC + "GameScene.unity");
    }

    // ------------------------------------------------------------ apagón y harina
    private static ScreenEffects BuildScreenEffects(Transform canvasRoot, RectTransform safe)
    {
        ScreenEffects fx = canvasRoot.gameObject.AddComponent<ScreenEffects>();

        RectTransform layer = NewUI("Efectos", safe);
        Stretch(layer);
        layer.SetSiblingIndex(1);   // encima de la zona para tocar, debajo del HUD (el reto se sigue viendo)

        // Oscuridad: imagen gigante con un hueco de luz que sigue a la torre
        Image dark = Themed(NewImage(layer, "Oscuridad", Spr("UI/oscuridad.png"), new Color(0.06f, 0.04f, 0.12f, 0.95f)), ThemeKey.Oscuridad);
        dark.raycastTarget = false;
        Place(dark.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(4400, 4400));
        CanvasGroup dg = dark.gameObject.AddComponent<CanvasGroup>();
        dg.blocksRaycasts = false;
        dg.interactable = false;
        fx.darkness = dark.rectTransform;
        fx.darknessGroup = dg;

        // Harina: varias nubes que tapan la pantalla
        RectTransform flour = NewUI("Harina", layer);
        Stretch(flour);
        flour.gameObject.AddComponent<CubrirPantalla>();
        Sprite flourSprite = UITheme.Get(ThemeKey.Harina) ?? Spr("Efectos/harina.png");
        Image[] puffs = new Image[8];
        for (int i = 0; i < puffs.Length; i++)
        {
            Image p = Themed(NewImage(flour, "Nube" + (i + 1), flourSprite, Color.white), ThemeKey.Harina);
            p.raycastTarget = false;
            p.preserveAspect = true;
            Place(p.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900, 700));
            puffs[i] = p;
        }
        fx.flourRoot = flour;
        fx.flourPuffs = puffs;
        return fx;
    }

    // ------------------------------------------------------------ HUD
    private static GameHud BuildGameHud(Transform canvasRoot, RectTransform safe)
    {
        GameHud hud = canvasRoot.gameObject.AddComponent<GameHud>();

        // ---------- Arriba a la izquierda: pausa + info ----------
        hud.pauseButton = IconButton(safe, "PauseButton", ThemeKey.Pausa, "II");
        Place((RectTransform)hud.pauseButton.transform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(24, -20), new Vector2(110, 110));

        // Etiqueta del nivel debajo del botón de pausa
        Image levelTag = Themed(NewImage(safe, "EtiquetaNivel", Spr("UI/boton_verde.png"), Pink), ThemeKey.BotonVerde);
        levelTag.raycastTarget = false;
        Place(levelTag.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(18, -138), new Vector2(122, 62));
        hud.levelText = Label(levelTag.rectTransform, "LevelText", "Nivel 1", 26, TextAlignmentOptions.Center, Color.white);
        hud.levelText.fontStyle = FontStyles.Bold;
        hud.levelText.enableAutoSizing = true;
        hud.levelText.fontSizeMin = 14;
        hud.levelText.fontSizeMax = 26;
        Stretch(hud.levelText.rectTransform, 8, 8, 4, 10);

        // Pastillas (como en la interfaz Sugaru): puntaje, corazones y pedidos
        RectTransform scorePill = Pill(safe, "PastillaPuntaje", new Vector2(150, -16), new Vector2(340, 92), "UI/estrella.png", ThemeKey.Estrella);
        hud.scoreText = Label(scorePill, "ScoreText", "0", 54, TextAlignmentOptions.MidlineLeft, MessagePink);
        hud.scoreText.fontStyle = FontStyles.Bold;
        Stretch(hud.scoreText.rectTransform, 104, 20, 8, 16);
        RectTransform heartPill = Pill(safe, "PastillaCorazones", new Vector2(150, -114), new Vector2(340, 84), null, ThemeKey.Estrella);
        RectTransform ordersPill = Pill(safe, "PastillaPedidos", new Vector2(150, -204), new Vector2(340, 84), "UI/fresa.png", ThemeKey.IconoPedido);
        hud.ordersText = Label(ordersPill, "OrdersText", "Pedidos 0/3", 32, TextAlignmentOptions.MidlineLeft, Brown);
        hud.ordersText.fontStyle = FontStyles.Bold;
        hud.ordersText.enableAutoSizing = true;
        hud.ordersText.fontSizeMin = 18;
        hud.ordersText.fontSizeMax = 32;
        Stretch(hud.ordersText.rectTransform, 100, 20, 6, 14);
        RectTransform heartsRow = NewUI("Corazones", heartPill);
        Stretch(heartsRow, 24, 20, 8, 16);
        HorizontalLayoutGroup hl = heartsRow.gameObject.AddComponent<HorizontalLayoutGroup>();
        hl.spacing = 6;
        hl.childControlWidth = hl.childControlHeight = false;
        hl.childForceExpandWidth = hl.childForceExpandHeight = false;
        hl.childAlignment = TextAnchor.MiddleLeft;
        hud.hearts = new Image[6];
        for (int i = 0; i < hud.hearts.Length; i++)
        {
            Image h = Themed(NewImage(heartsRow, "Corazon" + (i + 1), Spr("UI/corazon_lleno.png"), new Color(1f, 0.45f, 0.55f)), ThemeKey.CorazonLleno);
            h.raycastTarget = false;
            h.preserveAspect = true;
            h.rectTransform.sizeDelta = new Vector2(50, 50);
            hud.hearts[i] = h;
        }

        // ---------- Viento (debajo del panel de info) ----------
        RectTransform windRoot = Card(safe, "Viento", new Vector2(0, 1), Vector2.zero, new Vector2(300, 84));
        windRoot.pivot = new Vector2(0, 1);
        windRoot.anchoredPosition = new Vector2(150, -300);
        Image windImg = Themed(NewImage(windRoot, "Flecha", Spr("UI/viento.png"), MessagePink), ThemeKey.Viento);
        windImg.preserveAspect = true;
        windImg.raycastTarget = false;
        windImg.color = MessagePink;
        Place(windImg.rectTransform, new Vector2(0, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(60, 0), new Vector2(84, 62));
        hud.windText = Label(windRoot, "Texto", "Viento", 28, TextAlignmentOptions.MidlineLeft, MessagePink);
        hud.windText.fontStyle = FontStyles.Bold;
        Place(hud.windText.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(112, 0), new Vector2(180, 60));
        hud.windRoot = windRoot.gameObject;
        hud.windArrow = windImg.rectTransform;

        // ---------- Reto: SIEMPRE visible arriba al centro ----------
        Image banner = Themed(NewImage(safe, "RetoNivel", Spr("UI/cinta.png"), Pink), ThemeKey.Cinta);
        banner.raycastTarget = false;
        Place(banner.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(100, -8), new Vector2(860, 166));   // entre el panel de info y el ticket
        hud.levelChallengeText = Label(banner.rectTransform, "Texto", "Reto del nivel", 32, TextAlignmentOptions.Center, Color.white);
        hud.levelChallengeText.fontStyle = FontStyles.Normal;   // sin negrita
        hud.levelChallengeText.enableAutoSizing = true;
        hud.levelChallengeText.fontSizeMin = 16;
        hud.levelChallengeText.fontSizeMax = 26;
        hud.levelChallengeText.outlineWidth = 0.12f;
        hud.levelChallengeText.outlineColor = new Color32(200, 80, 130, 255);
        Stretch(hud.levelChallengeText.rectTransform, 140, 140, 20, 44);
        RectTransform orderPill = Card(banner.rectTransform, "RetoPedido", new Vector2(0.5f, 0), new Vector2(0, -28), new Vector2(580, 60));
        hud.orderChallengeText = Label(orderPill, "Texto", "Reto del cliente", 26, TextAlignmentOptions.Center, MessagePink);
        hud.orderChallengeText.fontStyle = FontStyles.Normal;
        hud.orderChallengeText.enableAutoSizing = true;
        hud.orderChallengeText.fontSizeMin = 14;
        hud.orderChallengeText.fontSizeMax = 22;
        Stretch(hud.orderChallengeText.rectTransform, 16, 16, 4, 4);
        hud.challengeBanner = banner.gameObject;
        hud.challengeAutoHide = false;     // el reto del nivel siempre está en pantalla
        hud.challengeShowSeconds = 5f;

        // ---------- Medidor de precisión (nivel 2) y cronómetro (nivel 8) ----------
        Sprite roundUI = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        Sprite knob = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
        hud.timingMeter = TimingMeterUI.Create(safe, null, roundUI);
        hud.orderTimer = OrderTimerUI.Create(safe, null, roundUI, knob);
        ((RectTransform)hud.orderTimer.transform).anchoredPosition = new Vector2(150f, -396f);

        // ---------- Ticket del pedido (arriba a la derecha, donde estaba) ----------
        hud.ticket = BuildTicket(safe);

        // ---------- Aviso de evento (ventilador, temblor, apagón...) ----------
        RectTransform ev = Card(safe, "AvisoEvento", new Vector2(0.5f, 1), new Vector2(100, -230), new Vector2(720, 96));
        ev.GetComponent<Image>().color = new Color(1f, 0.92f, 0.6f);
        Image evIcon = Themed(NewImage(ev, "Icono", Spr("UI/viento.png"), MessagePink), ThemeKey.Viento);
        evIcon.preserveAspect = true;
        evIcon.raycastTarget = false;
        Place(evIcon.rectTransform, new Vector2(0, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(60, 0), new Vector2(70, 60));
        hud.eventText = Label(ev, "Texto", "¡Cuidado!", 38, TextAlignmentOptions.Center, MessagePink);
        hud.eventText.fontStyle = FontStyles.Bold;
        hud.eventText.enableAutoSizing = true;
        hud.eventText.fontSizeMin = 20;
        hud.eventText.fontSizeMax = 38;
        Stretch(hud.eventText.rectTransform, 100, 30, 6, 6);
        hud.eventBanner = ev.gameObject;
        ev.gameObject.SetActive(false);

        // ---------- Barra de estabilidad (temblor) ----------
        RectTransform stab = NewUI("Estabilidad", safe);
        Place(stab, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(100, -340), new Vector2(620, 110));
        Image hand = Themed(NewImage(stab, "Mano", Spr("UI/mano_sujetar.png"), Color.white), ThemeKey.ManoSujetar);
        hand.preserveAspect = true;
        hand.raycastTarget = false;
        Place(hand.rectTransform, new Vector2(0, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(55, 0), new Vector2(110, 100));
        Image stabBg = BarBack(stab);
        Place(stabBg.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(120, 0), new Vector2(490, 56));
        Image stabFill = BarFill(stabBg.rectTransform);
        TextMeshProUGUI stabText = Label(stabBg.rectTransform, "Texto", "¡Toca para sujetar!", 26, TextAlignmentOptions.Center, Brown);
        stabText.fontStyle = FontStyles.Bold;
        Stretch(stabText.rectTransform);
        hud.stabilityRoot = stab.gameObject;
        hud.stabilityFill = stabFill;
        hud.stableColor = Color.white;                       // la barra ya es rosada
        hud.unstableColor = new Color(1f, 0.72f, 0.55f);     // se pone más naranja cuando la torre se va a caer
        hud.stabilityHand = hand.rectTransform;
        stab.gameObject.SetActive(false);

        // ---------- Mensajes ----------
        hud.messageText = Label(safe, "MessageText", "", 70, TextAlignmentOptions.Center, MessagePink);
        hud.messageText.fontStyle = FontStyles.Bold;
        hud.messageText.outlineWidth = 0.22f;
        hud.messageText.outlineColor = new Color32(255, 255, 255, 255);
        Place(hud.messageText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(40, 40), new Vector2(1100, 300));
        hud.messageText.gameObject.SetActive(false);

        hud.popupText = Label(safe, "PopupText", "", 76, TextAlignmentOptions.Center, MessagePink);
        hud.popupText.fontStyle = FontStyles.Bold;
        hud.popupText.outlineWidth = 0.22f;
        hud.popupText.outlineColor = new Color32(255, 255, 255, 255);
        Place(hud.popupText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(40, 170), new Vector2(900, 140));
        hud.popupText.gameObject.SetActive(false);

        RectTransform hint = NewUI("AyudaTocar", safe);
        Place(hint, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(60, 220), new Vector2(560, 120));
        Image tapHand = Themed(NewImage(hint, "Mano", Spr("UI/mano_tocar.png"), Color.white), ThemeKey.ManoTocar);
        tapHand.preserveAspect = true;
        tapHand.raycastTarget = false;
        Place(tapHand.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), Vector2.zero, new Vector2(100, 120));
        TextMeshProUGUI hintText = Label(hint, "Texto", "¡Toca para soltar el pastel!", 36, TextAlignmentOptions.MidlineLeft, Brown);
        hintText.fontStyle = FontStyles.Bold;
        Place(hintText.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(110, 0), new Vector2(460, 80));
        hud.tapHint = hint.gameObject;
        hint.gameObject.SetActive(false);

        return hud;
    }

    private static OrderTicketUI BuildTicket(Transform parent)
    {
        Image paper = Themed(NewImage(parent, "TicketPedido", Spr("UI/ticket.png"), Color.white), ThemeKey.Ticket);
        paper.raycastTarget = false;
        Place(paper.rectTransform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-24, -16), new Vector2(340, 610));
        OrderTicketUI t = paper.gameObject.AddComponent<OrderTicketUI>();
        t.group = paper.gameObject.AddComponent<CanvasGroup>();
        t.group.blocksRaycasts = false;

        Image icon = Themed(NewImage(paper.rectTransform, "IconoPedido", Spr("UI/icono_pedido.png"), Pink), ThemeKey.IconoPedido);
        icon.preserveAspect = true;
        icon.raycastTarget = false;
        Place(icon.rectTransform, new Vector2(0, 1), new Vector2(0.5f, 0.5f), new Vector2(44, -42), new Vector2(54, 54));
        t.titleText = Label(paper.rectTransform, "Titulo", "Pedido de Conejita", 26, TextAlignmentOptions.Center, Brown);
        t.titleText.fontStyle = FontStyles.Bold;
        t.titleText.enableAutoSizing = true;
        t.titleText.fontSizeMin = 16;
        t.titleText.fontSizeMax = 26;
        Place(t.titleText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(22, -16), new Vector2(260, 52));
        t.challengeText = Label(paper.rectTransform, "Reto", "Reto", 20, TextAlignmentOptions.Center, MessagePink);
        t.challengeText.fontStyle = FontStyles.Bold;
        t.challengeText.enableAutoSizing = true;
        t.challengeText.fontSizeMin = 14;
        t.challengeText.fontSizeMax = 22;
        Place(t.challengeText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -68), new Vector2(310, 46));

        RectTransform rows = NewUI("Pisos", paper.rectTransform);
        Stretch(rows, 16, 16, 118, 70);
        VerticalLayoutGroup v = rows.gameObject.AddComponent<VerticalLayoutGroup>();
        v.spacing = 2;
        v.childControlWidth = v.childControlHeight = true;
        v.childForceExpandWidth = true;
        v.childForceExpandHeight = false;
        v.childAlignment = TextAnchor.UpperCenter;
        t.rowsParent = rows;

        // Fila de ejemplo (se copia por cada piso). Hijos: Flecha, Icono, Nombre, Check
        RectTransform row = NewUI("FilaEjemplo", rows);
        row.gameObject.AddComponent<LayoutElement>().preferredHeight = 50;
        Image arrow = Themed(NewImage(row, "Flecha", Spr("UI/flecha.png"), Pink), ThemeKey.Flecha);
        arrow.raycastTarget = false;
        Place(arrow.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), Vector2.zero, new Vector2(30, 30));
        Image rowIcon = NewImage(row, "Icono", Spr("Sabores/capa_fresa.png"), Color.white);
        rowIcon.preserveAspect = true;
        rowIcon.raycastTarget = false;
        Place(rowIcon.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(34, 0), new Vector2(96, 38));
        TextMeshProUGUI name = Label(row, "Nombre", "1. Fresa", 24, TextAlignmentOptions.MidlineLeft, Brown);
        name.enableAutoSizing = true;
        name.fontSizeMin = 14;
        name.fontSizeMax = 24;
        Place(name.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(136, 0), new Vector2(128, 44));
        Image check = Themed(NewImage(row, "Check", Spr("UI/check.png"), Green), ThemeKey.Check);
        check.raycastTarget = false;
        Place(check.rectTransform, new Vector2(1, 0.5f), new Vector2(1, 0.5f), Vector2.zero, new Vector2(36, 36));
        row.gameObject.SetActive(false);
        t.rowTemplate = row.gameObject;

        // Paciencia
        RectTransform pat = NewUI("Paciencia", paper.rectTransform);
        Place(pat, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 32), new Vector2(290, 30));
        Image patBg = BarBack(pat);
        Stretch(patBg.rectTransform);
        Image patFill = BarFill(patBg.rectTransform);
        t.patienceRoot = pat.gameObject;
        t.patienceFill = patFill;
        t.patienceOk = Color.white;
        t.patienceLow = new Color(1f, 0.72f, 0.55f);
        return t;
    }

    // ------------------------------------------------------------ presentación animada del pedido
    private static OrderIntroUI BuildOrderIntro(RectTransform safe)
    {
        RectTransform root = NewUI("PresentacionPedido", safe);
        Stretch(root);
        OrderIntroUI intro = root.gameObject.AddComponent<OrderIntroUI>();

        // Globo de diálogo al lado del cliente (el cliente está a la izquierda)
        Image bubble = Themed(NewImage(root, "Globo", Spr("UI/burbuja_dialogo.png"), Color.white), ThemeKey.BurbujaDialogo);
        bubble.raycastTarget = false;
        Place(bubble.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-80, 20), new Vector2(860, 640));
        intro.card = bubble.rectTransform;
        intro.group = bubble.gameObject.AddComponent<CanvasGroup>();
        intro.group.blocksRaycasts = false;

        Image tail = Themed(NewImage(bubble.rectTransform, "Cola", Spr("UI/burbuja_cola.png"), Color.white), ThemeKey.BurbujaCola);
        tail.raycastTarget = false;
        Place(tail.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(60, 14), new Vector2(150, 128));   // apunta hacia el cliente

        intro.nameText = Label(bubble.rectTransform, "Nombre", "Conejita dice:", 40, TextAlignmentOptions.Center, MessagePink);
        intro.nameText.fontStyle = FontStyles.Bold;
        Place(intro.nameText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -30), new Vector2(780, 56));
        intro.greetingText = Label(bubble.rectTransform, "Saludo", "¡Hola! ¿Me preparas este pastel?", 30, TextAlignmentOptions.Center, Brown);
        intro.greetingText.enableAutoSizing = true;
        intro.greetingText.fontSizeMin = 20;
        intro.greetingText.fontSizeMax = 30;
        Place(intro.greetingText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -88), new Vector2(760, 80));
        intro.floorsText = Label(bubble.rectTransform, "Pisos", "Quiero 8 pisos, de abajo hacia arriba:", 26, TextAlignmentOptions.Center, Brown);
        intro.floorsText.fontStyle = FontStyles.Bold;
        Place(intro.floorsText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -170), new Vector2(760, 40));

        RectTransform layers = NewUI("Capas", bubble.rectTransform);
        Place(layers, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -216), new Vector2(760, 200));
        GridLayoutGroup g = layers.gameObject.AddComponent<GridLayoutGroup>();
        g.cellSize = new Vector2(176, 92);
        g.spacing = new Vector2(10, 12);
        g.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        g.constraintCount = 4;
        g.childAlignment = TextAnchor.UpperCenter;
        intro.layersParent = layers;

        Image tpl = NewImage(layers, "CapaEjemplo", Spr("Sabores/capa_fresa.png"), Pink);
        tpl.preserveAspect = true;
        tpl.raycastTarget = false;
        TextMeshProUGUI num = Label(tpl.rectTransform, "Numero", "1", 28, TextAlignmentOptions.Center, Color.white);
        num.fontStyle = FontStyles.Bold;
        num.outlineWidth = 0.3f;
        num.outlineColor = new Color32(120, 60, 80, 255);
        Place(num.rectTransform, new Vector2(0f, 1f), new Vector2(0.5f, 0.5f), new Vector2(18, -14), new Vector2(50, 40));
        tpl.gameObject.SetActive(false);
        intro.layerTemplate = tpl.gameObject;

        intro.challengeText = Label(bubble.rectTransform, "Reto", "", 26, TextAlignmentOptions.Center, MessagePink);
        intro.challengeText.fontStyle = FontStyles.Bold;
        Place(intro.challengeText.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 92), new Vector2(760, 44));
        intro.hintText = Label(bubble.rectTransform, "Ayuda", "Toca para empezar", 26, TextAlignmentOptions.Center, new Color(0.6f, 0.45f, 0.52f));
        Place(intro.hintText.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 40), new Vector2(760, 40));
        return intro;
    }

    // ------------------------------------------------------------ pausa
    private static void BuildPausePanel(RectTransform safe, GameHud hud)
    {
        RectTransform pause = Overlay(safe, "PausePanel");
        RectTransform pCard = Card(pause, "Tarjeta", new Vector2(0.5f, 0.5f), new Vector2(0, -40), new Vector2(680, 660));
        Peek(pCard, "PersonajeAsomado", "UI/asomado_gatito_normal.png", new Vector2(0.5f, 1f), new Vector2(0, -34), 190);
        RectTransform pList = NewUI("Contenido", pCard);
        Stretch(pList);
        Vertical(pList.gameObject, 24, new RectOffset(70, 70, 60, 50));
        pCard = pList;
        TextMeshProUGUI pTitle = Label(pCard, "Titulo", "Pausa", 76, TextAlignmentOptions.Center, MessagePink);
        pTitle.fontStyle = FontStyles.Bold;
        Height(pTitle, 110);
        hud.resumeButton = NewButton(pCard, "ResumeButton", "Continuar", 46, true); Height(hud.resumeButton, 130);
        hud.pauseRetryButton = NewButton(pCard, "RetryButton", "Reintentar", 42); Height(hud.pauseRetryButton, 120);
        hud.pauseMenuButton = NewButton(pCard, "MenuButton", "Menú", 42); Height(hud.pauseMenuButton, 120);
        hud.pausePanel = pause.gameObject;
    }

    // ------------------------------------------------------------ ¡GANASTE! / ¡PERDISTE!
    private static ResultScreen BuildResultScreen(RectTransform safe)
    {
        RectTransform root = Overlay(safe, "PantallaResultado");   // empieza desactivada
        ResultScreen r = root.gameObject.AddComponent<ResultScreen>();
        r.group = root.gameObject.AddComponent<CanvasGroup>();

        // Rayos de luz girando detrás de la tarjeta
        Image rays = Themed(NewImage(root, "Rayos", Spr("UI/rayos.png"), new Color(1f, 1f, 0.8f, 0.4f)), ThemeKey.Rayos);
        rays.raycastTarget = false;
        Place(rays.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 40), new Vector2(1700, 1700));
        r.rays = rays.rectTransform;

        RectTransform card = Card(root, "Tarjeta", new Vector2(0.5f, 0.5f), new Vector2(0, -45), new Vector2(1280, 800));
        r.card = card;

        // Listón con el título
        Image ribbon = Themed(NewImage(card, "Liston", Spr("UI/cinta.png"), Pink), ThemeKey.Cinta);
        ribbon.raycastTarget = false;
        Place(ribbon.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0, 10), new Vector2(980, 200));
        r.ribbon = ribbon;
        r.winRibbonColor = Color.white;
        r.loseRibbonColor = new Color(0.72f, 0.76f, 0.95f);
        r.titleText = Label(ribbon.rectTransform, "Titulo", "¡Pastelería exitosa!", 62, TextAlignmentOptions.Center, Color.white);
        r.titleText.fontStyle = FontStyles.Bold;
        r.titleText.enableAutoSizing = true;
        r.titleText.fontSizeMin = 30;
        r.titleText.fontSizeMax = 62;
        r.titleText.outlineWidth = 0.22f;
        r.titleText.outlineColor = new Color32(190, 70, 120, 255);
        Stretch(r.titleText.rectTransform, 160, 160, 26, 56);
        r.subtitleText = Label(card, "Subtitulo", "Nivel 1 completado", 34, TextAlignmentOptions.Center, Brown);
        Place(r.subtitleText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -112), new Vector2(900, 50));

        // ======================= CONTENIDO DE VICTORIA =======================
        RectTransform win = NewUI("Victoria", card);
        Stretch(win);
        r.winContent = win.gameObject;
        Peek(win, "PersonajeAsomado", "UI/asomado_conejita_feliz.png", new Vector2(0.9f, 1f), new Vector2(0, -30), 180);

        RectTransform starRow = NewUI("Estrellas", win);
        Place(starRow, new Vector2(0.3f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -170), new Vector2(560, 190));
        r.stars = new Image[3];
        for (int i = 0; i < 3; i++)
        {
            Image st = Themed(NewImage(starRow, "Estrella" + (i + 1), Spr("UI/estrella.png"), new Color(1f, 0.85f, 0.35f)), ThemeKey.Estrella);
            st.preserveAspect = true;
            st.raycastTarget = false;
            float size = i == 1 ? 180 : 140;
            Place(st.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2((i - 1) * 175, i == 1 ? 20 : 0), new Vector2(size, size));
            st.rectTransform.localEulerAngles = new Vector3(0, 0, (1 - i) * 12f);
            r.stars[i] = st;
        }

        r.scoreText = Label(win, "Puntaje", "0", 100, TextAlignmentOptions.Center, MessagePink);
        r.scoreText.fontStyle = FontStyles.Bold;
        r.scoreText.outlineWidth = 0.18f;
        r.scoreText.outlineColor = new Color32(255, 255, 255, 255);
        Place(r.scoreText.rectTransform, new Vector2(0.3f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -20), new Vector2(560, 130));

        Image record = Themed(NewImage(win, "NuevoRecord", Spr("UI/nuevo_record.png"), new Color(1f, 0.8f, 0.3f)), ThemeKey.NuevoRecord);
        record.preserveAspect = true;
        record.raycastTarget = false;
        Place(record.rectTransform, new Vector2(0.3f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(250, 50), new Vector2(230, 128));
        record.rectTransform.localEulerAngles = new Vector3(0, 0, -14f);
        r.newRecordBadge = record.gameObject;

        // Clientes felices (uno por pedido)
        RectTransform happy = NewUI("ClientesFelices", win);
        Place(happy, new Vector2(0.3f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 175), new Vector2(560, 150));
        HorizontalLayoutGroup hh = happy.gameObject.AddComponent<HorizontalLayoutGroup>();
        hh.spacing = 14;
        hh.childAlignment = TextAnchor.MiddleCenter;
        hh.childControlWidth = hh.childControlHeight = false;
        hh.childForceExpandWidth = hh.childForceExpandHeight = false;
        Image custTpl = NewImage(happy, "ClienteEjemplo", Spr("Clientes/conejita_feliz.png"), Color.white);
        custTpl.preserveAspect = true;
        custTpl.raycastTarget = false;
        custTpl.rectTransform.sizeDelta = new Vector2(140, 140);
        custTpl.gameObject.SetActive(false);
        r.customersRow = happy;
        r.customerTemplate = custTpl;

        // Estadísticas (a la derecha)
        RectTransform statsBox = NewUI("Estadisticas", win);
        Image statsBg = statsBox.gameObject.AddComponent<Image>();
        statsBg.color = new Color(1f, 0.92f, 0.95f, 0.95f);
        statsBg.sprite = Spr("UI/input.png");
        statsBg.type = Image.Type.Sliced;
        statsBg.raycastTarget = false;
        Place(statsBox, new Vector2(0.75f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-10, 10), new Vector2(520, 430));
        VerticalLayoutGroup sv = statsBox.gameObject.AddComponent<VerticalLayoutGroup>();
        sv.spacing = 8;
        sv.padding = new RectOffset(24, 24, 20, 20);
        sv.childControlWidth = sv.childControlHeight = true;
        sv.childForceExpandWidth = true;
        sv.childForceExpandHeight = false;
        r.statsParent = statsBox;
        RectTransform statRow = NewUI("FilaEjemplo", statsBox);
        statRow.gameObject.AddComponent<LayoutElement>().preferredHeight = 58;
        Image sIcon = NewImage(statRow, "Icono", Spr("UI/icono_pedido.png"), Pink);
        sIcon.preserveAspect = true;
        sIcon.raycastTarget = false;
        Place(sIcon.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), Vector2.zero, new Vector2(50, 50));
        TextMeshProUGUI sLabel = Label(statRow, "Texto", "Pedidos", 28, TextAlignmentOptions.MidlineLeft, Brown);
        Place(sLabel.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(64, 0), new Vector2(280, 50));
        TextMeshProUGUI sValue = Label(statRow, "Valor", "+0", 30, TextAlignmentOptions.MidlineRight, MessagePink);
        sValue.fontStyle = FontStyles.Bold;
        Place(sValue.rectTransform, new Vector2(1, 0.5f), new Vector2(1, 0.5f), Vector2.zero, new Vector2(140, 50));
        statRow.gameObject.SetActive(false);
        r.statRowTemplate = statRow.gameObject;

        r.footerText = Label(win, "Pie", "", 26, TextAlignmentOptions.Center, new Color(0.6f, 0.45f, 0.52f));
        Place(r.footerText.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 150), new Vector2(1100, 40));

        RectTransform wRow = NewUI("Botones", win);
        Place(wRow, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 26), new Vector2(1160, 120));
        HorizontalLayoutGroup wh = wRow.gameObject.AddComponent<HorizontalLayoutGroup>();
        wh.spacing = 24;
        wh.childControlWidth = wh.childControlHeight = true;
        wh.childForceExpandWidth = wh.childForceExpandHeight = true;
        r.menuButton = NewButton(wRow, "MenuButton", "Mapa", 42);
        r.retryButton = NewButton(wRow, "RetryButton", "Repetir", 42);
        r.nextButton = NewButton(wRow, "NextLevelButton", "¡Siguiente nivel!", 42, true);

        // ======================= CONTENIDO DE DERROTA =======================
        RectTransform lose = NewUI("Derrota", card);
        Stretch(lose);
        r.loseContent = lose.gameObject;
        Peek(lose, "PersonajeAsomado", "UI/asomado_panda_enojado.png", new Vector2(0.9f, 1f), new Vector2(0, -30), 180);

        Image sad = NewImage(lose, "ClienteEnojado", Spr("Clientes/conejita_enojado.png"), Color.white);
        sad.preserveAspect = true;
        sad.raycastTarget = false;
        Place(sad.rectTransform, new Vector2(0.25f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 10), new Vector2(380, 400));
        r.loseCustomer = sad;

        r.loseReasonText = Label(lose, "Motivo", "¡Te quedaste sin corazones!", 44, TextAlignmentOptions.Center, MessagePink);
        r.loseReasonText.fontStyle = FontStyles.Bold;
        r.loseReasonText.enableAutoSizing = true;
        r.loseReasonText.fontSizeMin = 26;
        r.loseReasonText.fontSizeMax = 44;
        Place(r.loseReasonText.rectTransform, new Vector2(0.68f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -180), new Vector2(680, 110));

        RectTransform prog = NewUI("Progreso", lose);
        Place(prog, new Vector2(0.68f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 40), new Vector2(620, 110));
        Image progBg = BarBack(prog);
        Place(progBg.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(620, 56));
        Image progFill = BarFill(progBg.rectTransform);
        progFill.fillAmount = 0.3f;
        r.loseProgressFill = progFill;
        r.loseProgressText = Label(prog, "Texto", "Pedidos: 1 / 3", 32, TextAlignmentOptions.Center, Brown);
        r.loseProgressText.fontStyle = FontStyles.Bold;
        Place(r.loseProgressText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(620, 50));

        RectTransform tipBox = Card(lose, "Consejo", new Vector2(0.68f, 0.5f), new Vector2(0, -110), new Vector2(660, 140));
        tipBox.GetComponent<Image>().color = new Color(1f, 0.96f, 0.8f);
        r.tipText = Label(tipBox, "Texto", "Consejo: suelta el pastel cuando esté justo encima.", 28, TextAlignmentOptions.Center, Brown);
        r.tipText.enableAutoSizing = true;
        r.tipText.fontSizeMin = 18;
        r.tipText.fontSizeMax = 28;
        Stretch(r.tipText.rectTransform, 24, 24, 12, 12);

        RectTransform lRow = NewUI("Botones", lose);
        Place(lRow, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 26), new Vector2(900, 120));
        HorizontalLayoutGroup lh = lRow.gameObject.AddComponent<HorizontalLayoutGroup>();
        lh.spacing = 30;
        lh.childControlWidth = lh.childControlHeight = true;
        lh.childForceExpandWidth = lh.childForceExpandHeight = true;
        r.loseMenuButton = NewButton(lRow, "MenuButton", "Mapa", 44);
        r.loseRetryButton = NewButton(lRow, "RetryButton", "¡Otra vez!", 44, true);

        // Confeti de la interfaz (encima de todo)
        RectTransform conf = NewUI("Confeti", root);
        Stretch(conf);
        r.confettiLayer = conf;
        r.confettiSprites = new[] { Spr("Efectos/confeti.png"), Spr("Efectos/brillo.png"), Spr("Efectos/corazon_mini.png"), Spr("UI/estrella.png") }
            .Where(s => s != null).ToArray();

        ribbon.transform.SetAsLastSibling();   // el listón queda por encima de los personajes asomados
        lose.gameObject.SetActive(false);
        return r;
    }

    // =====================================================================
    //  LOGIN
    // =====================================================================

    private static void BuildLoginScene()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        MakeCamera(SkyPink);
        MakeEventSystem();
        Instantiate(PF + "AudioManager.prefab");
        new GameObject("FirebaseManager").AddComponent<FirebaseManager>();
        new GameObject("Musica").AddComponent<SceneMusic>();

        RectTransform safe = MakeCanvas("Canvas", Spr("UI/fondo_menu.png"), ThemeKey.FondoLogin);
        FallingStrawberries(safe.parent, 22);

        // Logo grande que flota + personajes saludando
        Image logo = Themed(NewImage(safe, "Logo", Spr("UI/logo.png"), Color.white), ThemeKey.Logo);
        logo.preserveAspect = true;
        logo.raycastTarget = false;
        Place(logo.rectTransform, new Vector2(0.3f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 150), new Vector2(900, 520));
        Float(logo.gameObject, 14f, 1.4f, 0.025f, 1.5f);
        Character(safe, "Gatito", "Clientes/gatito_feliz.png", new Vector2(0.17f, 0f), new Vector2(0, 30), 330, 0.2f);
        Character(safe, "Perrito", "Clientes/perrito_feliz.png", new Vector2(0.43f, 0f), new Vector2(0, 30), 330, 1.1f);

        // Tarjeta del login con la conejita asomada
        RectTransform card = Card(safe, "PanelLogin", new Vector2(0.73f, 0.5f), new Vector2(0, -50), new Vector2(780, 880));
        Peek(card, "PersonajeAsomado", "UI/asomado_conejita_feliz.png", new Vector2(0.5f, 1f), new Vector2(0, -34), 175);
        RectTransform form = NewUI("Formulario", card);
        Stretch(form);
        Vertical(form.gameObject, 18, new RectOffset(60, 60, 60, 40));
        TextMeshProUGUI title = Label(form, "Titulo", "¡Bienvenida a Sugaru!", 50, TextAlignmentOptions.Center, MessagePink);
        title.fontStyle = FontStyles.Bold;
        Height(title, 86);
        TMP_InputField user = NewInputField(form, "UsernameInput", "Nombre (solo para registrarse)", TMP_InputField.ContentType.Standard);
        TMP_InputField email = NewInputField(form, "EmailInput", "Correo electrónico", TMP_InputField.ContentType.EmailAddress);
        TMP_InputField pass = NewInputField(form, "PasswordInput", "Contraseña (mínimo 6)", TMP_InputField.ContentType.Password);
        RectTransform row = ButtonRow(form, 130);
        Button register = NewButton(row, "RegisterButton", "Registrarse", 40);
        Button login = NewButton(row, "LoginButton", "Entrar", 44, true);
        TextMeshProUGUI message = Label(form, "MessageText", "", 30, TextAlignmentOptions.Center, Brown);
        Height(message, 100);

        LoginUI ui = new GameObject("LoginUI").AddComponent<LoginUI>();
        ui.usernameInput = user;
        ui.emailInput = email;
        ui.passwordInput = pass;
        ui.registerButton = register;
        ui.loginButton = login;
        ui.messageText = message;

        EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), SC + "LoginScene.unity");
    }

    // =====================================================================
    //  MENÚ PRINCIPAL + MAPA DE NIVELES (camino tipo Candy Crush)
    // =====================================================================

    // Posición de cada nivel en el mapa (x desde la izquierda, y desde el centro). Muévelos luego en la escena.
    private static readonly Vector2[] MapPoints =
    {
        new Vector2(330, -210), new Vector2(700, 90), new Vector2(1080, 250), new Vector2(1460, -40), new Vector2(1820, -240),
        new Vector2(2200, 40), new Vector2(2560, 250), new Vector2(2940, 0), new Vector2(3320, -230), new Vector2(3740, 130),
    };
    private const float MapWidth = 4200f;
    internal const float MapSideExtra = 400f;

    private static void BuildMainMenuScene()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        MakeCamera(SkyPink);
        MakeEventSystem();
        Instantiate(PF + "AudioManager.prefab");
        new GameObject("Musica").AddComponent<SceneMusic>();
        RectTransform safe = MakeCanvas("Canvas", Spr("UI/fondo_menu.png"), ThemeKey.FondoMenu);
        FallingStrawberries(safe.parent, 14);

        // ---------------- INICIO ----------------
        RectTransform home = NewUI("HomePanel", safe);
        Stretch(home);
        Image logo = Themed(NewImage(home, "Logo", Spr("UI/logo.png"), Color.white), ThemeKey.Logo);
        logo.preserveAspect = true;
        logo.raycastTarget = false;
        Place(logo.rectTransform, new Vector2(0.3f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -12), new Vector2(760, 400));
        Float(logo.gameObject, 10f, 1.4f, 0.02f, 1.2f);

        RectTransform info = Card(home, "InfoJugador", new Vector2(0.3f, 0.3f), Vector2.zero, new Vector2(760, 500));
        Vertical(info.gameObject, 10, new RectOffset(50, 50, 30, 30));
        Image avatar = Themed(NewImage(info, "Avatar", Spr("UI/avatar.png"), Color.white), ThemeKey.Avatar);
        avatar.preserveAspect = true;
        Height(avatar, 150);
        TextMeshProUGUI username = Label(info, "UsernameText", "Jugador", 56, TextAlignmentOptions.Center, MessagePink);
        username.fontStyle = FontStyles.Bold;
        Height(username, 72);
        TextMeshProUGUI total = Label(info, "TotalScoreText", "Puntaje total: 0", 40, TextAlignmentOptions.Center, Brown);
        Height(total, 54);
        TextMeshProUGUI last = Label(info, "LastScoreText", "Última partida: 0", 34, TextAlignmentOptions.Center, Brown);
        Height(last, 46);
        TextMeshProUGUI maxLevel = Label(info, "MaxLevelText", "Nivel máximo: 1 / 10", 34, TextAlignmentOptions.Center, Brown);
        Height(maxLevel, 46);

        RectTransform buttons = NewUI("Botones", home);
        Place(buttons, new Vector2(0.73f, 0.45f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(600, 640));
        Image dogPeek = Peek(buttons, "PersonajeAsomado", "UI/asomado_perrito_feliz.png", new Vector2(0.5f, 1f), new Vector2(0, -24), 170);
        RectTransform btnList = NewUI("Lista", buttons);
        Stretch(btnList);
        Vertical(btnList.gameObject, 22, new RectOffset(0, 0, 0, 0));
        Button play = NewButton(btnList, "PlayButton", "Jugar", 72, true); Height(play, 180);
        Button ranking = NewButton(btnList, "RankingButton", "Ranking", 46); Height(ranking, 128);
        Button settings = NewButton(btnList, "SettingsButton", "Configuraciones", 44); Height(settings, 128);
        Button logout = NewButton(btnList, "LogoutButton", "Salir", 42); Height(logout, 112);
        dogPeek.transform.SetAsLastSibling();   // las patitas quedan encima del botón Jugar
        Character(home, "Panda", "Clientes/panda_feliz.png", new Vector2(0.97f, 0f), new Vector2(-40, 10), 250, 0.6f);

        // ---------------- MAPA DE NIVELES ----------------
        RectTransform mapPanel = NewUI("MapaPanel", safe);
        Stretch(mapPanel);
        FullScreenBack(mapPanel, new Color(0.72f, 0.88f, 1f));   // cubre toda la pantalla (también los bordes del notch)

        // Scroll horizontal
        RectTransform scrollRt = NewUI("Scroll", mapPanel);
        Stretch(scrollRt);
        scrollRt.gameObject.AddComponent<CubrirPantalla>().margen = 0f;   // el mapa llega hasta los bordes de la pantalla
        ScrollRect scroll = scrollRt.gameObject.AddComponent<ScrollRect>();
        scroll.horizontal = true;
        scroll.vertical = false;
        scroll.movementType = ScrollRect.MovementType.Elastic;
        scroll.inertia = true;
        scroll.decelerationRate = 0.12f;
        scroll.scrollSensitivity = 40f;
        RectTransform viewport = NewUI("Viewport", scrollRt);
        Stretch(viewport);
        viewport.gameObject.AddComponent<RectMask2D>();
        Image vpImg = viewport.gameObject.AddComponent<Image>();
        vpImg.color = new Color(1f, 1f, 1f, 0f);   // para poder arrastrar desde cualquier parte
        scroll.viewport = viewport;

        RectTransform content = NewUI("Contenido", viewport);
        content.anchorMin = new Vector2(0f, 0f);
        content.anchorMax = new Vector2(0f, 1f);
        content.pivot = new Vector2(0f, 0.5f);
        content.anchoredPosition = Vector2.zero;
        content.sizeDelta = new Vector2(MapWidth, 0f);
        scroll.content = content;

        Image mapBg = Themed(NewImage(content, "FondoMapa", Spr("UI/fondo_mapa.png"), new Color(0.75f, 0.9f, 1f)), ThemeKey.FondoMapa);
        mapBg.raycastTarget = false;
        Stretch(mapBg.rectTransform);
        mapBg.rectTransform.offsetMin = new Vector2(-MapSideExtra, 0f);    // más grande a los lados: nunca se ve lo de atrás
        mapBg.rectTransform.offsetMax = new Vector2(MapSideExtra, 0f);

        RectTransform path = NewUI("Camino", content);
        Stretch(path);
        RectTransform points = NewUI("Posiciones", content);
        Stretch(points);
        RectTransform[] levelPoints = new RectTransform[MapPoints.Length];
        for (int i = 0; i < MapPoints.Length; i++)
        {
            Image p = NewImage(points, $"Posicion_Nivel_{i + 1:00}", null, new Color(1f, 0.4f, 0.6f, 0.35f));
            p.raycastTarget = false;
            Place(p.rectTransform, new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), MapPoints[i], new Vector2(200, 200));
            levelPoints[i] = p.rectTransform;
        }
        RectTransform nodes = NewUI("Niveles", content);
        Stretch(nodes);

        Image marker = Themed(NewImage(content, "MarcadorJugador", Spr("UI/marcador.png"), Pink), ThemeKey.MarcadorJugador);
        marker.preserveAspect = true;
        marker.raycastTarget = false;
        Place(marker.rectTransform, new Vector2(0f, 0.5f), new Vector2(0.5f, 0f), MapPoints[0] + new Vector2(0, 150), new Vector2(110, 140));

        // Barra superior: volver + título
        Button mapBack2 = IconButton(mapPanel, "BackButton", ThemeKey.BotonCerrar, "X");
        Place((RectTransform)mapBack2.transform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(24, -20), new Vector2(110, 110));
        Image mapTitle = Themed(NewImage(mapPanel, "Titulo", Spr("UI/cinta.png"), Pink), ThemeKey.Cinta);
        mapTitle.raycastTarget = false;
        Place(mapTitle.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -6), new Vector2(760, 170));
        TextMeshProUGUI mapTitleText = Label(mapTitle.rectTransform, "Texto", "Camino de Pasteles", 48, TextAlignmentOptions.Center, Color.white);
        mapTitleText.fontStyle = FontStyles.Bold;
        mapTitleText.outlineWidth = 0.2f;
        mapTitleText.outlineColor = new Color32(200, 80, 130, 255);
        Stretch(mapTitleText.rectTransform, 120, 120, 20, 46);

        LevelInfoPopup popup = BuildLevelPopup(safe);

        LevelMapUI map = mapPanel.gameObject.AddComponent<LevelMapUI>();
        map.scroll = scroll;
        map.content = content;
        map.levelPoints = levelPoints;
        map.nodePrefab = LoadC<LevelNodeUI>(PFU + "NodoNivel.prefab");
        map.nodesParent = nodes;
        map.pathParent = path;
        map.playerMarker = marker.rectTransform;
        map.popup = popup;

        // ---------------- RANKING ----------------
        RectTransform rankingPanel = NewUI("RankingPanel", safe);
        Stretch(rankingPanel);
        RectTransform rCard = Card(rankingPanel, "Tarjeta", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1300, 1000));
        TextMeshProUGUI rTitle = Label(rCard, "Titulo", "Ranking", 74, TextAlignmentOptions.Center, Brown);
        rTitle.fontStyle = FontStyles.Bold;
        Place(rTitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -20), new Vector2(800, 100));
        RectTransform header = NewUI("Encabezado", rCard);
        Place(header, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -125), new Vector2(1120, 56));
        HorizontalLayoutGroup hh = header.gameObject.AddComponent<HorizontalLayoutGroup>();
        hh.padding = new RectOffset(20, 20, 0, 0);
        hh.childControlWidth = hh.childControlHeight = true;
        hh.childForceExpandWidth = false;
        hh.childForceExpandHeight = true;
        RowCell(header, "Pos", "#", 0.12f, FontStyles.Bold);
        RowCell(header, "Jugador", "Jugador", 0.46f, FontStyles.Bold);
        RowCell(header, "Puntaje", "Puntaje", 0.24f, FontStyles.Bold);
        RowCell(header, "Nivel", "Nivel", 0.18f, FontStyles.Bold);

        GameObject rscroll = DefaultControls.CreateScrollView(new DefaultControls.Resources());
        rscroll.name = "ScrollView";
        rscroll.transform.SetParent(rCard, false);
        Stretch((RectTransform)rscroll.transform, 60, 60, 190, 170);
        rscroll.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.15f);
        ScrollRect sr = rscroll.GetComponent<ScrollRect>();
        sr.horizontal = false;
        if (sr.horizontalScrollbar != null) Object.DestroyImmediate(sr.horizontalScrollbar.gameObject);
        sr.horizontalScrollbar = null;
        RectTransform rcontent = sr.content;
        VerticalLayoutGroup cv = rcontent.gameObject.AddComponent<VerticalLayoutGroup>();
        cv.spacing = 8;
        cv.childControlWidth = cv.childControlHeight = true;
        cv.childForceExpandWidth = true;
        cv.childForceExpandHeight = false;
        rcontent.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        TextMeshProUGUI status = Label(rCard, "StatusText", "", 38, TextAlignmentOptions.Center, Brown);
        Place(status.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(800, 90));
        Button rankingBack = NewButton(rCard, "BackButton", "Volver", 44);
        Place((RectTransform)rankingBack.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 30), new Vector2(380, 120));
        RankingUI rankingUI = rankingPanel.gameObject.AddComponent<RankingUI>();
        rankingUI.rowsContainer = rcontent;
        rankingUI.rowPrefab = LoadC<RankingRowUI>(PFU + "RankingRow.prefab");
        rankingUI.statusText = status;

        // ---------------- AJUSTES ----------------
        RectTransform settingsPanel = NewUI("SettingsPanel", safe);
        Stretch(settingsPanel);
        RectTransform sCard = Card(settingsPanel, "Tarjeta", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1000, 960));
        Vertical(sCard.gameObject, 18, new RectOffset(80, 80, 40, 40));
        TextMeshProUGUI sTitle = Label(sCard, "Titulo", "Ajustes", 74, TextAlignmentOptions.Center, Brown);
        sTitle.fontStyle = FontStyles.Bold;
        Height(sTitle, 110);
        Height(Label(sCard, "TextoMusica", "Música", 44, TextAlignmentOptions.Left, Brown), 60);
        Slider musicSlider = NewSlider(sCard, "MusicSlider");
        Height(Label(sCard, "TextoEfectos", "Efectos de sonido", 44, TextAlignmentOptions.Left, Brown), 60);
        Slider sfxSlider = NewSlider(sCard, "SfxSlider");
        Toggle vibration = NewToggle(sCard, "VibrationToggle", "Vibración");
        Button settingsBack = NewButton(sCard, "BackButton", "Volver", 44);
        Height(settingsBack, 120);
        SettingsUI settingsUI = settingsPanel.gameObject.AddComponent<SettingsUI>();
        settingsUI.musicSlider = musicSlider;
        settingsUI.sfxSlider = sfxSlider;
        settingsUI.vibrationToggle = vibration;

        // ---------------- CONTROLADOR ----------------
        MainMenuUI menu = new GameObject("MainMenuUI").AddComponent<MainMenuUI>();
        menu.usernameText = username;
        menu.totalScoreText = total;
        menu.lastScoreText = last;
        menu.maxLevelText = maxLevel;
        menu.homePanel = home.gameObject;
        menu.levelsPanel = mapPanel.gameObject;
        menu.rankingPanel = rankingPanel.gameObject;
        menu.settingsPanel = settingsPanel.gameObject;
        menu.playButton = play;
        menu.rankingButton = ranking;
        menu.settingsButton = settings;
        menu.logoutButton = logout;
        menu.backButtons = new[] { mapBack2, rankingBack, settingsBack };
        menu.levelSelect = map;

        popup.transform.SetAsLastSibling();
        mapPanel.gameObject.SetActive(false);
        rankingPanel.gameObject.SetActive(false);
        settingsPanel.gameObject.SetActive(false);
        popup.gameObject.SetActive(false);

        PanelTools.SetupOpenScene();   // paneles editables + estado correcto al jugar
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), SC + "MainMenu.unity");
    }

    /// <summary>Ventana que sale al tocar un nivel del mapa.</summary>
    private static LevelInfoPopup BuildLevelPopup(RectTransform safe)
    {
        RectTransform root = Overlay(safe, "VentanaNivel");
        root.gameObject.SetActive(true);
        LevelInfoPopup p = root.gameObject.AddComponent<LevelInfoPopup>();

        RectTransform card = Card(root, "Tarjeta", new Vector2(0.5f, 0.5f), new Vector2(0, -30), new Vector2(1000, 760));
        p.card = card;
        Peek(card, "PersonajeAsomado", "UI/asomado_perrito_feliz.png", new Vector2(0.1f, 1f), new Vector2(0, -30), 170);

        Image ribbon = Themed(NewImage(card, "Liston", Spr("UI/cinta.png"), Pink), ThemeKey.Cinta);
        ribbon.raycastTarget = false;
        Place(ribbon.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0, 0), new Vector2(760, 170));
        p.numberText = Label(ribbon.rectTransform, "Numero", "Nivel 1", 52, TextAlignmentOptions.Center, Color.white);
        p.numberText.fontStyle = FontStyles.Bold;
        p.numberText.outlineWidth = 0.2f;
        p.numberText.outlineColor = new Color32(200, 80, 130, 255);
        Stretch(p.numberText.rectTransform, 120, 120, 20, 46);

        p.titleText = Label(card, "Titulo", "Pastelería Rosa", 44, TextAlignmentOptions.Center, MessagePink);
        p.titleText.fontStyle = FontStyles.Bold;
        Place(p.titleText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -96), new Vector2(900, 60));

        p.thumbnail = NewImage(card, "Miniatura", Spr("Escenario/miniatura_nivel_01.png"), Color.white);
        p.thumbnail.preserveAspect = true;
        p.thumbnail.raycastTarget = false;
        p.thumbnail.gameObject.AddComponent<CatalogImageNote>().source = CatalogImageNote.Source.MiniaturaDeNivel;
        Place(p.thumbnail.rectTransform, new Vector2(0.27f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 10), new Vector2(380, 280));

        RectTransform starRow = NewUI("Estrellas", card);
        Place(starRow, new Vector2(0.68f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -170), new Vector2(420, 120));
        p.stars = new Image[3];
        for (int i = 0; i < 3; i++)
        {
            Image s = NewImage(starRow, "Estrella" + (i + 1), Spr("UI/estrella.png"), new Color(1f, 0.85f, 0.35f));
            s.preserveAspect = true;
            s.raycastTarget = false;
            float size = i == 1 ? 116 : 92;
            Place(s.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2((i - 1) * 120, i == 1 ? 12 : 0), new Vector2(size, size));
            p.stars[i] = s;
        }

        RectTransform chBox = Card(card, "Reto", new Vector2(0.68f, 0.5f), new Vector2(0, -10), new Vector2(520, 190));
        chBox.GetComponent<Image>().color = new Color(1f, 0.93f, 0.96f);
        TextMeshProUGUI chTitle = Label(chBox, "Encabezado", "Reto del nivel", 28, TextAlignmentOptions.Top, MessagePink);
        chTitle.fontStyle = FontStyles.Bold;
        Stretch(chTitle.rectTransform, 20, 20, 14, 130);
        p.challengeText = Label(chBox, "Texto", "", 28, TextAlignmentOptions.Center, Brown);
        p.challengeText.enableAutoSizing = true;
        p.challengeText.fontSizeMin = 18;
        p.challengeText.fontSizeMax = 30;
        Stretch(p.challengeText.rectTransform, 22, 22, 56, 14);

        p.bestText = Label(card, "Mejor", "Mejor puntaje: 0", 30, TextAlignmentOptions.Center, Brown);
        Place(p.bestText.rectTransform, new Vector2(0.68f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 190), new Vector2(520, 50));

        p.playButton = NewButton(card, "PlayButton", "¡Jugar!", 56, true);
        Place((RectTransform)p.playButton.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 36), new Vector2(460, 140));

        ribbon.transform.SetAsLastSibling();
        p.closeButton = IconButton(card, "CloseButton", ThemeKey.BotonCerrar, "X");
        Place((RectTransform)p.closeButton.transform, new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(-20, -20), new Vector2(100, 100));
        return p;
    }

    // =====================================================================
    //  AYUDANTES: ESCENA
    // =====================================================================

    private static GameObject Instantiate(string prefabPath)
    {
        GameObject prefab = Load<GameObject>(prefabPath);
        if (prefab == null)
        {
            Debug.LogError("Falta el prefab " + prefabPath);
            return new GameObject(Path.GetFileNameWithoutExtension(prefabPath));
        }
        return (GameObject)PrefabUtility.InstantiatePrefab(prefab);
    }

    private static Camera MakeCamera(Color background)
    {
        GameObject go = new GameObject("Main Camera");
        go.tag = "MainCamera";
        Camera cam = go.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 5f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = background;
        cam.nearClipPlane = 0.3f;
        cam.farClipPlane = 100f;
        go.transform.position = new Vector3(0, 0, -10);
        go.AddComponent<AudioListener>();
        return cam;
    }

    private static void MakeEventSystem()
    {
        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
    }

    private static GameObject NewCanvasObject(string name)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        go.layer = 5;
        go.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler cs = go.GetComponent<CanvasScaler>();
        cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1920, 1080);
        cs.matchWidthOrHeight = 0.5f;   // celular horizontal
        return go;
    }

    /// <summary>Canvas horizontal 1920x1080 con fondo opcional (del TemaVisual) y un hijo SafeArea donde va todo.</summary>
    private static RectTransform MakeCanvas(string name, Sprite background, ThemeKey backgroundKey)
    {
        GameObject go = NewCanvasObject(name);
        Sprite themed = background != null ? (UITheme.Get(backgroundKey) ?? background) : null;
        if (themed != null)
        {
            Image bg = NewImage(go.transform, "Fondo", themed, Color.white);
            bg.raycastTarget = false;
            Stretch(bg.rectTransform);
            AspectRatioFitter arf = bg.gameObject.AddComponent<AspectRatioFitter>();
            arf.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            arf.aspectRatio = themed.rect.width / themed.rect.height;
            bg.gameObject.AddComponent<ThemedImage>().key = backgroundKey;
        }
        RectTransform safe = NewUI("SafeArea", go.transform);
        Stretch(safe);
        safe.gameObject.AddComponent<SafeArea>();
        return safe;
    }

    // =====================================================================
    //  AYUDANTES: UI
    // =====================================================================

    // ---------------------------------------------------------------- barras (estilo Sugaru)
    private static Image BarBack(Transform parent)
    {
        Image bg = NewImage(parent, "Fondo", Spr("UI/barra_fondo.png"), Cream);
        bg.raycastTarget = false;
        return bg;
    }

    /// <summary>Relleno rosado con rayitas que se llena de izquierda a derecha.</summary>
    private static Image BarFill(RectTransform back)
    {
        Image fill = NewImage(back, "Relleno", Spr("UI/barra_relleno.png"), Pink);
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        fill.fillOrigin = (int)Image.OriginHorizontal.Left;
        fill.raycastTarget = false;
        Stretch(fill.rectTransform, 3, 3, 3, 3);
        return fill;
    }

    // ---------------------------------------------------------------- estilo Sugaru

    /// <summary>Fresas (y estrellitas/corazones) que caen en el fondo del login y del menú.</summary>
    private static void FallingStrawberries(Transform canvasRoot, int count)
    {
        RectTransform layer = NewUI("FresasCayendo", canvasRoot);
        Stretch(layer);
        layer.SetSiblingIndex(1);   // encima del fondo, detrás de todo lo demás
        FresasFlotantes f = layer.gameObject.AddComponent<FresasFlotantes>();
        f.imagenes = new[] { Spr("UI/fresa.png"), Spr("UI/fresa_cara.png"), Spr("UI/fresa.png"), Spr("UI/estrella.png"), Spr("UI/corazon_lleno.png") }
            .Where(x => x != null).ToArray();
        f.cantidad = count;
    }

    /// <summary>Personaje que se asoma por encima de una ventana (cabeza + patitas sobre el borde).</summary>
    private static Image Peek(RectTransform parent, string name, string sprite, Vector2 anchor, Vector2 pos, float height)
    {
        Sprite sp = Spr(sprite);
        Image img = NewImage(parent, name, sp, Color.white);
        img.preserveAspect = true;
        img.raycastTarget = false;
        float w = sp != null ? height * sp.rect.width / sp.rect.height : height;
        img.rectTransform.anchorMin = img.rectTransform.anchorMax = anchor;
        img.rectTransform.pivot = new Vector2(0.5f, 0f);
        img.rectTransform.anchoredPosition = pos;
        img.rectTransform.sizeDelta = new Vector2(w, height);
        img.transform.SetAsFirstSibling();   // detrás de lo demás de la ventana: las patitas quedan sobre el borde
        Float(img.gameObject, 5f, 1.8f, 0.02f, 1.5f);
        return img;
    }

    /// <summary>Personaje de cuerpo entero que se balancea (decoración del login y el menú).</summary>
    private static Image Character(Transform parent, string name, string sprite, Vector2 anchor, Vector2 pos, float height, float phase)
    {
        Sprite sp = Spr(sprite);
        Image img = NewImage(parent, name, sp, Color.white);
        img.preserveAspect = true;
        img.raycastTarget = false;
        float w = sp != null ? height * sp.rect.width / sp.rect.height : height;
        img.rectTransform.anchorMin = img.rectTransform.anchorMax = anchor;
        img.rectTransform.pivot = new Vector2(0.5f, 0f);
        img.rectTransform.anchoredPosition = pos;
        img.rectTransform.sizeDelta = new Vector2(w, height);
        Flotar fl = Float(img.gameObject, 8f, 2.2f + phase * 0.3f, 0.03f, 3f);
        fl.rebotarAlAparecer = true;
        return img;
    }

    private static Flotar Float(GameObject go, float altura, float velocidad, float respiro, float balanceo)
    {
        Flotar f = go.AddComponent<Flotar>();
        f.altura = altura;
        f.velocidad = velocidad;
        f.respiro = respiro;
        f.balanceo = balanceo;
        return f;
    }

    /// <summary>"Pastilla" del HUD: fondo crema redondeado con un ícono a la izquierda.</summary>
    private static RectTransform Pill(Transform parent, string name, Vector2 pos, Vector2 size, string icon, ThemeKey iconKey)
    {
        Image bg = Themed(NewImage(parent, name, Spr("UI/boton.png"), Cream), ThemeKey.Boton);
        bg.raycastTarget = false;
        RectTransform rt = bg.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
        rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        if (icon != null)
        {
            Image ic = Themed(NewImage(rt, "Icono", Spr(icon), Pink), iconKey);
            ic.preserveAspect = true;
            ic.raycastTarget = false;
            Place(ic.rectTransform, new Vector2(0, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(50, 4), new Vector2(size.y * 0.95f, size.y * 0.95f));
        }
        return rt;
    }

    /// <summary>Hace que esta imagen tome su sprite del TemaVisual (casilla "key").</summary>
    private static Image Themed(Image img, ThemeKey key)
    {
        Sprite s = UITheme.Get(key);
        if (s != null) SetImage(img, s, img.color);
        ThemedImage t = img.gameObject.AddComponent<ThemedImage>();
        t.key = key;
        t.MarkApplied();
        return img;
    }

    private static RectTransform NewUI(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.layer = 5;
        if (parent != null) go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    private static Image NewImage(Transform parent, string name, Sprite sprite, Color fallback)
    {
        Image img = NewUI(name, parent).gameObject.AddComponent<Image>();
        SetImage(img, sprite, fallback);
        return img;
    }

    private static void SetImage(Image img, Sprite sprite, Color fallback)
    {
        img.sprite = sprite;
        img.color = sprite != null ? Color.white : fallback;
        img.type = sprite != null && sprite.border != Vector4.zero ? Image.Type.Sliced : Image.Type.Simple;
    }

    private static TextMeshProUGUI Label(Transform parent, string name, string text, float size, TextAlignmentOptions align, Color color)
    {
        GameObject go = TMP_DefaultControls.CreateText(new TMP_DefaultControls.Resources());
        go.name = name;
        go.layer = 5;
        go.transform.SetParent(parent, false);
        TextMeshProUGUI t = go.GetComponent<TextMeshProUGUI>();
        t.text = text;
        t.fontSize = size;
        t.alignment = align;
        t.color = color;
        t.raycastTarget = false;
        return t;
    }

    private static TMP_DefaultControls.Resources TmpResources()
    {
        return new TMP_DefaultControls.Resources
        {
            standard = Spr("UI/boton.png"),
            background = Spr("UI/panel.png"),
            inputField = Spr("UI/input.png")
        };
    }

    private static Button NewButton(Transform parent, string name, string label, float fontSize, bool green = false)
    {
        GameObject go = TMP_DefaultControls.CreateButton(TmpResources());
        go.name = name;
        go.layer = 5;
        go.transform.SetParent(parent, false);
        Image img = go.GetComponent<Image>();
        SetImage(img, Spr(green ? "UI/boton_verde.png" : "UI/boton.png"), green ? Green : Pink);
        Themed(img, green ? ThemeKey.BotonVerde : ThemeKey.Boton);
        TMP_Text t = go.GetComponentInChildren<TMP_Text>();
        t.text = label;
        t.fontSize = fontSize;
        t.fontStyle = FontStyles.Bold;
        t.color = green ? Color.white : ButtonText;   // principal: blanco · secundario (crema): rosado oscuro
        if (green)
        {
            t.outlineWidth = 0.12f;
            t.outlineColor = new Color32(205, 70, 112, 255);
        }
        t.enableAutoSizing = true;      // si el texto no cabe, se achica solo
        t.fontSizeMin = 18;
        t.fontSizeMax = fontSize;
        t.margin = new Vector4(16, 4, 16, 4);
        return go.GetComponent<Button>();
    }

    private static Button IconButton(Transform parent, string name, ThemeKey key, string fallbackText)
    {
        Sprite icon = UITheme.Get(key);
        Button b = NewButton(parent, name, icon != null ? "" : fallbackText, 60);
        Image img = (Image)b.targetGraphic;
        Object.DestroyImmediate(img.GetComponent<ThemedImage>());
        if (icon != null) SetImage(img, icon, Color.white);
        img.preserveAspect = true;
        img.gameObject.AddComponent<ThemedImage>().key = key;
        return b;
    }

    private static TMP_InputField NewInputField(Transform parent, string name, string placeholder, TMP_InputField.ContentType type)
    {
        GameObject go = TMP_DefaultControls.CreateInputField(TmpResources());
        go.name = name;
        go.layer = 5;
        go.transform.SetParent(parent, false);
        Themed(go.GetComponent<Image>(), ThemeKey.Input);
        TMP_InputField f = go.GetComponent<TMP_InputField>();
        f.contentType = type;
        f.pointSize = 42;
        if (f.placeholder is TMP_Text ph) ph.text = placeholder;
        if (f.textComponent != null) f.textComponent.color = Brown;
        go.AddComponent<LayoutElement>().preferredHeight = 120;
        return f;
    }

    private static DefaultControls.Resources UguiResources()
    {
        return new DefaultControls.Resources
        {
            standard = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd"),
            background = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd"),
            knob = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd"),
            checkmark = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Checkmark.psd")
        };
    }

    private static Slider NewSlider(Transform parent, string name)
    {
        GameObject go = DefaultControls.CreateSlider(UguiResources());
        go.name = name;
        go.transform.SetParent(parent, false);
        Height(go.GetComponent<RectTransform>(), 70);
        Slider s = go.GetComponent<Slider>();
        s.minValue = 0f;
        s.maxValue = 1f;
        s.value = 0.7f;
        Transform handle = go.transform.Find("Handle Slide Area/Handle");
        if (handle != null)
        {
            ((RectTransform)handle).sizeDelta = new Vector2(64, 0);
            SetImage(handle.GetComponent<Image>(), Spr("UI/barra_perilla.png"), Pink);
            handle.GetComponent<Image>().preserveAspect = true;
        }
        Transform fill = go.transform.Find("Fill Area/Fill");
        if (fill != null) SetImage(fill.GetComponent<Image>(), Spr("UI/barra_relleno.png"), Pink);
        Transform back = go.transform.Find("Background");
        if (back != null) SetImage(back.GetComponent<Image>(), Spr("UI/barra_fondo.png"), Cream);
        return s;
    }

    private static Toggle NewToggle(Transform parent, string name, string label)
    {
        RectTransform row = NewUI(name + "Fila", parent);
        Height(row, 100);
        HorizontalLayoutGroup h = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        h.spacing = 24;
        h.childAlignment = TextAnchor.MiddleLeft;
        h.childControlWidth = h.childControlHeight = false;
        h.childForceExpandWidth = h.childForceExpandHeight = false;

        GameObject go = DefaultControls.CreateToggle(UguiResources());
        go.name = name;
        go.transform.SetParent(row, false);
        ((RectTransform)go.transform).sizeDelta = new Vector2(80, 80);
        Transform oldLabel = go.transform.Find("Label");
        if (oldLabel != null) Object.DestroyImmediate(oldLabel.gameObject);
        Transform background = go.transform.Find("Background");
        if (background != null)
        {
            RectTransform b = (RectTransform)background;
            b.anchorMin = Vector2.zero;
            b.anchorMax = Vector2.one;
            b.offsetMin = b.offsetMax = Vector2.zero;
            Transform check = background.Find("Checkmark");
            if (check != null)
            {
                RectTransform c = (RectTransform)check;
                c.anchorMin = Vector2.zero;
                c.anchorMax = Vector2.one;
                c.offsetMin = new Vector2(8, 8);
                c.offsetMax = new Vector2(-8, -8);
            }
        }
        TextMeshProUGUI t = Label(row, "Texto", label, 40, TextAlignmentOptions.MidlineLeft, Brown);
        t.rectTransform.sizeDelta = new Vector2(640, 90);
        Toggle toggle = go.GetComponent<Toggle>();
        toggle.isOn = false;
        return toggle;
    }

    private static RectTransform Card(Transform parent, string name, Vector2 anchor, Vector2 pos, Vector2 size)
    {
        Image img = Themed(NewImage(parent, name, Spr("UI/panel.png"), Cream), ThemeKey.Panel);
        Place(img.rectTransform, anchor, new Vector2(0.5f, anchor.y >= 1f ? 1f : 0.5f), pos, size);
        return img.rectTransform;
    }

    /// <summary>Ventana encima de todo: fondo oscuro que cubre TODA la pantalla (aunque haya notch) y adentro sus paneles.</summary>
    private static RectTransform Overlay(Transform parent, string name)
    {
        RectTransform root = NewUI(name, parent);
        Stretch(root);
        FullScreenBack(root, Dim);
        root.gameObject.SetActive(false);
        return root;
    }

    /// <summary>Fondo que tapa toda la pantalla (sale del SafeArea) sin cambiar el tamaño de los paneles.</summary>
    internal static Image FullScreenBack(RectTransform parent, Color color)
    {
        Image back = NewImage(parent, "FondoPantalla", null, color);
        back.raycastTarget = true;   // bloquea los toques de lo que está detrás
        back.transform.SetAsFirstSibling();
        back.gameObject.AddComponent<CubrirPantalla>();
        return back;
    }

    private static RectTransform ButtonRow(Transform parent, float height)
    {
        RectTransform row = NewUI("Botones", parent);
        Height(row, height);
        HorizontalLayoutGroup h = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        h.spacing = 24;
        h.childControlWidth = h.childControlHeight = true;
        h.childForceExpandWidth = h.childForceExpandHeight = true;
        return row;
    }

    private static void Vertical(GameObject go, float spacing, RectOffset padding)
    {
        VerticalLayoutGroup v = go.AddComponent<VerticalLayoutGroup>();
        v.spacing = spacing;
        v.padding = padding;
        v.childAlignment = TextAnchor.UpperCenter;
        v.childControlWidth = v.childControlHeight = true;
        v.childForceExpandWidth = true;
        v.childForceExpandHeight = false;
    }

    private static void Height(Component c, float h)
    {
        LayoutElement le = c.GetComponent<LayoutElement>();
        if (le == null) le = c.gameObject.AddComponent<LayoutElement>();
        le.preferredHeight = h;
        le.minHeight = h;
    }

    private static void Stretch(RectTransform rt, float left = 0, float right = 0, float top = 0, float bottom = 0)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = new Vector2(left, bottom);
        rt.offsetMax = new Vector2(-right, -top);
    }

    private static void Place(RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
    {
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = pivot;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
    }

    // =====================================================================
    //  AYUDANTES: ARCHIVOS
    // =====================================================================

    private static void Save(GameObject go, string pathWithoutExtension)
    {
        PrefabUtility.SaveAsPrefabAsset(go, pathWithoutExtension + ".prefab");
        Object.DestroyImmediate(go);
    }

    private static T Load<T>(string path) where T : Object => AssetDatabase.LoadAssetAtPath<T>(path);

    private static T LoadC<T>(string prefabPath) where T : Component
    {
        GameObject go = Load<GameObject>(prefabPath);
        return go != null ? go.GetComponent<T>() : null;
    }

    private static Sprite Spr(string relativePath)
    {
        Sprite s = Load<Sprite>(SP + relativePath);
        if (s == null) s = Load<Sprite>(SP + Path.ChangeExtension(relativePath, ".jpg"));
        return s;
    }

    private static AudioClip Clip(string name)
    {
        foreach (string ext in new[] { ".wav", ".mp3", ".ogg" })
        {
            AudioClip c = Load<AudioClip>(SFX + name + ext);
            if (c != null) return c;
        }
        return null;
    }

    private static void EnsureFolder(string parent, string name)
    {
        if (!AssetDatabase.IsValidFolder(parent + "/" + name)) AssetDatabase.CreateFolder(parent, name);
    }

}
