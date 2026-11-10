using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Hace que TUS imágenes se vean al jugar, las pongas donde las pongas:
///  • Si cambias la imagen de un objeto en la escena o en un prefab, se guarda sola en el lugar
///    de donde la lee el juego (TemaVisual o LevelCatalog).
///  • Menú "Torre de Pasteles → 6. Recuperar mis imágenes": revisa las escenas y prefabs, encuentra
///    las imágenes que pusiste a mano y las guarda en el TemaVisual / LevelCatalog (sin rehacer las escenas).
/// </summary>
[InitializeOnLoad]
public static class ImageSync
{
    private const string ThemePath = "Assets/Resources/TemaVisual.asset";
    private const string CatalogPath = "Assets/Resources/LevelCatalog.asset";
    private static readonly HashSet<Component> pending = new HashSet<Component>();

    static ImageSync()
    {
        Undo.postprocessModifications += OnModifications;
    }

    // =====================================================================
    //  1) Cambios hechos a mano en el Inspector
    // =====================================================================

    private static UndoPropertyModification[] OnModifications(UndoPropertyModification[] mods)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return mods;
        foreach (UndoPropertyModification m in mods)
        {
            PropertyModification pm = m.currentValue;
            if (pm == null || pm.propertyPath != "m_Sprite") continue;
            if (pm.target is Image || pm.target is SpriteRenderer)
            {
                if (pending.Count == 0) EditorApplication.delayCall += ProcessPending;
                pending.Add((Component)pm.target);
            }
        }
        return mods;
    }

    private static void ProcessPending()
    {
        Component[] list = pending.Where(c => c != null).ToArray();
        pending.Clear();
        foreach (Component c in list) OnSpriteChanged(c);
    }

    private static void OnSpriteChanged(Component c)
    {
        Sprite s = c is Image img ? img.sprite : ((SpriteRenderer)c).sprite;
        if (s == null) return;

        // a) Objetos del TemaVisual
        ThemedImage ti = c.GetComponent<ThemedImage>();
        if (ti != null)
        {
            if (ti.soloEsteObjeto) return;
            UITheme theme = AssetDatabase.LoadAssetAtPath<UITheme>(ThemePath);
            if (theme == null || UITheme.Get(ti.key) == s) return;
            Undo.RecordObject(theme, "Imagen del tema");
            theme.Set(ti.key, s);
            EditorUtility.SetDirty(theme);
            AssetDatabase.SaveAssetIfDirty(theme);
            ti.MarkApplied();
            ThemedImage.RefreshAll();
            Debug.Log($"Torre de Pasteles: guardé '{s.name}' en TemaVisual → {Nice(ti.key)}. " +
                      "Todos los objetos con esa casilla la usan ahora (si querías cambiar SOLO este, marca 'Solo Este Objeto').", c);
            return;
        }

        // b) Objetos cuya imagen sale del LevelCatalog
        CatalogImageNote note = c.GetComponent<CatalogImageNote>();
        if (note != null) SaveToCatalog(note.source, s, c, true);
    }

    // =====================================================================
    //  Guardar en el LevelCatalog
    // =====================================================================

    /// <returns>Texto con lo que se hizo (vacío si nada)</returns>
    private static string SaveToCatalog(CatalogImageNote.Source source, Sprite s, Object context, bool interactive)
    {
        LevelCatalog cat = AssetDatabase.LoadAssetAtPath<LevelCatalog>(CatalogPath);
        if (cat == null) return "";
        Undo.RecordObject(cat, "Imagen del catálogo");
        string done = "";

        switch (source)
        {
            case CatalogImageNote.Source.FondoDeNivel:
            {
                int test = 1;
                StackGame g = Object.FindFirstObjectByType<StackGame>();
                if (g != null) test = g.testLevel;
                int choice = EditorUtility.DisplayDialogComplex("Fondo del nivel",
                    $"El fondo del juego cambia en cada nivel (LevelCatalog → Levels → Background).\n\n¿Dónde quieres usar '{s.name}'?",
                    "En los 10 niveles", "No guardar", $"Solo en el nivel {test}");
                if (choice == 1) return "";
                for (int i = 0; i < cat.levels.Length; i++)
                    if (choice == 0 || i == test - 1) cat.levels[i].background = s;
                done = choice == 0 ? $"Fondo '{s.name}' → los 10 niveles" : $"Fondo '{s.name}' → nivel {test}";
                break;
            }
            case CatalogImageNote.Source.Cliente:
            {
                if (cat.customers.Any(cu => cu != null && (cu.normal == s || cu.happy == s || cu.angry == s))) return "";
                string first = cat.customers.Length > 0 ? cat.customers[0].name : "el primero";
                int choice = EditorUtility.DisplayDialogComplex("Animal",
                    $"Los animales salen del LevelCatalog → Customers (cada uno con cara normal, feliz y enojada).\n\n¿Qué hago con '{s.name}'?",
                    "Agregarlo como animal nuevo", "No guardar", $"Reemplazar a {first}");
                if (choice == 1) return "";
                if (choice == 0)
                {
                    var list = cat.customers.ToList();
                    list.Add(new LevelCatalog.Customer { name = Pretty(s.name), normal = s, happy = s, angry = s, greeting = "¡Hola! ¿Me preparas este pastel?" });
                    cat.customers = list.ToArray();
                    done = $"Animal nuevo '{Pretty(s.name)}' (pon también sus caras feliz y enojada en LevelCatalog → Customers)";
                }
                else
                {
                    cat.customers[0].normal = s;
                    done = $"'{s.name}' → cara normal de {first} (pon también la feliz y la enojada)";
                }
                break;
            }
            case CatalogImageNote.Source.SaborDePastel:
            {
                if (cat.flavors.Any(f => f != null && f.layerSprite == s)) return "";
                FixLayerBorders(s);
                string first = cat.flavors.Length > 0 ? cat.flavors[0].name : "el primero";
                int choice = EditorUtility.DisplayDialogComplex("Sabor de pastel",
                    $"Los pasteles salen del LevelCatalog → Flavors (un sabor por imagen).\n\n¿Qué hago con '{s.name}'?",
                    "Agregarlo como sabor nuevo", "No guardar", $"Reemplazar {first}");
                if (choice == 1) return "";
                if (choice == 0)
                {
                    var list = cat.flavors.ToList();
                    list.Add(new LevelCatalog.Flavor { name = Pretty(s.name), layerSprite = s, color = Color.white });
                    cat.flavors = list.ToArray();
                    done = $"Sabor nuevo '{Pretty(s.name)}' (sube 'Flavor Count' en los niveles para que salga más seguido)";
                }
                else
                {
                    cat.flavors[0].layerSprite = s;
                    done = $"'{s.name}' → sabor {first}";
                }
                break;
            }
            default:
                if (interactive)
                    EditorUtility.DisplayDialog("Miniatura del nivel", "Las miniaturas se cambian en LevelCatalog → Levels → Thumbnail.", "OK");
                Selection.activeObject = cat;
                return "";
        }

        EditorUtility.SetDirty(cat);
        AssetDatabase.SaveAssetIfDirty(cat);
        Debug.Log("Torre de Pasteles: " + done, context);
        return done;
    }

    // =====================================================================
    //  2) Menú: recuperar las imágenes que ya pusiste
    // =====================================================================

    [MenuItem("Torre de Pasteles/6. Recuperar mis imágenes (escenas y prefabs)", priority = 6)]
    public static void RecoverMenu()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        UITheme theme = AssetDatabase.LoadAssetAtPath<UITheme>(ThemePath);
        LevelCatalog cat = AssetDatabase.LoadAssetAtPath<LevelCatalog>(CatalogPath);
        if (theme == null || cat == null)
        {
            EditorUtility.DisplayDialog("Torre de Pasteles", "Primero usa 'CREAR TODO' (falta TemaVisual o LevelCatalog).", "OK");
            return;
        }

        StringBuilder report = new StringBuilder();
        int converted = ConvertTexturesToSprites();
        if (converted > 0) report.AppendLine($"• {converted} imágenes convertidas a Sprite.");

        var themeFound = new Dictionary<ThemeKey, Sprite>();
        var catalogFound = new List<(CatalogImageNote.Source, Sprite)>();

        // ---------- Prefabs ----------
        foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            bool changed = Scan(root, theme, cat, themeFound, catalogFound);
            if (changed) PrefabUtility.SaveAsPrefabAsset(root, path);
            PrefabUtility.UnloadPrefabContents(root);
        }

        // ---------- Escenas ----------
        string current = EditorSceneManager.GetActiveScene().path;
        foreach (EditorBuildSettingsScene bs in EditorBuildSettings.scenes)
        {
            if (!File.Exists(bs.path)) continue;
            var scene = EditorSceneManager.OpenScene(bs.path, OpenSceneMode.Single);
            bool changed = false;
            foreach (GameObject go in scene.GetRootGameObjects())
                changed |= Scan(go, theme, cat, themeFound, catalogFound);
            if (changed) EditorSceneManager.SaveScene(scene);
        }

        // ---------- Guardar en el TemaVisual ----------
        foreach (var kv in themeFound)
        {
            theme.Set(kv.Key, kv.Value);
            report.AppendLine($"• TemaVisual → {Nice(kv.Key)} = {kv.Value.name}");
        }
        // Casillas que copian a otra: si siguen con la imagen de ejemplo, se vacían para que usen la tuya
        if (UITheme.IsBuiltInSprite(theme.fondoLogin) && !UITheme.IsBuiltInSprite(theme.fondoMenu)) theme.fondoLogin = null;
        if (UITheme.IsBuiltInSprite(theme.iconoCorazon) && !UITheme.IsBuiltInSprite(theme.corazonLleno)) theme.iconoCorazon = null;
        EditorUtility.SetDirty(theme);

        // ---------- Guardar en el LevelCatalog ----------
        foreach (var (source, sprite) in catalogFound.Distinct())
        {
            string done = SaveToCatalog(source, sprite, null, false);
            if (!string.IsNullOrEmpty(done)) report.AppendLine("• " + done);
        }
        AssetDatabase.SaveAssets();

        // Volver a abrir las escenas para que los objetos tomen las imágenes del tema
        foreach (EditorBuildSettingsScene bs in EditorBuildSettings.scenes)
        {
            if (!File.Exists(bs.path)) continue;
            var scene = EditorSceneManager.OpenScene(bs.path, OpenSceneMode.Single);
            ThemedImage.RefreshAll();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
        if (!string.IsNullOrEmpty(current) && File.Exists(current)) EditorSceneManager.OpenScene(current, OpenSceneMode.Single);

        string msg = report.Length > 0
            ? "Listo, guardé tus imágenes donde el juego las lee:\n\n" + report + "\nDale Play para verlas."
            : "No encontré imágenes tuyas puestas a mano en las escenas o prefabs.\n\n" +
              "Revisa que hayas guardado la escena (Ctrl+S) y que tus imágenes NO se llamen igual que las del juego " +
              "(boton, panel, logo...). También puedes ponerlas directo en Assets/Resources/TemaVisual o LevelCatalog.";
        Debug.Log("Torre de Pasteles – Recuperar imágenes:\n" + report);
        EditorUtility.DisplayDialog("Recuperar mis imágenes", msg, "OK");
    }

    /// <summary>Busca imágenes puestas a mano debajo de "root". Devuelve true si cambió algo en el objeto.</summary>
    private static bool Scan(GameObject root, UITheme theme, LevelCatalog cat,
                             Dictionary<ThemeKey, Sprite> themeFound, List<(CatalogImageNote.Source, Sprite)> catalogFound)
    {
        bool changed = false;

        // Corazones del HUD y nodos del mapa (versiones viejas no tenían ThemedImage)
        foreach (GameHud hud in root.GetComponentsInChildren<GameHud>(true))
        {
            if (hud.hearts == null) continue;
            foreach (Image h in hud.hearts)
            {
                if (h == null || h.GetComponent<ThemedImage>() != null) continue;
                h.gameObject.AddComponent<ThemedImage>().key = ThemeKey.CorazonLleno;
                changed = true;
            }
        }
        foreach (LevelNodeUI node in root.GetComponentsInChildren<LevelNodeUI>(true))
        {
            if (node.baseImage == null || node.baseImage.GetComponent<ThemedImage>() != null) continue;
            node.baseImage.gameObject.AddComponent<ThemedImage>().key = ThemeKey.NodoNivel;
            changed = true;
        }

        // Notas de catálogo en fondo, cliente y capas (por si la escena es de una versión anterior)
        foreach (StackGame g in root.GetComponentsInChildren<StackGame>(true))
        {
            if (g.backgroundRenderer != null) changed |= EnsureNote(g.backgroundRenderer.gameObject, CatalogImageNote.Source.FondoDeNivel);
            if (g.customer != null && g.customer.sprite != null) changed |= EnsureNote(g.customer.sprite.gameObject, CatalogImageNote.Source.Cliente);
        }
        foreach (CakeLayer l in root.GetComponentsInChildren<CakeLayer>(true))
            if (l.body != null) changed |= EnsureNote(l.body.gameObject, CatalogImageNote.Source.SaborDePastel);

        // Imágenes del tema puestas a mano
        foreach (ThemedImage ti in root.GetComponentsInChildren<ThemedImage>(true))
        {
            if (ti.soloEsteObjeto || !ti.HasOwnSprite) continue;
            Sprite s = ti.CurrentSprite;
            if (themeFound.TryGetValue(ti.key, out Sprite other) && other != s)
            {
                // dos imágenes distintas para la misma casilla: esta se queda solo para este objeto
                ti.soloEsteObjeto = true;
                changed = true;
                continue;
            }
            themeFound[ti.key] = s;
        }

        // Imágenes del catálogo puestas a mano
        foreach (CatalogImageNote n in root.GetComponentsInChildren<CatalogImageNote>(true))
        {
            Sprite s = n.GetComponent<SpriteRenderer>() != null ? n.GetComponent<SpriteRenderer>().sprite
                     : n.GetComponent<Image>() != null ? n.GetComponent<Image>().sprite : null;
            if (s == null || IsCatalogBuiltIn(n.source, s, cat)) continue;
            catalogFound.Add((n.source, s));
        }
        return changed;
    }

    private static bool EnsureNote(GameObject go, CatalogImageNote.Source source)
    {
        if (go.GetComponent<CatalogImageNote>() != null) return false;
        go.AddComponent<CatalogImageNote>().source = source;
        return true;
    }

    private static bool IsCatalogBuiltIn(CatalogImageNote.Source source, Sprite s, LevelCatalog cat)
    {
        string n = s.name.ToLowerInvariant();
        switch (source)
        {
            case CatalogImageNote.Source.FondoDeNivel:
                return n.StartsWith("fondo_nivel_") || cat.levels.Any(l => l != null && l.background == s);
            case CatalogImageNote.Source.Cliente:
                return cat.customers.Any(c => c != null && (c.normal == s || c.happy == s || c.angry == s))
                       || n.EndsWith("_normal") && new[] { "conejita", "gatito", "osito", "pollito", "pinguina" }.Any(a => n.StartsWith(a));
            case CatalogImageNote.Source.SaborDePastel:
                return cat.flavors.Any(f => f != null && f.layerSprite == s) || n == "capa_quemada";
            default:
                return true;
        }
    }

    /// <summary>Convierte a Sprite las imágenes (png/jpg) del proyecto que todavía no lo son.</summary>
    private static int ConvertTexturesToSprites()
    {
        string[] skip = { "/Firebase", "/Plugins", "/TextMesh Pro", "/ExternalDependencyManager", "/Editor", "/Fuentes", "/StreamingAssets" };
        int n = 0;
        foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string ext = Path.GetExtension(path).ToLowerInvariant();
            if (ext != ".png" && ext != ".jpg" && ext != ".jpeg") continue;
            if (skip.Any(s => path.Contains(s))) continue;
            TextureImporter imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp == null || imp.textureType == TextureImporterType.Sprite) continue;
            SpriteImageImporter.Apply(imp, path);
            imp.SaveAndReimport();
            n++;
        }
        return n;
    }

    /// <summary>Las capas de pastel necesitan bordes a los lados para estirarse bien.</summary>
    private static void FixLayerBorders(Sprite s)
    {
        if (s == null || s.border != Vector4.zero) return;
        string path = AssetDatabase.GetAssetPath(s);
        TextureImporter imp = AssetImporter.GetAtPath(path) as TextureImporter;
        if (imp == null) return;
        float side = Mathf.Min(70f, s.rect.width * 0.14f);
        imp.spriteBorder = new Vector4(side, 0, side, 0);
        TextureImporterSettings st = new TextureImporterSettings();
        imp.ReadTextureSettings(st);
        st.spriteMeshType = SpriteMeshType.FullRect;
        imp.SetTextureSettings(st);
        imp.SaveAndReimport();
    }

    private static string Nice(ThemeKey k) => ObjectNames.NicifyVariableName(k.ToString());

    private static string Pretty(string file)
    {
        string n = file.Replace("capa_", "").Replace('_', ' ').Trim();
        return n.Length > 0 ? char.ToUpper(n[0]) + n.Substring(1) : file;
    }
}

// =====================================================================
//  Avisos en el Inspector: de dónde sale cada imagen
// =====================================================================

[CustomEditor(typeof(ThemedImage)), CanEditMultipleObjects]
public class ThemedImageEditor : Editor
{
    public override void OnInspectorGUI()
    {
        ThemedImage t = (ThemedImage)target;
        EditorGUILayout.HelpBox(
            t.soloEsteObjeto
                ? "Este objeto usa SU PROPIA imagen: cámbiala arriba en 'Source Image' / 'Sprite'."
                : $"Esta imagen viene del TemaVisual → '{ObjectNames.NicifyVariableName(t.key.ToString())}'.\n" +
                  "Puedes arrastrar tu imagen arriba (Source Image / Sprite): se guarda sola en el TemaVisual y " +
                  "cambia en todos los objetos iguales. Marca 'Solo Este Objeto' si quieres cambiar únicamente este.",
            MessageType.Info);
        DrawDefaultInspector();
        if (GUILayout.Button("Abrir TemaVisual"))
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<UITheme>("Assets/Resources/TemaVisual.asset");
    }
}

[CustomEditor(typeof(CatalogImageNote))]
public class CatalogImageNoteEditor : Editor
{
    public override void OnInspectorGUI()
    {
        CatalogImageNote n = (CatalogImageNote)target;
        string where = n.source == CatalogImageNote.Source.FondoDeNivel ? "LevelCatalog → Levels → Background (uno por nivel)"
                     : n.source == CatalogImageNote.Source.Cliente ? "LevelCatalog → Customers (cara normal, feliz y enojada)"
                     : n.source == CatalogImageNote.Source.SaborDePastel ? "LevelCatalog → Flavors (una imagen por sabor)"
                     : "LevelCatalog → Levels → Thumbnail";
        EditorGUILayout.HelpBox(
            "Al jugar, el juego pone aquí la imagen de " + where + ".\n" +
            "Si arrastras tu imagen a este objeto, te pregunto dónde guardarla para que sí se vea al jugar.",
            MessageType.Info);
        if (GUILayout.Button("Abrir LevelCatalog"))
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<LevelCatalog>("Assets/Resources/LevelCatalog.asset");
    }
}
