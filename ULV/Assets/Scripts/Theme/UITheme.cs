using UnityEngine;

/// <summary>Cada imagen de la interfaz y del escenario que se puede cambiar desde el TemaVisual.</summary>
public enum ThemeKey
{
    Logo, FondoLogin, FondoMenu, FondoMapa,
    Boton, BotonVerde, BotonCerrar, Panel, Input, Ticket, Cinta, BurbujaDialogo, BurbujaCola, Rayos,
    CorazonLleno, CorazonVacio, Estrella, EstrellaVacia, NuevoRecord,
    Pausa, Candado, Check, Flecha, ManoTocar, ManoSujetar, Viento,
    NodoNivel, NodoBloqueado, NodoCompletado, PuntoCamino, MarcadorJugador,
    Plato, Mostrador, VentiladorBase, VentiladorAspas, Oscuridad, Harina,
    IconoPedido, IconoPerfecto, IconoMosca, IconoQuemado, IconoCorazon, Avatar
}

/// <summary>
/// TEMA VISUAL: aquí van TODAS las imágenes de la interfaz y del escenario.
/// Archivo: Assets/Resources/TemaVisual.asset → selecciónalo y arrastra tus imágenes a cada casilla.
/// Los cambios se ven al instante en las escenas (todo lo que tenga el componente ThemedImage).
///
/// Los animales, los sabores de pastel y los fondos de cada nivel están en Assets/Resources/LevelCatalog.
/// </summary>
[CreateAssetMenu(fileName = "TemaVisual", menuName = "Torre de Pasteles/Tema visual")]
public class UITheme : ScriptableObject
{
    [Header("Logo y fondos de pantallas")]
    public Sprite logo;
    public Sprite fondoLogin;
    public Sprite fondoMenu;
    [Tooltip("Fondo largo del mapa de niveles (ideal 4200 x 1080)")]
    public Sprite fondoMapa;

    [Header("Botones y paneles (deja bordes de 40 px para que se estiren bien)")]
    public Sprite boton;
    public Sprite botonVerde;
    public Sprite botonCerrar;
    public Sprite panel;
    public Sprite input;
    public Sprite ticket;
    [Tooltip("Cinta/listón de títulos (bordes de 130 px a cada lado)")]
    public Sprite cinta;
    public Sprite burbujaDialogo;
    public Sprite burbujaCola;
    [Tooltip("Rayos de luz detrás del panel de victoria")]
    public Sprite rayos;

    [Header("Íconos")]
    public Sprite corazonLleno;
    public Sprite corazonVacio;
    public Sprite estrella;
    public Sprite estrellaVacia;
    public Sprite nuevoRecord;
    public Sprite pausa;
    public Sprite candado;
    public Sprite check;
    public Sprite flecha;
    public Sprite manoTocar;
    public Sprite manoSujetar;
    public Sprite viento;
    public Sprite iconoPedido;
    public Sprite iconoPerfecto;
    public Sprite iconoMosca;
    public Sprite iconoQuemado;
    public Sprite iconoCorazon;
    public Sprite avatar;

    [Header("Mapa de niveles")]
    public Sprite nodoNivel;
    public Sprite nodoBloqueado;
    public Sprite nodoCompletado;
    public Sprite puntoCamino;
    public Sprite marcadorJugador;

    [Header("Escenario del juego")]
    public Sprite plato;
    public Sprite mostrador;
    public Sprite ventiladorBase;
    public Sprite ventiladorAspas;
    [Tooltip("Imagen negra con un hueco transparente en el centro (apagón)")]
    public Sprite oscuridad;
    public Sprite harina;

    [Header("Mosca (frames de animación)")]
    public Sprite[] mosca;

    private static UITheme cached;

    public static UITheme Instance
    {
        get
        {
            if (cached == null) cached = Resources.Load<UITheme>("TemaVisual");
            return cached;
        }
    }

    public static Sprite Get(ThemeKey key)
    {
        UITheme t = Instance;
        if (t == null) return null;
        switch (key)
        {
            case ThemeKey.Logo: return t.logo;
            case ThemeKey.FondoLogin: return t.fondoLogin != null ? t.fondoLogin : t.fondoMenu;
            case ThemeKey.FondoMenu: return t.fondoMenu;
            case ThemeKey.FondoMapa: return t.fondoMapa;
            case ThemeKey.Boton: return t.boton;
            case ThemeKey.BotonVerde: return t.botonVerde != null ? t.botonVerde : t.boton;
            case ThemeKey.BotonCerrar: return t.botonCerrar;
            case ThemeKey.Panel: return t.panel;
            case ThemeKey.Input: return t.input;
            case ThemeKey.Ticket: return t.ticket;
            case ThemeKey.Cinta: return t.cinta;
            case ThemeKey.BurbujaDialogo: return t.burbujaDialogo != null ? t.burbujaDialogo : t.panel;
            case ThemeKey.BurbujaCola: return t.burbujaCola;
            case ThemeKey.Rayos: return t.rayos;
            case ThemeKey.CorazonLleno: return t.corazonLleno;
            case ThemeKey.CorazonVacio: return t.corazonVacio;
            case ThemeKey.Estrella: return t.estrella;
            case ThemeKey.EstrellaVacia: return t.estrellaVacia != null ? t.estrellaVacia : t.estrella;
            case ThemeKey.NuevoRecord: return t.nuevoRecord;
            case ThemeKey.Pausa: return t.pausa;
            case ThemeKey.Candado: return t.candado;
            case ThemeKey.Check: return t.check;
            case ThemeKey.Flecha: return t.flecha;
            case ThemeKey.ManoTocar: return t.manoTocar;
            case ThemeKey.ManoSujetar: return t.manoSujetar != null ? t.manoSujetar : t.manoTocar;
            case ThemeKey.Viento: return t.viento;
            case ThemeKey.NodoNivel: return t.nodoNivel;
            case ThemeKey.NodoBloqueado: return t.nodoBloqueado != null ? t.nodoBloqueado : t.nodoNivel;
            case ThemeKey.NodoCompletado: return t.nodoCompletado != null ? t.nodoCompletado : t.nodoNivel;
            case ThemeKey.PuntoCamino: return t.puntoCamino;
            case ThemeKey.MarcadorJugador: return t.marcadorJugador != null ? t.marcadorJugador : t.avatar;
            case ThemeKey.Plato: return t.plato;
            case ThemeKey.Mostrador: return t.mostrador;
            case ThemeKey.VentiladorBase: return t.ventiladorBase;
            case ThemeKey.VentiladorAspas: return t.ventiladorAspas;
            case ThemeKey.Oscuridad: return t.oscuridad;
            case ThemeKey.Harina: return t.harina;
            case ThemeKey.IconoPedido: return t.iconoPedido != null ? t.iconoPedido : t.avatar;
            case ThemeKey.IconoPerfecto: return t.iconoPerfecto != null ? t.iconoPerfecto : t.estrella;
            case ThemeKey.IconoMosca: return t.iconoMosca != null ? t.iconoMosca : (t.mosca != null && t.mosca.Length > 0 ? t.mosca[0] : null);
            case ThemeKey.IconoQuemado: return t.iconoQuemado;
            case ThemeKey.IconoCorazon: return t.iconoCorazon != null ? t.iconoCorazon : t.corazonLleno;
            case ThemeKey.Avatar: return t.avatar;
        }
        return null;
    }

    // ------------------------------------------------------------------ imágenes que trae el juego
    /// <summary>Nombre de archivo de la imagen que trae el juego para cada casilla.</summary>
    public static readonly System.Collections.Generic.Dictionary<ThemeKey, string> DefaultNames =
        new System.Collections.Generic.Dictionary<ThemeKey, string>
    {
        { ThemeKey.Logo, "logo" }, { ThemeKey.FondoLogin, "fondo_menu" }, { ThemeKey.FondoMenu, "fondo_menu" },
        { ThemeKey.FondoMapa, "fondo_mapa" }, { ThemeKey.Boton, "boton" }, { ThemeKey.BotonVerde, "boton_verde" },
        { ThemeKey.BotonCerrar, "cerrar" }, { ThemeKey.Panel, "panel" }, { ThemeKey.Input, "input" },
        { ThemeKey.Ticket, "ticket" }, { ThemeKey.Cinta, "cinta" }, { ThemeKey.BurbujaDialogo, "burbuja_dialogo" },
        { ThemeKey.BurbujaCola, "burbuja_cola" }, { ThemeKey.Rayos, "rayos" }, { ThemeKey.CorazonLleno, "corazon_lleno" },
        { ThemeKey.CorazonVacio, "corazon_vacio" }, { ThemeKey.Estrella, "estrella" }, { ThemeKey.EstrellaVacia, "estrella_vacia" },
        { ThemeKey.NuevoRecord, "nuevo_record" }, { ThemeKey.Pausa, "btn_pausa" }, { ThemeKey.Candado, "candado" },
        { ThemeKey.Check, "check" }, { ThemeKey.Flecha, "flecha" }, { ThemeKey.ManoTocar, "mano_tocar" },
        { ThemeKey.ManoSujetar, "mano_sujetar" }, { ThemeKey.Viento, "viento" }, { ThemeKey.NodoNivel, "nodo_nivel" },
        { ThemeKey.NodoBloqueado, "nodo_bloqueado" }, { ThemeKey.NodoCompletado, "nodo_completado" },
        { ThemeKey.PuntoCamino, "punto_camino" }, { ThemeKey.MarcadorJugador, "marcador" }, { ThemeKey.Plato, "plato" },
        { ThemeKey.Mostrador, "mostrador" }, { ThemeKey.VentiladorBase, "ventilador_base" },
        { ThemeKey.VentiladorAspas, "ventilador_aspas" }, { ThemeKey.Oscuridad, "oscuridad" }, { ThemeKey.Harina, "harina" },
        { ThemeKey.IconoPedido, "icono_pedido" }, { ThemeKey.IconoPerfecto, "icono_perfecto" }, { ThemeKey.IconoMosca, "icono_mosca" },
        { ThemeKey.IconoQuemado, "icono_quemado" }, { ThemeKey.IconoCorazon, "corazon_lleno" }, { ThemeKey.Avatar, "avatar" },
    };

    /// <summary>¿Es una de las imágenes de ejemplo que trae el juego (y no una tuya)?</summary>
    public static bool IsBuiltInSprite(Sprite s)
    {
        if (s == null) return false;
        string n = s.name.ToLowerInvariant();
        foreach (string d in DefaultNames.Values) if (n == d) return true;
        return false;
    }

    /// <summary>Guarda una imagen en la casilla del tema (lo usa el editor cuando cambias una imagen en la escena).</summary>
    public void Set(ThemeKey key, Sprite s)
    {
        switch (key)
        {
            case ThemeKey.Logo: logo = s; break;
            case ThemeKey.FondoLogin: fondoLogin = s; break;
            case ThemeKey.FondoMenu: fondoMenu = s; break;
            case ThemeKey.FondoMapa: fondoMapa = s; break;
            case ThemeKey.Boton: boton = s; break;
            case ThemeKey.BotonVerde: botonVerde = s; break;
            case ThemeKey.BotonCerrar: botonCerrar = s; break;
            case ThemeKey.Panel: panel = s; break;
            case ThemeKey.Input: input = s; break;
            case ThemeKey.Ticket: ticket = s; break;
            case ThemeKey.Cinta: cinta = s; break;
            case ThemeKey.BurbujaDialogo: burbujaDialogo = s; break;
            case ThemeKey.BurbujaCola: burbujaCola = s; break;
            case ThemeKey.Rayos: rayos = s; break;
            case ThemeKey.CorazonLleno: corazonLleno = s; break;
            case ThemeKey.CorazonVacio: corazonVacio = s; break;
            case ThemeKey.Estrella: estrella = s; break;
            case ThemeKey.EstrellaVacia: estrellaVacia = s; break;
            case ThemeKey.NuevoRecord: nuevoRecord = s; break;
            case ThemeKey.Pausa: pausa = s; break;
            case ThemeKey.Candado: candado = s; break;
            case ThemeKey.Check: check = s; break;
            case ThemeKey.Flecha: flecha = s; break;
            case ThemeKey.ManoTocar: manoTocar = s; break;
            case ThemeKey.ManoSujetar: manoSujetar = s; break;
            case ThemeKey.Viento: viento = s; break;
            case ThemeKey.NodoNivel: nodoNivel = s; break;
            case ThemeKey.NodoBloqueado: nodoBloqueado = s; break;
            case ThemeKey.NodoCompletado: nodoCompletado = s; break;
            case ThemeKey.PuntoCamino: puntoCamino = s; break;
            case ThemeKey.MarcadorJugador: marcadorJugador = s; break;
            case ThemeKey.Plato: plato = s; break;
            case ThemeKey.Mostrador: mostrador = s; break;
            case ThemeKey.VentiladorBase: ventiladorBase = s; break;
            case ThemeKey.VentiladorAspas: ventiladorAspas = s; break;
            case ThemeKey.Oscuridad: oscuridad = s; break;
            case ThemeKey.Harina: harina = s; break;
            case ThemeKey.IconoPedido: iconoPedido = s; break;
            case ThemeKey.IconoPerfecto: iconoPerfecto = s; break;
            case ThemeKey.IconoMosca: iconoMosca = s; break;
            case ThemeKey.IconoQuemado: iconoQuemado = s; break;
            case ThemeKey.IconoCorazon: iconoCorazon = s; break;
            case ThemeKey.Avatar: avatar = s; break;
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        // Actualiza las imágenes de la escena abierta cuando cambias algo aquí
        UnityEditor.EditorApplication.delayCall += ThemedImage.RefreshAll;
    }
#endif
}
