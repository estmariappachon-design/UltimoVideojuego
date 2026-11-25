using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>Una tecla del teclado en pantalla.</summary>
public class TeclaPantalla : MonoBehaviour
{
    public enum Accion { Letra, Borrar, Espacio, Mayus, Simbolos, Siguiente, PuntoCom }
    public Accion accion = Accion.Letra;
    [Tooltip("Lo que escribe en modo letras")] public string normal = "a";
    [Tooltip("Lo que escribe en modo símbolos (vacío = igual que normal)")] public string simbolo = "";
    public TMP_Text etiqueta;
    public Button boton;
}

/// <summary>
/// TECLADO EN PANTALLA del login (estilo Sugaru): así no estorba el teclado del celular.
/// Escribe en el campo que hayas tocado (nombre, correo o contraseña). Tiene números, ñ, @, Mayús, símbolos,
/// ".com" y "Siguiente" para pasar al otro campo.
/// Está en LoginScene → Canvas → SafeArea → TecladoPantalla (muévelo y cámbiale las imágenes como quieras).
/// </summary>
public class TecladoPantalla : MonoBehaviour
{
    [Header("Campos donde escribe (en orden)")]
    public TMP_InputField[] campos;

    [Header("Teclas")]
    [Tooltip("Donde se crean las teclas (si está vacío, se crean al jugar)")]
    public RectTransform teclasRoot;
    public Sprite teclaSprite;
    public Sprite teclaEspecialSprite;
    public TMP_FontAsset fuente;
    public Color colorTexto = new Color(0.85f, 0.34f, 0.52f);
    public Color colorTextoEspecial = Color.white;
    public float tamanoLetra = 40f;
    public float espacio = 10f;

    [Header("Comportamiento")]
    [Tooltip("No abrir el teclado del celular al tocar un campo")]
    public bool ocultarTecladoDelCelular = true;
    [Tooltip("Color del campo donde se está escribiendo")]
    public Color colorCampoActivo = new Color(1f, 0.88f, 0.93f);

    private TMP_InputField activo;
    private bool mayus;
    private bool simbolos;
    private readonly List<TeclaPantalla> teclas = new List<TeclaPantalla>();
    private readonly Dictionary<TMP_InputField, Color> coloresOriginales = new Dictionary<TMP_InputField, Color>();

    // Filas: letras | símbolos
    private static readonly string[][] Letras =
    {
        new[] { "1", "2", "3", "4", "5", "6", "7", "8", "9", "0" },
        new[] { "q", "w", "e", "r", "t", "y", "u", "i", "o", "p" },
        new[] { "a", "s", "d", "f", "g", "h", "j", "k", "l", "ñ" },
        new[] { "z", "x", "c", "v", "b", "n", "m" },
    };
    private static readonly string[][] Simbolos =
    {
        new[] { "1", "2", "3", "4", "5", "6", "7", "8", "9", "0" },
        new[] { "!", "#", "$", "%", "&", "*", "(", ")", "-", "_" },
        new[] { "@", ".", ",", ";", ":", "'", "\"", "?", "/", "+" },
        new[] { "=", "<", ">", "[", "]", "{", "}" },
    };

    private void Start()
    {
        if (teclasRoot == null) teclasRoot = (RectTransform)transform;
        teclas.Clear();
        teclas.AddRange(teclasRoot.GetComponentsInChildren<TeclaPantalla>(true));
        if (teclas.Count == 0) { Construir(); teclas.AddRange(teclasRoot.GetComponentsInChildren<TeclaPantalla>(true)); }
        foreach (TeclaPantalla t in teclas)
        {
            if (t.boton == null) t.boton = t.GetComponent<Button>();
            TeclaPantalla tt = t;
            if (t.boton != null) t.boton.onClick.AddListener(() => Presionar(tt));
        }

        if (campos != null)
        {
            foreach (TMP_InputField f in campos)
            {
                if (f == null) continue;
                if (ocultarTecladoDelCelular) OcultarTecladoNativo(f);
                if (f.image != null) coloresOriginales[f] = f.image.color;
                TMP_InputField ff = f;
                f.onSelect.AddListener(_ => Activar(ff));
            }
            foreach (TMP_InputField f in campos) if (f != null && f.gameObject.activeInHierarchy) { Activar(f); break; }
        }
        ActualizarEtiquetas();
    }

    /// <summary>
    /// Evita que se abra el teclado del celular. "shouldHideSoftKeyboard" no existe en todas las versiones
    /// de TextMeshPro, por eso se busca por nombre (así nunca da error al compilar).
    /// </summary>
    private static void OcultarTecladoNativo(TMP_InputField f)
    {
        f.shouldHideMobileInput = true;
        var prop = typeof(TMP_InputField).GetProperty("shouldHideSoftKeyboard");
        if (prop != null && prop.CanWrite) prop.SetValue(f, true);
    }

    private void Activar(TMP_InputField f)
    {
        activo = f;
        foreach (var kv in coloresOriginales)
            if (kv.Key != null && kv.Key.image != null) kv.Key.image.color = kv.Key == f ? colorCampoActivo : kv.Value;
    }

    private void Presionar(TeclaPantalla t)
    {
        AudioManager.Play(AudioManager.Sfx.Click);
        switch (t.accion)
        {
            case TeclaPantalla.Accion.Letra:
                string s = simbolos && !string.IsNullOrEmpty(t.simbolo) ? t.simbolo : t.normal;
                if (mayus && !simbolos) { s = s.ToUpper(); mayus = false; ActualizarEtiquetas(); }
                Escribir(s);
                break;
            case TeclaPantalla.Accion.Espacio: Escribir(" "); break;
            case TeclaPantalla.Accion.PuntoCom: Escribir(".com"); break;
            case TeclaPantalla.Accion.Borrar:
                if (activo != null && activo.text.Length > 0) Poner(activo.text.Substring(0, activo.text.Length - 1));
                break;
            case TeclaPantalla.Accion.Mayus: mayus = !mayus; ActualizarEtiquetas(); break;
            case TeclaPantalla.Accion.Simbolos: simbolos = !simbolos; mayus = false; ActualizarEtiquetas(); break;
            case TeclaPantalla.Accion.Siguiente: Siguiente(); break;
        }
    }

    private void Escribir(string s)
    {
        if (activo == null) return;
        string nuevo = activo.text + s;
        if (activo.characterLimit > 0 && nuevo.Length > activo.characterLimit) return;
        Poner(nuevo);
    }

    private void Poner(string texto)
    {
        activo.text = texto;
        activo.caretPosition = texto.Length;
        activo.stringPosition = texto.Length;
        activo.ForceLabelUpdate();
    }

    private void Siguiente()
    {
        if (campos == null || campos.Length == 0) return;
        int i = System.Array.IndexOf(campos, activo);
        for (int k = 1; k <= campos.Length; k++)
        {
            TMP_InputField f = campos[(i + k + campos.Length) % campos.Length];
            if (f != null && f.gameObject.activeInHierarchy) { Activar(f); return; }
        }
    }

    private void ActualizarEtiquetas()
    {
        foreach (TeclaPantalla t in teclas)
        {
            if (t.etiqueta == null) continue;
            switch (t.accion)
            {
                case TeclaPantalla.Accion.Letra:
                    string s = simbolos && !string.IsNullOrEmpty(t.simbolo) ? t.simbolo : t.normal;
                    t.etiqueta.text = mayus && !simbolos ? s.ToUpper() : s;
                    break;
                case TeclaPantalla.Accion.Simbolos: t.etiqueta.text = simbolos ? "ABC" : "?123"; break;
                case TeclaPantalla.Accion.Mayus:
                    t.etiqueta.text = "Mayús";
                    t.etiqueta.fontStyle = mayus ? FontStyles.Bold | FontStyles.Underline : FontStyles.Bold;
                    break;
            }
        }
    }

    // ================================================================== CONSTRUIR LAS TECLAS
    /// <summary>Crea las teclas dentro de teclasRoot (lo usa el constructor del editor; al jugar se hace solo si faltan).</summary>
    public void Construir()
    {
        if (teclasRoot == null) teclasRoot = (RectTransform)transform;
        for (int i = teclasRoot.childCount - 1; i >= 0; i--)
        {
            if (Application.isPlaying) Destroy(teclasRoot.GetChild(i).gameObject);
            else DestroyImmediate(teclasRoot.GetChild(i).gameObject);
        }
        VerticalLayoutGroup v = teclasRoot.GetComponent<VerticalLayoutGroup>();
        if (v == null) v = teclasRoot.gameObject.AddComponent<VerticalLayoutGroup>();
        v.spacing = espacio;
        v.childControlWidth = v.childControlHeight = true;
        v.childForceExpandWidth = v.childForceExpandHeight = true;

        for (int r = 0; r < Letras.Length; r++)
        {
            RectTransform row = Fila("Fila" + (r + 1));
            if (r == 3) Tecla(row, TeclaPantalla.Accion.Mayus, "Mayús", "", 1.6f, true);
            for (int k = 0; k < Letras[r].Length; k++)
                Tecla(row, TeclaPantalla.Accion.Letra, Letras[r][k], Simbolos[r][k], 1f, false);
            if (r == 3) Tecla(row, TeclaPantalla.Accion.Borrar, "Borrar", "", 1.6f, true);
        }
        RectTransform last = Fila("Fila5");
        Tecla(last, TeclaPantalla.Accion.Simbolos, "?123", "", 1.5f, true);
        Tecla(last, TeclaPantalla.Accion.Letra, "@", "@", 1f, false);
        Tecla(last, TeclaPantalla.Accion.Espacio, "espacio", "", 3.2f, false);
        Tecla(last, TeclaPantalla.Accion.Letra, ".", ".", 1f, false);
        Tecla(last, TeclaPantalla.Accion.PuntoCom, ".com", "", 1.4f, false);
        Tecla(last, TeclaPantalla.Accion.Siguiente, "Siguiente", "", 2f, true);
    }

    private RectTransform Fila(string name)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.layer = 5;
        go.transform.SetParent(teclasRoot, false);
        HorizontalLayoutGroup h = go.AddComponent<HorizontalLayoutGroup>();
        h.spacing = espacio;
        h.childControlWidth = h.childControlHeight = true;
        h.childForceExpandWidth = false;
        h.childForceExpandHeight = true;
        return (RectTransform)go.transform;
    }

    private void Tecla(RectTransform row, TeclaPantalla.Accion accion, string normal, string simbolo, float ancho, bool especial)
    {
        GameObject go = new GameObject("Tecla_" + normal, typeof(RectTransform), typeof(Image), typeof(Button));
        go.layer = 5;
        go.transform.SetParent(row, false);
        Image img = go.GetComponent<Image>();
        Sprite sp = especial && teclaEspecialSprite != null ? teclaEspecialSprite : teclaSprite;
        img.sprite = sp;
        img.type = sp != null && sp.border != Vector4.zero ? Image.Type.Sliced : Image.Type.Simple;
        img.pixelsPerUnitMultiplier = 2.4f;   // bordes redondeados más chicos (las teclas son pequeñas)
        if (sp == null) img.color = especial ? new Color(0.97f, 0.47f, 0.63f) : new Color(1f, 0.97f, 0.95f);
        Button b = go.GetComponent<Button>();
        b.targetGraphic = img;
        LayoutElement le = go.AddComponent<LayoutElement>();
        le.flexibleWidth = ancho;
        le.minWidth = 10;

        GameObject lt = new GameObject("Texto", typeof(RectTransform), typeof(TextMeshProUGUI));
        lt.layer = 5;
        lt.transform.SetParent(go.transform, false);
        RectTransform lrt = (RectTransform)lt.transform;
        lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
        lrt.offsetMin = new Vector2(4, 6); lrt.offsetMax = new Vector2(-4, -2);
        TextMeshProUGUI t = lt.GetComponent<TextMeshProUGUI>();
        if (fuente != null) t.font = fuente;
        t.text = normal;
        t.fontSize = tamanoLetra;
        t.enableAutoSizing = true;
        t.fontSizeMin = 14;
        t.fontSizeMax = tamanoLetra;
        t.fontStyle = FontStyles.Bold;
        t.alignment = TextAlignmentOptions.Center;
        t.color = especial ? colorTextoEspecial : colorTexto;
        t.raycastTarget = false;

        TeclaPantalla tecla = go.AddComponent<TeclaPantalla>();
        tecla.accion = accion;
        tecla.normal = normal;
        tecla.simbolo = simbolo;
        tecla.etiqueta = t;
        tecla.boton = b;
    }
}
