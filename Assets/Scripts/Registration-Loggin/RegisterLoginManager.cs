using UnityEngine;
using TMPro;
using System.Collections;
using System.Text.RegularExpressions;
using UnityEngine.Networking;
using UnityEngine.UI; // Add this namespace for Toggle and other UI components
using UnityEngine.SceneManagement; // Add this namespace for scene management

public class RegisterLoginManager : MonoBehaviour
{
    [Header("UI Components")]
    public TMP_InputField emailInput;
    public TMP_InputField passwordInput;
    public TMP_InputField emailForPasswordChangeInput;
    public TextMeshProUGUI errorMessageText;
    public Toggle showPasswordToggle;

    [Header("Game Objects")]
    public GameObject registrationPanel;
    public GameObject mainLoginForm;
    public GameObject afterRegistrationOpen;
    public GameObject afterRegistrationSuccess;
    public GameObject afterLoginOpen;
    public GameObject mainMenu;
    public GameObject additionalGameObject;

    private static bool isLoggedIn = false;
    private string supabaseUrl = "https://nhtwbrztvgchuefjjzgt.supabase.co";
    private string anonPublicKey = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6Im5odHdicnp0dmdjaHVlZmpqemd0Iiwicm9sZSI6ImFub24iLCJpYXQiOjE3NDEyNTIyOTYsImV4cCI6MjA1NjgyODI5Nn0.-gABrEYEL0QxEqEzO-bfvnQ_RjnvQhHZFUgNpe6AxIs";
    private string authEndpoint = "/auth/v1";

    private void Start()
    {
        if (isLoggedIn)
        {
            mainLoginForm.SetActive(false);
            mainMenu.SetActive(true);
        }
        else
        {
            registrationPanel.SetActive(false);
            mainLoginForm.SetActive(true);
        }

        passwordInput.contentType = TMP_InputField.ContentType.Password;
        if (showPasswordToggle != null)
        {
            showPasswordToggle.onValueChanged.AddListener(ToggleShowPassword);
        }

        SceneManager.sceneLoaded += OnSceneLoaded; // Subscribe to scene loaded event
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded; // Unsubscribe from scene loaded event
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (isLoggedIn)
        {
            mainLoginForm.SetActive(false);
            mainMenu.SetActive(true);
        }
    }

    public void Register()
    {
        StartCoroutine(RegisterUser(emailInput.text, passwordInput.text));
    }

    public void Login()
    {
        StartCoroutine(LoginUser(emailInput.text, passwordInput.text));
    }

    public void ChangePassword()
    {
        StartCoroutine(SendPasswordResetEmail(emailForPasswordChangeInput.text));
    }

    private IEnumerator RegisterUser(string email, string password)
    {
        string url = supabaseUrl + authEndpoint + "/signup";
        string jsonData = $"{{\"email\":\"{email}\",\"password\":\"{password}\",\"data\":{{\"email_confirmed_at\":\"{System.DateTime.UtcNow.ToString("o")}\"}}}}";

        UnityWebRequest request = new UnityWebRequest(url, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("apikey", anonPublicKey);

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log($"[RegisterUser] Registration successful for email: {email}");
            ShowErrorMessage("Registration successful.");

            afterRegistrationOpen.SetActive(true);
            additionalGameObject.SetActive(false);
            StartCoroutine(LoginUser(email, password));
        }
        else
        {
            Debug.LogError($"[RegisterUser] Registration failed: {request.downloadHandler.text}");
            ShowErrorMessage("Registration failed. Please check your details and try again.");
        }
    }

    private IEnumerator LoginUser(string email, string password)
    {
        email = email.Trim();
        password = password.Trim();

        string url = supabaseUrl + authEndpoint + "/token?grant_type=password";
        string jsonData = $"{{\"email\":\"{email}\",\"password\":\"{password}\"}}";

        UnityWebRequest request = new UnityWebRequest(url, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("apikey", anonPublicKey);

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log($"[LoginUser] Server response: {request.downloadHandler.text}");
            afterLoginOpen.SetActive(true);
            mainLoginForm.SetActive(false);
            mainMenu.SetActive(true);
            isLoggedIn = true;
        }
        else
        {
            Debug.LogError($"[LoginUser] Login failed: {request.downloadHandler.text}");
            ShowErrorMessage("Login failed. Please check your credentials and try again.");
        }
    }

    private IEnumerator SendPasswordResetEmail(string email)
    {
        email = email.Trim();

        Debug.Log($"[SendPasswordResetEmail] Trimmed email: {email}");

        if (!IsValidEmail(email))
        {
            ShowErrorMessage("Invalid email address. Please enter a valid email.");
            Debug.LogError($"[SendPasswordResetEmail] Email validation failed for: {email}");
            yield break;
        }

        // Explicitly set the redirectTo URL to the reset password form
        string redirectToUrl = "https://praise-game.eu/resetPasswordForm.html";
        string url = $"{supabaseUrl}{authEndpoint}/recover";
        string jsonData = $"{{\"email\":\"{email}\",\"redirectTo\":\"{redirectToUrl}\"}}";

        Debug.Log($"[SendPasswordResetEmail] Sending JSON payload: {jsonData}");

        UnityWebRequest request = new UnityWebRequest(url, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("apikey", anonPublicKey);

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log($"[SendPasswordResetEmail] Password reset email sent successfully for email: {email}");
            ShowErrorMessage("Password reset email sent successfully. Please check your inbox.");
        }
        else
        {
            Debug.LogError($"[SendPasswordResetEmail] Failed to send password reset email: {request.downloadHandler.text}");
            ShowErrorMessage("Failed to send password reset email. Please try again.");
        }
    }

    private bool IsValidEmail(string email)
    {
        string emailPattern = @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$";
        return Regex.IsMatch(email, emailPattern);
    }

    private void ShowErrorMessage(string message)
    {
        errorMessageText.text = message;
        errorMessageText.gameObject.SetActive(true);
        StartCoroutine(HideErrorMessageAfterDelay());
    }

    private IEnumerator HideErrorMessageAfterDelay()
    {
        yield return new WaitForSeconds(5f);
        errorMessageText.gameObject.SetActive(false);
    }

    private void ToggleShowPassword(bool isOn)
    {
        passwordInput.contentType = isOn ? TMP_InputField.ContentType.Standard : TMP_InputField.ContentType.Password;
        passwordInput.ForceLabelUpdate();
    }
}
