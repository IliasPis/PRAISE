using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;

public class FinalScore : MonoBehaviour
{
    [Header("UI Elements")]
    public Image winnerAvatar; // Image to display the winner's avatar
    public TextMeshProUGUI winnerNameText; // Text to display the winner's name
    public TextMeshProUGUI timePlayedText; // Text to display the time played
    public TextMeshProUGUI scoreText; // Text to display the winner's score
    public TextMeshProUGUI totalQuestionsText; // Text to display the total questions answered

    private const string AvatarKey = "Avatar";
    private const string Player1ScoreKey = "Player1Score";
    private const string Player2ScoreKey = "Player2Score";
    private const string TotalQuestionsKey = "TotalQuestions"; // Custom Property key for total questions answered
    private const string TimePlayedKey = "TimePlayed"; // Custom Property key for time played

    private float syncedStartTime = -1f;
    private float displayTimer = 0f;

    void Start()
    {
        // Synchronize start time using Photon room custom properties (for both players)
        if (PhotonNetwork.InRoom)
        {
            if (PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey("FinalScoreStartTime"))
            {
                syncedStartTime = (float)PhotonNetwork.CurrentRoom.CustomProperties["FinalScoreStartTime"];
            }
            else if (PhotonNetwork.IsMasterClient)
            {
                syncedStartTime = (float)PhotonNetwork.Time;
                ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable();
                props["FinalScoreStartTime"] = syncedStartTime;
                PhotonNetwork.CurrentRoom.SetCustomProperties(props);
            }
        }

        // If not in room or property not set, fallback to local timer
        if (syncedStartTime < 0f)
        {
            syncedStartTime = Time.time;
        }

        DisplayWinnerInfo();
    }

    void Update()
    {
        // Always update timer for both players
        float timerValue = 0f;
        if (PhotonNetwork.InRoom && PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey("FinalScoreStartTime"))
        {
            float startTime = (float)PhotonNetwork.CurrentRoom.CustomProperties["FinalScoreStartTime"];
            timerValue = (float)(PhotonNetwork.Time - startTime);
        }
        else
        {
            timerValue = Time.time - syncedStartTime;
        }

        int hours = Mathf.FloorToInt(timerValue / 3600);
        int minutes = Mathf.FloorToInt((timerValue % 3600) / 60);
        int seconds = Mathf.FloorToInt(timerValue % 60);
        if (timePlayedText != null)
            timePlayedText.text = $"{hours:D2}:{minutes:D2}:{seconds:D2}";
    }

    void DisplayWinnerInfo()
    {
        // Read scores from PlayerPrefs
        int player1Score = PlayerPrefs.GetInt(Player1ScoreKey, 0);
        int player2Score = PlayerPrefs.GetInt(Player2ScoreKey, 0);

        string winnerName = "";
        Sprite winnerAvatarSprite = null;

        if (PhotonNetwork.PlayerList.Length > 0)
        {
            string player1Name = PhotonNetwork.PlayerList[0].NickName;
            Sprite player1AvatarSprite = GetAvatarSpriteFromCustomProperties(PhotonNetwork.PlayerList[0]);
            string player2Name = PhotonNetwork.PlayerList.Length > 1 ? PhotonNetwork.PlayerList[1].NickName : "";
            Sprite player2AvatarSprite = PhotonNetwork.PlayerList.Length > 1 ? GetAvatarSpriteFromCustomProperties(PhotonNetwork.PlayerList[1]) : null;

            if (player1Score > player2Score)
            {
                winnerName = player1Name;
                winnerAvatarSprite = player1AvatarSprite;
                scoreText.text = player1Score.ToString();
            }
            else if (player2Score > player1Score)
            {
                winnerName = player2Name;
                winnerAvatarSprite = player2AvatarSprite;
                scoreText.text = player2Score.ToString();
            }
            else // Tie
            {
                winnerName = "It's a Tie!";
                winnerAvatarSprite = null;
                scoreText.text = player1Score.ToString();
            }
        }
        else
        {
            winnerName = "Unknown";
            winnerAvatarSprite = null;
            scoreText.text = "0";
        }

        winnerNameText.text = winnerName;
        winnerAvatar.sprite = winnerAvatarSprite;

        // --- Get total questions from QuizManager and set it in the assigned text ---
        QuizManager quizManager = FindObjectOfType<QuizManager>();
        if (quizManager != null)
        {
            var totalQuestionsField = quizManager.GetType().GetField("totalQuestions", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (totalQuestionsField != null && totalQuestionsText != null)
            {
                int totalQuestions = (int)totalQuestionsField.GetValue(quizManager);
                totalQuestionsText.text = totalQuestions.ToString();
            }
        }
        // Do NOT set timePlayedText here, it is handled in Update() for synchronization
    }

    Sprite GetAvatarSpriteFromCustomProperties(Player player)
    {
        if (player.CustomProperties.TryGetValue(AvatarKey, out object avatar))
        {
            string avatarName = avatar as string;
            Sprite avatarSprite = Resources.Load<Sprite>($"Avatars/{avatarName}");
            return avatarSprite;
        }
        return null;
    }

    // Helper methods to access private fields
    private int GetPrivateIntField(object obj, string fieldName)
    {
        var field = obj.GetType().GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null)
            return (int)field.GetValue(obj);
        return 0;
    }

    private float GetPrivateFloatField(object obj, string fieldName)
    {
        var field = obj.GetType().GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null)
            return (float)field.GetValue(obj);
        return 0f;
    }
}