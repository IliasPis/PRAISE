using UnityEngine;
using TMPro;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.UI;
using System.Collections;

public class MultiplayerManager : MonoBehaviourPunCallbacks
{
    [Header("Lobby UI Elements")]
    public TMP_Text roomCodeText;
    public TMP_Text playerNameText;
    public TMP_Text opponentNameText;
    public GameObject playerAvatar;
    public GameObject opponentAvatar;
    public GameObject playerReadyButton;
    public GameObject opponentReadyButton;
    public GameObject playerCheckmark; // Player's ready checkmark
    public GameObject opponentCheckmark; // Opponent's ready checkmark
    public TMP_InputField joinCodeInput;
    public TMP_Text invalidCodeMessage;
    public GameObject quickMatchScreen;
    public TMP_Text quickMatchTimerText;
    public GameObject timeoutMessagePanel;
    public TMP_Text timeoutMessageText;
    public GameObject tryAgainButton;
    public GameObject playWithAIButton;

    [Header("Game State Transition")]
    public GameObject currentLobbyUI;
    public GameObject gameUI;
    public string gameSceneName; // Scene to load when both players are ready

    [Header("Join Lobby Transitions")]
    public GameObject joinCodeLobby;
    public GameObject joinLobbyUI;

    [Header("Opponent Connection UI")]
    public GameObject waitPlayerText; // To be set active false
    public GameObject nameOfPlayer2;  // To be set active true

    public Image playerAvatarImage;  // Player's avatar image
    public Image opponentAvatarImage; // Opponent's avatar image

    [Header("Single Player Avatar Save")]
    [Tooltip("Assign the image whose sprite you want to save for single player.")]
    public Image singlePlayerAvatarImage;

    private bool isPlayerReady = false;
    private bool isOpponentReady = false;
    private bool isQuickMatch = false;
    private float quickMatchTime = 60f;

    private const string AvatarKey = "Avatar"; // Custom Property key for avatars

    private void Start()
    {
        if (!PhotonNetwork.IsConnected)
        {
            PhotonNetwork.ConnectUsingSettings();
        }

        if (string.IsNullOrEmpty(PhotonNetwork.NickName))
        {
            PhotonNetwork.NickName = PlayerPrefs.GetString("PlayerName", "Player"); // Load from PlayerPrefs or set default
            Debug.Log($"Player name loaded: {PhotonNetwork.NickName}");
        }

        playerNameText.text = PhotonNetwork.NickName; // Display the player name immediately
        playerReadyButton.SetActive(false);
        opponentReadyButton.SetActive(false);
        playerCheckmark.SetActive(false);
        opponentCheckmark.SetActive(false);
        opponentAvatar.SetActive(false);
        opponentNameText.text = "Waiting for other player...";
        waitPlayerText.SetActive(true);
        nameOfPlayer2.SetActive(false);
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log("Connected to Master Server.");
        PhotonNetwork.JoinLobby();
    }

    public override void OnJoinedLobby()
    {
        Debug.Log("Joined Photon Lobby.");
    }

    public void CreateLobby()
    {
        if (!PhotonNetwork.IsConnectedAndReady)
        {
            Debug.LogError("Not connected to Master Server. Please wait and try again.");
            DisplayInvalidCodeMessage("Connecting to server. Please try again.");
            return;
        }

        string roomCode = GenerateRoomCode();
        roomCodeText.text = roomCode;

        RoomOptions roomOptions = new RoomOptions { MaxPlayers = 2 };
        PhotonNetwork.CreateRoom(roomCode, roomOptions, null);

        playerNameText.text = PhotonNetwork.NickName;
        playerAvatar.SetActive(true);
        opponentNameText.text = "Waiting for opponent...";
    }

    public void EvaluateRoomCode()
    {
        if (!PhotonNetwork.IsConnectedAndReady)
        {
            Debug.LogError("Not connected to Master Server. Please wait and try again.");
            DisplayInvalidCodeMessage("Connecting to server. Please try again.");
            return;
        }

        string roomCode = joinCodeInput.text.Trim();
        if (string.IsNullOrEmpty(roomCode))
        {
            DisplayInvalidCodeMessage("Room Code cannot be empty.");
            return;
        }

        if (PhotonNetwork.InRoom)
        {
            StartCoroutine(LeaveRoomAndJoin(roomCode));
            return;
        }

        Debug.Log($"Attempting to join room with code: {roomCode}");
        PhotonNetwork.JoinRoom(roomCode);
    }

    private IEnumerator LeaveRoomAndJoin(string roomCode)
    {
        PhotonNetwork.LeaveRoom();

        // Wait until the client has left the room and reconnected to the Master Server
        while (PhotonNetwork.InRoom || !PhotonNetwork.IsConnectedAndReady)
        {
            yield return null;
        }

        Debug.Log($"Joining room with code: {roomCode}");
        PhotonNetwork.JoinRoom(roomCode);
    }

    public override void OnLeftRoom()
    {
        Debug.Log("Successfully left the room and returned to the Master Server.");
    }

   public void Quickmatch()
{
    if (!PhotonNetwork.IsConnectedAndReady)
    {
        Debug.LogError("Not connected to Master Server. Please wait and try again.");
        DisplayInvalidCodeMessage("Connecting to server. Please try again.");
        return;
    }

    isQuickMatch = true;
    quickMatchScreen.SetActive(true);
    StartCoroutine(StartQuickMatchTimer());

    Debug.Log("Attempting to join a random room for Quickmatch...");
    PhotonNetwork.JoinRandomRoom();
}

// Called when a random room cannot be found
public override void OnJoinRandomFailed(short returnCode, string message)
{
    Debug.Log($"Quickmatch failed: {message}. Creating a new room...");
    CreateQuickmatchRoom();
}

private void CreateQuickmatchRoom()
{
    string roomName = $"Quickmatch_{Random.Range(1000, 9999)}";
    RoomOptions roomOptions = new RoomOptions
    {
        MaxPlayers = 2,
        IsVisible = true,
        IsOpen = true
    };

    PhotonNetwork.CreateRoom(roomName, roomOptions);
    Debug.Log($"Created Quickmatch room: {roomName}");
}

    public void CopyRoomCode()
{
    string roomCode = roomCodeText.text;
    if (!string.IsNullOrEmpty(roomCode))
    {
        GUIUtility.systemCopyBuffer = roomCode;
        Debug.Log($"Room code copied: {roomCode}");
        roomCodeText.text = "Code copied!";
    }
    else
    {
        roomCodeText.text = "Room code is empty!";
    }
}



    public void BackToMenu()
    {
        if (PhotonNetwork.InRoom)
        {
            PhotonNetwork.LeaveRoom();
        }

        quickMatchScreen.SetActive(false);
        timeoutMessagePanel.SetActive(false);
        currentLobbyUI.SetActive(false);
    }

    private void DisplayInvalidCodeMessage(string message)
    {
        invalidCodeMessage.gameObject.SetActive(true);
        invalidCodeMessage.text = message;
        StartCoroutine(HideInvalidCodeMessage());
    }

    private IEnumerator HideInvalidCodeMessage()
    {
        yield return new WaitForSeconds(10f);
        invalidCodeMessage.gameObject.SetActive(false);
    }

    private string GenerateRoomCode()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        char[] code = new char[6];
        for (int i = 0; i < code.Length; i++)
        {
            code[i] = chars[Random.Range(0, chars.Length)];
        }
        return new string(code);
    }

   public override void OnJoinedRoom()
{
    Debug.Log($"Joined Room: {PhotonNetwork.CurrentRoom.Name}");

    joinCodeLobby.SetActive(false);
    joinLobbyUI.SetActive(true);

    playerNameText.text = PhotonNetwork.NickName;
    playerAvatar.SetActive(true);
    playerReadyButton.SetActive(true);

    if (PhotonNetwork.CurrentRoom.PlayerCount == 2)
    {
        AssignOpponent(PhotonNetwork.PlayerListOthers[0]);
    }
    else
    {
        opponentNameText.text = "Waiting for opponent...";
        opponentAvatar.SetActive(false);
        opponentReadyButton.SetActive(false);
        waitPlayerText.SetActive(true);
        nameOfPlayer2.SetActive(false);
    }
}

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        Debug.Log($"{newPlayer.NickName} joined the room.");
        AssignOpponent(newPlayer);
    }

    private void AssignOpponent(Player opponent)
{
    if (opponent == null)
    {
        Debug.LogError("Cannot assign opponent: Opponent is null.");
        return;
    }

    opponentNameText.text = opponent.NickName;
    opponentAvatar.SetActive(true);
    opponentReadyButton.SetActive(true);

    // Request opponent avatar choice
    photonView.RPC("RequestOpponentAvatar", opponent);
    waitPlayerText.SetActive(false);
    nameOfPlayer2.SetActive(true);
}

[PunRPC]
private void RequestOpponentAvatar(PhotonMessageInfo info)
{
    Debug.Log($"Received request for avatar from {info.Sender.NickName}.");
    if (playerAvatarImage.sprite != null)
    {
        photonView.RPC("UpdateOpponentAvatar", info.Sender, playerAvatarImage.sprite.name);
    }
}

[PunRPC]
private void UpdateOpponentAvatar(string avatarSpriteName)
{
    Debug.Log($"Updating opponent avatar with sprite: {avatarSpriteName}.");
    Sprite avatarSprite = Resources.Load<Sprite>($"Avatars/{avatarSpriteName}"); // Ensure avatars are in a "Resources/Avatars" folder
    if (avatarSprite != null)
    {
        opponentAvatarImage.sprite = avatarSprite;
    }
    else
    {
        Debug.LogWarning($"Avatar sprite {avatarSpriteName} not found in Resources/Avatars.");
    }
}


    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        Debug.Log($"{otherPlayer.NickName} left the room.");
        opponentNameText.text = "Waiting for opponent...";
        opponentAvatar.SetActive(false);
        opponentReadyButton.SetActive(false);
        playerCheckmark.SetActive(false);
        opponentCheckmark.SetActive(false);
        waitPlayerText.SetActive(true);
        nameOfPlayer2.SetActive(false);
    }

    public void OnPlayerReady()
    {
        if (playerCheckmark == null)
        {
            Debug.LogError("Player checkmark is not assigned in the Inspector!");
            return;
        }

        if (photonView == null)
        {
            Debug.LogError("PhotonView is not assigned!");
            return;
        }

        isPlayerReady = true;
        playerCheckmark.SetActive(true);
        Debug.Log("Player is ready. Setting checkmark visible.");

        photonView.RPC("SetOpponentReady", RpcTarget.Others);
        CheckGameStart();
    }

    [PunRPC]
    private void SetOpponentReady()
    {
        if (opponentCheckmark == null)
        {
            Debug.LogError("Opponent checkmark is not assigned in the Inspector!");
            return;
        }

        isOpponentReady = true;
        opponentCheckmark.SetActive(true);
        Debug.Log("Opponent is ready. Setting opponent's checkmark visible.");

        CheckGameStart();
    }

    private void CheckGameStart()
    {
        if (isPlayerReady && isOpponentReady)
        {
            Debug.Log("Both players are ready. Loading game scene...");
            PhotonNetwork.LoadLevel(gameSceneName);
        }
    }

    private IEnumerator StartQuickMatchTimer()
    {
        float timeRemaining = quickMatchTime;
        while (timeRemaining > 0)
        {
            quickMatchTimerText.text = Mathf.CeilToInt(timeRemaining).ToString();
            yield return new WaitForSeconds(1f);
            timeRemaining--;
        }

        ShowTimeoutMessage();
    }

    private void ShowTimeoutMessage()
    {
        timeoutMessagePanel.SetActive(true);
        timeoutMessageText.text = "We can't find another player right now. Do you want to try again or play by yourself?";
    }

    public void TryAgain()
    {
        timeoutMessagePanel.SetActive(false);
        StartCoroutine(StartQuickMatchTimer());
        PhotonNetwork.JoinRandomRoom();
    }

    public void OvertimePlayWithAI()
    {
        Debug.Log("Switching to play with AI...");
    }

    /// <summary>
    /// Call this from a UI button to save the current multiplayer avatar to PlayerPrefs for single player use.
    /// </summary>
    public void SaveCurrentAvatarToPlayerPrefs()
    {
        string avatarName = "";
        if (PhotonNetwork.LocalPlayer != null && PhotonNetwork.LocalPlayer.CustomProperties != null
            && PhotonNetwork.LocalPlayer.CustomProperties.ContainsKey(AvatarKey))
        {
            avatarName = PhotonNetwork.LocalPlayer.CustomProperties[AvatarKey] as string;
        }
        if (!string.IsNullOrEmpty(avatarName))
        {
            PlayerPrefs.SetString("Avatar", avatarName);
            PlayerPrefs.Save();
            Debug.Log($"Saved avatar '{avatarName}' to PlayerPrefs for single player.");
        }
        else
        {
            Debug.LogWarning("No avatar found in Photon custom properties to save to PlayerPrefs.");
        }
    }

    // filepath: /C:/Users/ilias/PRAISE/Assets/Scripts/Multiplayer/MultiplayerManager.cs
public void SetPlayerAvatar(string avatarName)
{
    ExitGames.Client.Photon.Hashtable customProperties = new ExitGames.Client.Photon.Hashtable();
    customProperties[AvatarKey] = avatarName;
    PhotonNetwork.LocalPlayer.SetCustomProperties(customProperties);
}

// Example method to be called when the player selects an avatar
public void OnAvatarSelected(string avatarName)
{
    SetPlayerAvatar(avatarName);
}

    /// <summary>
    /// Call this from a UI button to save the currently assigned sprite in singlePlayerAvatarImage to PlayerPrefs for single player use.
    /// </summary>
    public void SaveAssignedImageToPlayerPrefs()
    {
        if (singlePlayerAvatarImage == null || singlePlayerAvatarImage.sprite == null)
        {
            Debug.LogWarning("Single player avatar image or sprite is not assigned.");
            return;
        }

        string avatarName = singlePlayerAvatarImage.sprite.name;
        if (!string.IsNullOrEmpty(avatarName))
        {
            PlayerPrefs.SetString("Avatar", avatarName);
            PlayerPrefs.Save();
            Debug.Log($"Saved avatar '{avatarName}' from assigned image to PlayerPrefs for single player.");
        }
        else
        {
            Debug.LogWarning("Assigned image sprite has no name, cannot save to PlayerPrefs.");
        }
    }

}
