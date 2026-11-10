using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Ventana "Torre de Pasteles → Paneles (ver y editar)".
/// Muestra todos los paneles de la escena abierta (mapa, ranking, ajustes, pausa, ¡ganaste!, ¡perdiste!,
/// globo del pedido, avisos, filas de ejemplo...) y te deja verlos uno por uno en la ventana Scene para editarlos.
/// Puedes guardar la escena con los paneles visibles: al darle Play el juego los pone como deben estar.
/// </summary>
public class PanelTools : EditorWindow
{
    private Vector2 scrollPos;

    [MenuItem("Torre de Pasteles/Paneles (ver y editar)", priority = 0)]
    public static void Open()
    {
        PanelTools w = GetWindow<PanelTools>("Paneles");
        w.minSize = new Vector2(330, 420);
        w.Show();
    }

    [MenuItem("Torre de Pasteles/7. Preparar paneles en mis escenas", priority = 7)]
    public static void PrepareMenu()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        string current = EditorSceneManager.GetActiveScene().path;
        int total = 0;
        foreach (EditorBuildSettingsScene bs in EditorBuildSettings.scenes)
        {
            if (!File.Exists(bs.path)) continue;
            Scene scene = EditorSceneManager.OpenScene(bs.path, OpenSceneMode.Single);
            total += SetupOpenScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
        if (!string.IsNullOrEmpty(current) && File.Exists(current)) EditorSceneManager.OpenScene(current, OpenSceneMode.Single);
        EditorUtility.DisplayDialog("Paneles",
            $"Listo: {total} paneles preparados en tus escenas (no se borró nada de lo que cambiaste).\n\n" +
            "Abre 'Torre de Pasteles → Paneles (ver y editar)' para verlos y editarlos.", "OK");
        Open();
    }

    private void OnHierarchyChange() => Repaint();
    private void OnFocus() => Repaint();

    private void OnGUI()
    {
        // ---------------- Escenas ----------------
        EditorGUILayout.LabelField("Escena", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        foreach (string s in new[] { "LoginScene", "MainMenu", "GameScene" })
        {
            string path = "Assets/Scenes/" + s + ".unity";
            bool isOpen = SceneManager.GetActiveScene().path == path;
            GUI.enabled = !isOpen && File.Exists(path) && !Application.isPlaying;
            if (GUILayout.Button(isOpen ? "● " + s : s))
                if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) EditorSceneManager.OpenScene(path);
            GUI.enabled = true;
        }
        EditorGUILayout.EndHorizontal();

        if (Application.isPlaying)
        {
            EditorGUILayout.HelpBox("Detén el Play para editar los paneles.", MessageType.Warning);
            return;
        }

        List<PanelInicio> panels = Object.FindObjectsByType<PanelInicio>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Where(p => p.gameObject.scene == SceneManager.GetActiveScene())
            .OrderBy(p => p.grupo).ThenBy(p => p.transform.GetSiblingIndex()).ToList();

        if (panels.Count == 0)
        {
            EditorGUILayout.HelpBox(SceneManager.GetActiveScene().name == "LoginScene"
                ? "LoginScene tiene una sola pantalla: todo ya está a la vista."
                : "Esta escena todavía no tiene paneles marcados.", MessageType.Info);
            if (GUILayout.Button("Preparar paneles de esta escena"))
            {
                SetupOpenScene();
                EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            }
            return;
        }

        EditorGUILayout.Space(4);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Mostrar TODO")) foreach (PanelInicio p in panels) SetVisible(p, true);
        if (GUILayout.Button("Como al empezar el juego")) foreach (PanelInicio p in panels) SetVisible(p, p.visibleAlJugar);
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.HelpBox("Visible/Oculto = mostrar u ocultar en la escena · 'Ver' = muestra SOLO ese y lo enfoca.\n" +
                                "Puedes guardar con los paneles visibles: al dar Play el juego los acomoda solo.", MessageType.None);

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
        foreach (IGrouping<PanelInicio.Grupo, PanelInicio> g in panels.GroupBy(p => p.grupo))
        {
            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField(GroupTitle(g.Key), EditorStyles.boldLabel);
            foreach (PanelInicio p in g)
            {
                EditorGUILayout.BeginHorizontal();
                bool vis = p.gameObject.activeSelf;
                bool newVis = GUILayout.Toggle(vis, vis ? "Visible" : "Oculto", "Button", GUILayout.Width(64));
                if (newVis != vis) SetVisible(p, newVis);
                GUIStyle label = new GUIStyle(EditorStyles.label) { fontStyle = vis && p.gameObject.activeInHierarchy ? FontStyle.Bold : FontStyle.Normal };
                GUILayout.Label(p.nombre, label);
                if (GUILayout.Button("Ver", GUILayout.Width(44))) ShowOnly(p, panels);
                if (GUILayout.Button("Elegir", GUILayout.Width(52))) { Selection.activeGameObject = p.gameObject; EditorGUIUtility.PingObject(p.gameObject); }
                EditorGUILayout.EndHorizontal();
            }
        }
        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space(4);
        EditorGUILayout.HelpBox("Consejos: en la ventana Scene activa el botón 2D, elige un panel y presiona F para centrarlo. " +
                                "Muévelo con la herramienta Rect (T). El mapa muestra una vista previa de los niveles: " +
                                "mueve los puntos Posicion_Nivel_XX y el camino se redibuja.", MessageType.Info);
    }

    private static string GroupTitle(PanelInicio.Grupo g)
    {
        switch (g)
        {
            case PanelInicio.Grupo.Pantalla: return "Pantallas del menú";
            case PanelInicio.Grupo.Ventana: return "Ventanas";
            case PanelInicio.Grupo.Variante: return "¡Ganaste! / ¡Perdiste!";
            case PanelInicio.Grupo.Aviso: return "Avisos y efectos del juego";
            default: return "Filas y objetos de ejemplo (plantillas)";
        }
    }

    private static void SetVisible(PanelInicio p, bool visible)
    {
        if (p.gameObject.activeSelf == visible) return;
        Undo.RecordObject(p.gameObject, "Mostrar panel");
        p.gameObject.SetActive(visible);
        EditorSceneManager.MarkSceneDirty(p.gameObject.scene);
    }

    /// <summary>Muestra solo este panel: oculta los otros de su grupo y las ventanas que lo taparían, y lo enfoca.</summary>
    private static void ShowOnly(PanelInicio target, List<PanelInicio> all)
    {
        // Paneles que lo contienen (por ejemplo la ventana de resultado que contiene "Victoria")
        HashSet<PanelInicio> chain = new HashSet<PanelInicio>(target.GetComponentsInParent<PanelInicio>(true));

        bool mainTarget = target.grupo == PanelInicio.Grupo.Pantalla || target.grupo == PanelInicio.Grupo.Ventana || target.grupo == PanelInicio.Grupo.Variante;
        foreach (PanelInicio p in all)
        {
            if (chain.Contains(p)) { SetVisible(p, true); continue; }
            PanelInicio parentPanel = p.transform.parent != null ? p.transform.parent.GetComponentInParent<PanelInicio>(true) : null;
            bool hide =
                (mainTarget && p.grupo == target.grupo)                                        // otra pantalla / ventana / variante
                || p.grupo == PanelInicio.Grupo.Ventana                                         // ventanas que lo taparían
                || (target.grupo != PanelInicio.Grupo.Aviso && p.grupo == PanelInicio.Grupo.Aviso)   // avisos que estorban
                || (p.grupo == PanelInicio.Grupo.Variante && parentPanel != null && chain.Contains(parentPanel)); // victoria vs derrota
            if (hide) SetVisible(p, false);
        }

        // Activar también los objetos padre que estén apagados
        for (Transform t = target.transform.parent; t != null; t = t.parent)
            if (!t.gameObject.activeSelf) { Undo.RecordObject(t.gameObject, "Mostrar panel"); t.gameObject.SetActive(true); }

        Selection.activeGameObject = target.gameObject;
        if (SceneView.lastActiveSceneView != null)
        {
            SceneView.lastActiveSceneView.in2DMode = true;
            SceneView.lastActiveSceneView.FrameSelected();
        }
    }

    // =====================================================================
    //  Marcar los paneles de la escena abierta (lo usa el constructor y el menú 7)
    // =====================================================================

    /// <returns>Cantidad de paneles marcados</returns>
    public static int SetupOpenScene()
    {
        int n = 0;
        void Mark(Component c, string name, PanelInicio.Grupo g, bool visible) { if (c != null) Mark2(c.gameObject, name, g, visible); }
        void Mark2(GameObject go, string name, PanelInicio.Grupo g, bool visible)
        {
            if (go == null) return;
            PanelInicio p = go.GetComponent<PanelInicio>();
            if (p == null) p = go.AddComponent<PanelInicio>();
            p.nombre = name;
            p.grupo = g;
            p.visibleAlJugar = visible;
            EditorUtility.SetDirty(p);
            n++;
            Canvas root = go.GetComponentInParent<Canvas>(true);
            if (root != null) root = root.rootCanvas;
            if (root != null && root.GetComponent<PanelesAlEmpezar>() == null) root.gameObject.AddComponent<PanelesAlEmpezar>();
        }

        foreach (MainMenuUI m in Find<MainMenuUI>())
        {
            Mark2(m.homePanel, "Inicio (jugador y botones)", PanelInicio.Grupo.Pantalla, true);
            Mark2(m.levelsPanel, "Mapa de niveles", PanelInicio.Grupo.Pantalla, false);
            Mark2(m.rankingPanel, "Ranking", PanelInicio.Grupo.Pantalla, false);
            Mark2(m.settingsPanel, "Ajustes", PanelInicio.Grupo.Pantalla, false);
        }
        foreach (LevelInfoPopup p in Find<LevelInfoPopup>()) Mark(p, "Ventana del nivel (¡Jugar!)", PanelInicio.Grupo.Ventana, false);

        foreach (GameHud h in Find<GameHud>())
        {
            Mark2(h.pausePanel, "Pausa", PanelInicio.Grupo.Ventana, false);
            Mark2(h.eventBanner, "Aviso de evento (ventilador, temblor...)", PanelInicio.Grupo.Aviso, false);
            Mark2(h.stabilityRoot, "Barra de estabilidad (temblor)", PanelInicio.Grupo.Aviso, false);
            Mark2(h.tapHint, "Ayuda '¡Toca para soltar!'", PanelInicio.Grupo.Aviso, false);
            if (h.messageText != null) Mark2(h.messageText.gameObject, "Mensaje grande", PanelInicio.Grupo.Aviso, false);
            if (h.popupText != null) Mark2(h.popupText.gameObject, "Texto emergente (¡Perfecto!)", PanelInicio.Grupo.Aviso, false);
            if (h.ticket != null && h.ticket.rowTemplate != null) Mark2(h.ticket.rowTemplate, "Fila del ticket", PanelInicio.Grupo.Plantilla, false);
        }
        foreach (OrderIntroUI o in Find<OrderIntroUI>())
        {
            Mark(o, "Globo del pedido (presentación)", PanelInicio.Grupo.Ventana, false);
            Mark2(o.layerTemplate, "Capa del globo del pedido", PanelInicio.Grupo.Plantilla, false);
        }
        foreach (ResultScreen r in Find<ResultScreen>())
        {
            Mark(r, "¡Ganaste! / ¡Perdiste!", PanelInicio.Grupo.Ventana, false);
            Mark2(r.winContent, "Contenido de VICTORIA", PanelInicio.Grupo.Variante, true);
            Mark2(r.loseContent, "Contenido de DERROTA", PanelInicio.Grupo.Variante, false);
            Mark2(r.statRowTemplate, "Fila de estadística", PanelInicio.Grupo.Plantilla, false);
            if (r.customerTemplate != null) Mark2(r.customerTemplate.gameObject, "Cliente feliz", PanelInicio.Grupo.Plantilla, false);
        }
        foreach (ScreenEffects fx in Find<ScreenEffects>())
        {
            if (fx.darkness != null) Mark2(fx.darkness.gameObject, "Apagón (oscuridad)", PanelInicio.Grupo.Aviso, false);
            if (fx.flourRoot != null) Mark2(fx.flourRoot.gameObject, "Nube de harina", PanelInicio.Grupo.Aviso, false);
        }
        return n;
    }

    private static IEnumerable<T> Find<T>() where T : Object
        => Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);
}
