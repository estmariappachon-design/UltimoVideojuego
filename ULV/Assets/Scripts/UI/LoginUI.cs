using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Pantalla de login / registro (LoginScene).
/// </summary>
public class LoginUI : MonoBehaviour
{
    [Header("Campos")]
    public TMP_InputField usernameInput;   // solo se usa al registrarse
    public TMP_InputField emailInput;
    public TMP_InputField passwordInput;   // Content Type = Password en el Inspector

    [Header("Botones")]
    public Button registerButton;
    public Button loginButton;

    [Header("Mensajes")]
    public TMP_Text messageText;

    private void Start()
    {
        registerButton.onClick.AddListener(OnRegister);
        loginButton.onClick.AddListener(OnLogin);
        StartCoroutine(WaitForFirebase());
    }

    private IEnumerator WaitForFirebase()
    {
        SetInteractable(false);
        messageText.text = "Conectando con Firebase...";

        while (FirebaseManager.Instance == null ||
               (!FirebaseManager.Instance.IsReady && string.IsNullOrEmpty(FirebaseManager.Instance.InitError)))
        {
            yield return null;
        }

        if (!FirebaseManager.Instance.IsReady)
        {
            messageText.text = FirebaseManager.Instance.InitError;
            yield break;
        }

        TryAutoLogin();
    }

    private async void TryAutoLogin()
    {
        messageText.text = "Revisando sesión guardada...";
        bool ok = await FirebaseManager.Instance.TryAutoLoginAsync();
        if (ok)
        {
            FirebaseManager.Instance.GoToMenu();
            return;
        }
        messageText.text = "";
        SetInteractable(true);
    }

    private async void OnRegister()
    {
        string username = usernameInput.text.Trim();
        string email = emailInput.text.Trim();
        string password = passwordInput.text;

        if (username.Length < 3 || username.Length > 15)
        {
            messageText.text = "El nombre debe tener entre 3 y 15 caracteres.";
            return;
        }
        if (string.IsNullOrEmpty(email) || password.Length < 6)
        {
            messageText.text = "Correo obligatorio y contraseña de mínimo 6 caracteres.";
            return;
        }

        SetInteractable(false);
        messageText.text = "Registrando...";

        string error = await FirebaseManager.Instance.RegisterAsync(username, email, password);
        if (error == null)
        {
            messageText.text = "¡Cuenta creada!";
            FirebaseManager.Instance.GoToMenu();
        }
        else
        {
            messageText.text = error;
            SetInteractable(true);
        }
    }

    private async void OnLogin()
    {
        string email = emailInput.text.Trim();
        string password = passwordInput.text;

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            messageText.text = "Escribe correo y contraseña.";
            return;
        }

        SetInteractable(false);
        messageText.text = "Iniciando sesión...";

        string error = await FirebaseManager.Instance.LoginAsync(email, password);
        if (error == null)
        {
            FirebaseManager.Instance.GoToMenu();
        }
        else
        {
            messageText.text = error;
            SetInteractable(true);
        }
    }

    private void SetInteractable(bool value)
    {
        registerButton.interactable = value;
        loginButton.interactable = value;
    }
}
