using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
#endif

/// <summary>
/// Hace que el juego funcione con CUALQUIER opción de "Active Input Handling" (Player Settings):
///  • Input Manager (Old), Input System Package (New) o Both.
/// Si el proyecto usa SOLO el Input System nuevo (Unity lo pone así al cambiar a WebGL),
/// cambia el StandaloneInputModule del EventSystem por el InputSystemUIInputModule al cargar cada escena.
/// </summary>
public static class InputCompat
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Init()
    {
        FixEventSystems();
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => FixEventSystems();

    public static void FixEventSystems()
    {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        foreach (EventSystem es in Object.FindObjectsByType<EventSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            StandaloneInputModule old = es.GetComponent<StandaloneInputModule>();
            if (old != null)
            {
                old.enabled = false;
                Object.DestroyImmediate(old);
            }
            InputSystemUIInputModule m = es.GetComponent<InputSystemUIInputModule>();
            if (m == null) m = es.gameObject.AddComponent<InputSystemUIInputModule>();
            if (m.actionsAsset == null) m.AssignDefaultActions();   // clic, toque, arrastrar, teclado
        }
#endif
    }

    /// <summary>¿Se presionó la barra espaciadora este frame? (funciona con los dos sistemas de entrada)</summary>
    public static bool SpacePressed()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame) return true;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKeyDown(KeyCode.Space)) return true;
#endif
        return false;
    }
}
