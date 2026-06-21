using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[System.Serializable]
public class AvatarButtonData
{
    public Button button;
    public Image avatarImage;
    public string avatarId;
}

public class UsernameCloudSave : MonoBehaviour
{
    [Header("UI References")]
    public TMP_InputField usernameInput;
    public Button confirmButton;
    public Image selectedAvatar;

    [Header("Feedback Texts")]
    public TMP_Text msgEmptyName;
    public TMP_Text msgTooLong;
    public TMP_Text msgInvalidChars;
    public TMP_Text msgNotAuthenticated;
    public TMP_Text msgSaving;

    [Header("Avatars")]
    public List<AvatarButtonData> avatarButtons;

    private string currentAvatarId = "";

    private void Start()
    {
        confirmButton.onClick.AddListener(OnConfirmClicked);
        HideAllMessages();

        foreach (AvatarButtonData avatar in avatarButtons)
        {
            AvatarButtonData localAvatar = avatar;
            avatar.button.onClick.AddListener(() =>
            {
                SetSelectedAvatar(localAvatar.avatarImage.sprite, localAvatar.avatarId);
            });
        }

        if (string.IsNullOrEmpty(currentAvatarId) && avatarButtons.Count > 0)
        {
            SetSelectedAvatar(avatarButtons[0].avatarImage.sprite, avatarButtons[0].avatarId);
        }

        usernameInput.onSelect.AddListener((eventData) =>
        {
            usernameInput.ActivateInputField();
        });

        CheckIfUserIsRegistered();
    }

    public void SetSelectedAvatar(Sprite sprite, string avatarId)
    {
        if (selectedAvatar != null)
            selectedAvatar.sprite = sprite;

        currentAvatarId = avatarId;
    }

    private async void CheckIfUserIsRegistered()
    {
        HideAllMessages();

        if (!await EnsureAuthenticatedAsync())
        {
            ShowMessage(msgNotAuthenticated);
            return;
        }

        try
        {
            FirebaseWebUser profile = await FirebaseWebBridge.LoadUserProfileAsync();
            if (profile != null && profile.exists && !string.IsNullOrEmpty(profile.username))
            {
                CacheProfile(profile);
                await CloudSlotSync.PullAllSlotsAsync();
                UnityEngine.SceneManagement.SceneManager.LoadScene(2);
            }
            else
            {
                Debug.Log("[UsernameCloudSave] Jugador sin username, debe registrarse.");
            }
        }
        catch (Exception e)
        {
            Debug.LogError("[UsernameCloudSave] Error consultando registro: " + e.Message);
        }
    }

    public async void OnConfirmClicked()
    {
        HideAllMessages();

        string username = Capitalize(usernameInput.text.Trim());

        if (string.IsNullOrEmpty(username))
        {
            ShowMessage(msgEmptyName);
            return;
        }
        if (username.Length > 12)
        {
            ShowMessage(msgTooLong);
            return;
        }
        if (!System.Text.RegularExpressions.Regex.IsMatch(username, @"^[a-zA-Z0-9]+$"))
        {
            ShowMessage(msgInvalidChars);
            return;
        }
        if (!await EnsureAuthenticatedAsync())
        {
            ShowMessage(msgNotAuthenticated);
            return;
        }

        try
        {
            ShowMessage(msgSaving);

            string joinedAt = DateTime.UtcNow.ToString("o");
            int userNumber = unchecked((int)DateTimeOffset.UtcNow.ToUnixTimeSeconds());

            FirebaseWebProfileInput playerData = new FirebaseWebProfileInput
            {
                username = username,
                avatarId = currentAvatarId,
                joined_at = joinedAt,
                userNumber = userNumber
            };

            FirebaseWebUser savedProfile = await FirebaseWebBridge.SaveUserProfileAsync(playerData);
            CacheProfile(savedProfile);

            UnityEngine.SceneManagement.SceneManager.LoadScene(2);
        }
        catch (Exception e)
        {
            Debug.LogError("[UsernameCloudSave] Error guardando perfil: " + e.Message);
        }
    }

    private static async Task<bool> EnsureAuthenticatedAsync()
    {
        await FirebaseWebBridge.EnsureInitializedAsync();

        if (!FirebaseWebBridge.IsSignedIn)
            await FirebaseWebBridge.RefreshCurrentUserAsync();

        return FirebaseWebBridge.IsSignedIn;
    }

    private static void CacheProfile(FirebaseWebUser profile)
    {
        if (profile == null) return;

        PlayerPrefs.SetString("userType", "firebase");
        PlayerPrefs.SetString("playerId", profile.uid);
        PlayerPrefs.SetString("username", profile.username);
        PlayerPrefs.SetString("avatarId", profile.avatarId);
        PlayerPrefs.SetString("joined_at", profile.joined_at);
        PlayerPrefs.SetInt("userNumber", profile.userNumber);
        PlayerPrefs.Save();
    }

    private void HideAllMessages()
    {
        if (msgEmptyName) msgEmptyName.gameObject.SetActive(false);
        if (msgTooLong) msgTooLong.gameObject.SetActive(false);
        if (msgInvalidChars) msgInvalidChars.gameObject.SetActive(false);
        if (msgNotAuthenticated) msgNotAuthenticated.gameObject.SetActive(false);
        if (msgSaving) msgSaving.gameObject.SetActive(false);
    }

    private void ShowMessage(TMP_Text msg)
    {
        HideAllMessages();
        if (msg != null) msg.gameObject.SetActive(true);
    }

    private string Capitalize(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;
        return char.ToUpper(input[0]) + input.Substring(1);
    }
}
