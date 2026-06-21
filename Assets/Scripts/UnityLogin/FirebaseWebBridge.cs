using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
#if UNITY_ANDROID && !UNITY_EDITOR
using Firebase;
using Firebase.Auth;
#endif

[Serializable]
public class FirebaseWebUser
{
    public bool signedIn;
    public bool exists;
    public string uid;
    public string email;
    public string displayName;
    public string photoURL;
    public string provider;
    public string username;
    public string avatarId;
    public string joined_at;
    public int userNumber;
}

[Serializable]
public class FirebaseWebProfileInput
{
    public string username;
    public string avatarId;
    public string joined_at;
    public int userNumber;
}

[Serializable]
public class FirebaseWebSlotResult
{
    public bool exists;
    public string slotId;
    public string json;
}

[Serializable]
internal class FirebaseBridgeResponse
{
    public string requestId;
    public bool success;
    public string action;
    public string payload;
    public string error;
}

public class FirebaseWebBridge : MonoBehaviour
{
    private const string BridgeObjectName = "FirebaseWebBridge";
    private const string AndroidGoogleWebClientId = "702777328635-cnj34u37po4ua46din71oo8g61912f42.apps.googleusercontent.com";

    private static FirebaseWebBridge instance;
    private static readonly Dictionary<string, TaskCompletionSource<FirebaseBridgeResponse>> Pending =
        new Dictionary<string, TaskCompletionSource<FirebaseBridgeResponse>>();

#if UNITY_ANDROID && !UNITY_EDITOR
    private static FirebaseApp androidApp;
    private static FirebaseAuth androidAuth;
    private const string FirestoreBaseUrl = "https://firestore.googleapis.com/v1/projects/{0}/databases/(default)/documents";
#endif

    public static bool IsReady { get; private set; }
    public static bool IsSignedIn => CurrentUser != null && CurrentUser.signedIn && !string.IsNullOrEmpty(CurrentUser.uid);
    public static FirebaseWebUser CurrentUser { get; private set; }

    public static bool IsFirebaseRuntimeSupported
    {
        get
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return true;
#elif UNITY_ANDROID && !UNITY_EDITOR
            return true;
#else
            return false;
#endif
        }
    }

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")] private static extern void FirebaseBridge_Initialize(string gameObjectName, string requestId);
    [DllImport("__Internal")] private static extern void FirebaseBridge_GetCurrentUser(string requestId);
    [DllImport("__Internal")] private static extern void FirebaseBridge_SignInWithGoogle(string requestId);
    [DllImport("__Internal")] private static extern void FirebaseBridge_SignOut(string requestId);
    [DllImport("__Internal")] private static extern void FirebaseBridge_LoadUserProfile(string requestId);
    [DllImport("__Internal")] private static extern void FirebaseBridge_SaveUserProfile(string requestId, string json);
    [DllImport("__Internal")] private static extern void FirebaseBridge_LoadSlot(string requestId, string slotId);
    [DllImport("__Internal")] private static extern void FirebaseBridge_SaveSlot(string requestId, string slotId, string json);
#endif

    public static FirebaseWebBridge Instance
    {
        get
        {
            if (instance != null) return instance;

            GameObject existing = GameObject.Find(BridgeObjectName);
            if (existing == null)
                existing = new GameObject(BridgeObjectName);

            instance = existing.GetComponent<FirebaseWebBridge>();
            if (instance == null)
                instance = existing.AddComponent<FirebaseWebBridge>();

            DontDestroyOnLoad(existing);
            return instance;
        }
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        gameObject.name = BridgeObjectName;
        DontDestroyOnLoad(gameObject);
    }

    public static async Task<FirebaseWebUser> EnsureInitializedAsync()
    {
        _ = Instance;

#if UNITY_WEBGL && !UNITY_EDITOR
        FirebaseBridgeResponse response = await CallAsync(id => FirebaseBridge_Initialize(BridgeObjectName, id));
        IsReady = true;
        CurrentUser = ParseUser(response.payload);
        CacheSession(CurrentUser);
        return CurrentUser;
#elif UNITY_ANDROID && !UNITY_EDITOR
        await EnsureAndroidInitializedAsync();
        CurrentUser = ParseUser(androidAuth.CurrentUser);
        CacheSession(CurrentUser);
        return CurrentUser;
#else
        IsReady = true;
        CurrentUser = new FirebaseWebUser { signedIn = false };
        await Task.CompletedTask;
        return CurrentUser;
#endif
    }

    public static async Task<FirebaseWebUser> RefreshCurrentUserAsync()
    {
        _ = Instance;

#if UNITY_WEBGL && !UNITY_EDITOR
        FirebaseBridgeResponse response = await CallAsync(FirebaseBridge_GetCurrentUser);
        CurrentUser = ParseUser(response.payload);
        CacheSession(CurrentUser);
        return CurrentUser;
#elif UNITY_ANDROID && !UNITY_EDITOR
        await EnsureAndroidInitializedAsync();
        if (androidAuth.CurrentUser != null)
            await androidAuth.CurrentUser.ReloadAsync();

        CurrentUser = ParseUser(androidAuth.CurrentUser);
        CacheSession(CurrentUser);
        return CurrentUser;
#else
        await Task.CompletedTask;
        return CurrentUser ?? new FirebaseWebUser { signedIn = false };
#endif
    }

    public static async Task<FirebaseWebUser> SignInWithGoogleAsync()
    {
        _ = Instance;

#if UNITY_WEBGL && !UNITY_EDITOR
        FirebaseBridgeResponse response = await CallAsync(FirebaseBridge_SignInWithGoogle);
        CurrentUser = ParseUser(response.payload);
        CacheSession(CurrentUser);
        return CurrentUser;
#elif UNITY_ANDROID && !UNITY_EDITOR
        await EnsureAndroidInitializedAsync();
        FirebaseBridgeResponse response = await CallAsync(id =>
        {
            AndroidJavaClass bridgeClass = new AndroidJavaClass("com.saintmartin.chessescape.GoogleSignInBridge");
            try
            {
                bridgeClass.CallStatic("signIn", BridgeObjectName, id, AndroidGoogleWebClientId);
            }
            finally
            {
                bridgeClass.Dispose();
            }
        });

        CurrentUser = ParseUser(response.payload);
        CacheSession(CurrentUser);
        return CurrentUser;
#else
        await Task.CompletedTask;
        return CurrentUser ?? new FirebaseWebUser { signedIn = false };
#endif
    }

    public static async Task SignOutAsync()
    {
        _ = Instance;

#if UNITY_WEBGL && !UNITY_EDITOR
        await CallAsync(FirebaseBridge_SignOut);
#elif UNITY_ANDROID && !UNITY_EDITOR
        await EnsureAndroidInitializedAsync();
        androidAuth.SignOut();
#else
        await Task.CompletedTask;
#endif
        CurrentUser = new FirebaseWebUser { signedIn = false };
        ClearSessionCache();
    }

    public static async Task<FirebaseWebUser> LoadUserProfileAsync()
    {
        _ = Instance;

#if UNITY_WEBGL && !UNITY_EDITOR
        FirebaseBridgeResponse response = await CallAsync(FirebaseBridge_LoadUserProfile);
        CurrentUser = ParseUser(response.payload);
        CacheSession(CurrentUser);
        return CurrentUser;
#elif UNITY_ANDROID && !UNITY_EDITOR
        await EnsureAndroidInitializedAsync();
        if (androidAuth.CurrentUser == null || string.IsNullOrEmpty(androidAuth.CurrentUser.UserId))
            return new FirebaseWebUser { signedIn = false };

        string token = await GetAndroidIdTokenAsync();
        if (string.IsNullOrEmpty(token))
            throw new Exception("No token de Firebase disponible.");

        string projectId = androidApp.Options.ProjectId;
        string url = string.Format(FirestoreBaseUrl, projectId) + "/users/" + androidAuth.CurrentUser.UserId;

        using UnityWebRequest request = UnityWebRequest.Get(url);
        request.SetRequestHeader("Authorization", "Bearer " + token);
        request.downloadHandler = new DownloadHandlerBuffer();

        await request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            if (request.responseCode == 404)
                return new FirebaseWebUser { signedIn = true, exists = false, uid = androidAuth.CurrentUser.UserId, email = androidAuth.CurrentUser.Email, displayName = androidAuth.CurrentUser.DisplayName, photoURL = androidAuth.CurrentUser.PhotoUrl?.ToString() };

            throw new Exception("Firestore loadUserProfile failed: " + request.error + " / " + request.downloadHandler.text);
        }

        FirestoreUserDocumentResponse document = JsonUtility.FromJson<FirestoreUserDocumentResponse>(request.downloadHandler.text);
        FirebaseWebUser user = ParseUser(androidAuth.CurrentUser);
        if (document?.fields != null)
        {
            user.exists = true;
            if (document.fields.username != null)
                user.username = document.fields.username.stringValue;
            if (document.fields.avatarId != null)
                user.avatarId = document.fields.avatarId.stringValue;
            if (document.fields.joined_at != null)
                user.joined_at = document.fields.joined_at.stringValue;
            if (document.fields.userNumber != null && int.TryParse(document.fields.userNumber.integerValue, out int number))
                user.userNumber = number;
            if (document.fields.provider != null)
                user.provider = document.fields.provider.stringValue;
        }

        CacheSession(user);
        return user;
#else
        await Task.CompletedTask;
        return CurrentUser ?? new FirebaseWebUser { signedIn = false };
#endif
    }

    public static async Task<FirebaseWebUser> SaveUserProfileAsync(FirebaseWebProfileInput profile)
    {
        _ = Instance;
        string json = JsonUtility.ToJson(profile);

#if UNITY_WEBGL && !UNITY_EDITOR
        FirebaseBridgeResponse response = await CallAsync(id => FirebaseBridge_SaveUserProfile(id, json));
        CurrentUser = ParseUser(response.payload);
        CacheSession(CurrentUser);
        return CurrentUser;
#elif UNITY_ANDROID && !UNITY_EDITOR
        await EnsureAndroidInitializedAsync();
        if (androidAuth.CurrentUser == null || string.IsNullOrEmpty(androidAuth.CurrentUser.UserId))
            throw new Exception("No hay usuario autenticado para guardar el perfil.");

        string token = await GetAndroidIdTokenAsync();
        if (string.IsNullOrEmpty(token))
            throw new Exception("No token de Firebase disponible.");

        string projectId = androidApp.Options.ProjectId;
        string documentName = GetFirestoreDocumentName(projectId, $"users/{androidAuth.CurrentUser.UserId}");
        string url = string.Format(FirestoreBaseUrl, projectId) + ":commit";

        string payload = BuildFirestoreCommitPayload(documentName, new Dictionary<string, string>
        {
            { "uid", androidAuth.CurrentUser.UserId },
            { "email", androidAuth.CurrentUser.Email ?? string.Empty },
            { "displayName", androidAuth.CurrentUser.DisplayName ?? string.Empty },
            { "photoURL", androidAuth.CurrentUser.PhotoUrl?.ToString() ?? string.Empty },
            { "provider", "google" },
            { "username", profile.username ?? string.Empty },
            { "avatarId", profile.avatarId ?? string.Empty },
            { "joined_at", profile.joined_at ?? string.Empty },
            { "updated_at", DateTime.UtcNow.ToString("o") }
        }, new Dictionary<string, long>
        {
            { "userNumber", profile.userNumber }
        });

        using (UnityWebRequest request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(payload);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", "Bearer " + token);

            await request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
                throw new Exception("Firestore saveUserProfile failed: " + request.error + " / " + request.downloadHandler.text);
        }

        FirebaseWebUser user = ParseUser(androidAuth.CurrentUser);
        user.exists = true;
        user.username = profile.username;
        user.avatarId = profile.avatarId;
        user.joined_at = profile.joined_at;
        user.userNumber = profile.userNumber;
        user.provider = "google";

        CacheSession(user);
        return user;
#else
        await Task.CompletedTask;
        return CurrentUser ?? new FirebaseWebUser { signedIn = false };
#endif
    }

    public static async Task<FirebaseWebSlotResult> LoadSlotAsync(string slotId)
    {
        _ = Instance;

#if UNITY_WEBGL && !UNITY_EDITOR
        FirebaseBridgeResponse response = await CallAsync(id => FirebaseBridge_LoadSlot(id, slotId));
        return JsonUtility.FromJson<FirebaseWebSlotResult>(response.payload);
#elif UNITY_ANDROID && !UNITY_EDITOR
        await EnsureAndroidInitializedAsync();
        if (androidAuth.CurrentUser == null || string.IsNullOrEmpty(androidAuth.CurrentUser.UserId))
            return new FirebaseWebSlotResult { exists = false, slotId = slotId, json = string.Empty };

        string token = await GetAndroidIdTokenAsync();
        if (string.IsNullOrEmpty(token))
            throw new Exception("No token de Firebase disponible.");

        string projectId = androidApp.Options.ProjectId;
        string url = string.Format(FirestoreBaseUrl, projectId) + "/users/" + androidAuth.CurrentUser.UserId + "/slots/" + slotId;

        using UnityWebRequest request = UnityWebRequest.Get(url);
        request.SetRequestHeader("Authorization", "Bearer " + token);
        request.downloadHandler = new DownloadHandlerBuffer();

        await request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            if (request.responseCode == 404)
                return new FirebaseWebSlotResult { exists = false, slotId = slotId, json = string.Empty };

            throw new Exception("Firestore loadSlot failed: " + request.error + " / " + request.downloadHandler.text);
        }

        FirestoreSlotDocumentResponse document = JsonUtility.FromJson<FirestoreSlotDocumentResponse>(request.downloadHandler.text);
        bool exists = document?.fields?.json != null;
        string jsonValue = exists ? document.fields.json.stringValue : string.Empty;
        return new FirebaseWebSlotResult { exists = exists, slotId = slotId, json = jsonValue };
#else
        await Task.CompletedTask;
        return new FirebaseWebSlotResult { exists = false, slotId = slotId, json = string.Empty };
#endif
    }

    public static async Task SaveSlotAsync(string slotId, string json)
    {
        _ = Instance;

#if UNITY_WEBGL && !UNITY_EDITOR
        await CallAsync(id => FirebaseBridge_SaveSlot(id, slotId, json));
#elif UNITY_ANDROID && !UNITY_EDITOR
        await EnsureAndroidInitializedAsync();
        if (androidAuth.CurrentUser == null || string.IsNullOrEmpty(androidAuth.CurrentUser.UserId))
            throw new Exception("No hay usuario autenticado para guardar la ranura.");

        string token = await GetAndroidIdTokenAsync();
        if (string.IsNullOrEmpty(token))
            throw new Exception("No token de Firebase disponible.");

        string projectId = androidApp.Options.ProjectId;
        string documentName = GetFirestoreDocumentName(projectId, $"users/{androidAuth.CurrentUser.UserId}/slots/{slotId}");
        string url = string.Format(FirestoreBaseUrl, projectId) + ":commit";

        string payload = BuildFirestoreCommitPayload(documentName, new Dictionary<string, string>
        {
            { "slotId", slotId },
            { "json", json ?? string.Empty },
            { "updated_at", DateTime.UtcNow.ToString("o") }
        }, null);

        using (UnityWebRequest request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(payload);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", "Bearer " + token);

            await request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
                throw new Exception("Firestore saveSlot failed: " + request.error + " / " + request.downloadHandler.text);
        }
#else
        await Task.CompletedTask;
#endif
    }

    public void OnFirebaseBridgeResult(string json)
    {
        FirebaseBridgeResponse response = JsonUtility.FromJson<FirebaseBridgeResponse>(json);
        if (response == null || string.IsNullOrEmpty(response.requestId))
        {
            Debug.LogError("[FirebaseWebBridge] Respuesta invalida: " + json);
            return;
        }

        if (!Pending.TryGetValue(response.requestId, out TaskCompletionSource<FirebaseBridgeResponse> tcs))
        {
            Debug.LogWarning("[FirebaseWebBridge] Respuesta sin request pendiente: " + response.requestId);
            return;
        }

        Pending.Remove(response.requestId);

        if (response.success)
            tcs.TrySetResult(response);
        else
            tcs.TrySetException(new Exception(string.IsNullOrEmpty(response.error) ? "Firebase Web SDK error." : response.error));
    }

    private static Task<FirebaseBridgeResponse> CallAsync(Action<string> invoke)
    {
        string requestId = Guid.NewGuid().ToString("N");
        TaskCompletionSource<FirebaseBridgeResponse> tcs = new TaskCompletionSource<FirebaseBridgeResponse>();
        Pending[requestId] = tcs;

        try
        {
            invoke(requestId);
        }
        catch (Exception ex)
        {
            Pending.Remove(requestId);
            tcs.TrySetException(ex);
        }

        return tcs.Task;
    }

    private static FirebaseWebUser ParseUser(string payload)
    {
        if (string.IsNullOrEmpty(payload))
            return new FirebaseWebUser { signedIn = false };

        try
        {
            FirebaseWebUser user = JsonUtility.FromJson<FirebaseWebUser>(payload);
            if (user == null)
                return new FirebaseWebUser { signedIn = false };

            user.signedIn = true;
            return user;
        }
        catch (Exception)
        {
            return new FirebaseWebUser { signedIn = false };
        }
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    private static FirebaseWebUser ParseUser(FirebaseUser user)
    {
        if (user == null)
            return new FirebaseWebUser { signedIn = false };

        return new FirebaseWebUser
        {
            signedIn = true,
            exists = false,
            uid = user.UserId,
            email = user.Email,
            displayName = user.DisplayName,
            photoURL = user.PhotoUrl?.ToString(),
            provider = "google"
        };
    }

    private static async Task EnsureAndroidInitializedAsync()
    {
        if (IsReady && androidApp != null && androidAuth != null)
            return;

        var dependencyStatus = await FirebaseApp.CheckAndFixDependenciesAsync();
        if (dependencyStatus != DependencyStatus.Available)
            throw new Exception($"Firebase dependencies no disponibles: {dependencyStatus}");

        androidApp = FirebaseApp.DefaultInstance;
        androidAuth = FirebaseAuth.DefaultInstance;
        IsReady = true;
    }

    private static async Task<string> GetAndroidIdTokenAsync()
    {
        if (androidAuth?.CurrentUser == null)
            return null;

        var tokenResult = await androidAuth.CurrentUser.TokenAsync(false);
        return tokenResult;
    }

    private static string GetFirestoreDocumentName(string projectId, string documentPath)
    {
        return $"projects/{projectId}/databases/(default)/documents/{documentPath}";
    }

    private static string BuildFirestoreCommitPayload(string documentName, Dictionary<string, string> stringFields, Dictionary<string, long> integerFields)
    {
        var sb = new StringBuilder();
        sb.Append("{\"writes\":[{\"update\":{\"name\":\"");
        sb.Append(EscapeJson(documentName));
        sb.Append("\",\"fields\":{");

        bool first = true;
        if (stringFields != null)
        {
            foreach (var field in stringFields)
            {
                if (!first) sb.Append(',');
                first = false;
                sb.Append("\"");
                sb.Append(EscapeJson(field.Key));
                sb.Append("\":{\"stringValue\":\"");
                sb.Append(EscapeJson(field.Value ?? string.Empty));
                sb.Append("\"}");
            }
        }

        if (integerFields != null)
        {
            foreach (var field in integerFields)
            {
                if (!first) sb.Append(',');
                first = false;
                sb.Append("\"");
                sb.Append(EscapeJson(field.Key));
                sb.Append("\":{\"integerValue\":\"");
                sb.Append(field.Value);
                sb.Append("\"}");
            }
        }

        sb.Append("}}}]}");
        return sb.ToString();
    }

    private static string EscapeJson(string str)
    {
        if (string.IsNullOrEmpty(str))
            return string.Empty;

        return str.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r").Replace("\t", "\\t");
    }

    [Serializable]
    private class FirestoreUserDocumentResponse
    {
        public FirestoreUserFields fields;
    }

    [Serializable]
    private class FirestoreSlotDocumentResponse
    {
        public FirestoreSlotFields fields;
    }

    [Serializable]
    private class FirestoreUserFields
    {
        public FirestoreString username;
        public FirestoreString avatarId;
        public FirestoreString joined_at;
        public FirestoreInteger userNumber;
        public FirestoreString provider;
    }

    [Serializable]
    private class FirestoreSlotFields
    {
        public FirestoreString slotId;
        public FirestoreString json;
    }

    [Serializable]
    private class FirestoreString
    {
        public string stringValue;
    }

    [Serializable]
    private class FirestoreInteger
    {
        public string integerValue;
    }
#endif

    private static void CacheSession(FirebaseWebUser user)
    {
        if (user == null || !user.signedIn || string.IsNullOrEmpty(user.uid))
            return;

        PlayerPrefs.SetString("userType", "firebase");
        PlayerPrefs.SetString("playerId", user.uid);

        if (!string.IsNullOrEmpty(user.username))
            PlayerPrefs.SetString("username", user.username);
        if (!string.IsNullOrEmpty(user.avatarId))
            PlayerPrefs.SetString("avatarId", user.avatarId);
        if (!string.IsNullOrEmpty(user.joined_at))
            PlayerPrefs.SetString("joined_at", user.joined_at);
        if (user.userNumber > 0)
            PlayerPrefs.SetInt("userNumber", user.userNumber);

        PlayerPrefs.Save();
    }

    private static void ClearSessionCache()
    {
        PlayerPrefs.DeleteKey("userType");
        PlayerPrefs.DeleteKey("playerId");
        PlayerPrefs.DeleteKey("username");
        PlayerPrefs.DeleteKey("avatarId");
        PlayerPrefs.DeleteKey("joined_at");
        PlayerPrefs.DeleteKey("userNumber");
        PlayerPrefs.Save();
    }
}
