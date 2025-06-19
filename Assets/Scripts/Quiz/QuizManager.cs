using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;
using TMPro; // For TextMeshPro
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class QuizManager : MonoBehaviourPunCallbacks
{
    [Header("UI Elements")]
    public GameObject answeringPanel; // Panel for the answering player
    public GameObject waitingPanel; // Panel for the waiting player
    public TextMeshProUGUI questionText; // Question Text
    public TextMeshProUGUI timerText; // Timer Display
    public TextMeshProUGUI messageText; // Message Display
    public TextMeshProUGUI player1ScoreText; // Player 1 Score Text
    public TextMeshProUGUI player2ScoreText; // Player 2 Score Text
    public TextMeshProUGUI totalTimeText; // Total Time Played
    public Button[] answerButtons; // Array of Answer Buttons
    public Button skipButton; // Button to skip the question
    public Button resetGameButton; // Button to reset the game
    public Button exitButton; // Button to exit to the reset scene
    public GameObject player1Shadow; // Shadow for Player 1
    public GameObject player2Shadow; // Shadow for Player 2
    public Image player1Avatar; // Avatar for Player 1
    public Image player2Avatar; // Avatar for Player 2
    public TextMeshProUGUI player1NameText; // Name Text for Player 1
    public TextMeshProUGUI player2NameText; // Name Text for Player 2
    public GameObject finalScorePanel; // Final score panel
    public GameObject quizPanel; // Quiz panel to deactivate
    public GameObject WaitingLobbyPanel;
    public GameObject playerWinnerPanel; // Single winner panel to show at end
    public GameObject playerWaitingForOpponentPanel; // Assign in inspector: panel to show when waiting for opponent
    public GameObject playerLeftGameObject; // GameObject to show when a player leaves
    public TextMeshProUGUI questionsLeftText; // Text to display questions left
    public GameObject instructionsBeforePlay; // Instructions before play

    [Header("Quiz Data")]
    public LanguageCategories[] languageCategories; // Categories organized by language
    private int currentQuestionIndex = 0;
    private int questionsAnsweredInTurn = 0;
    private Dictionary<string, int> categoryQuestionTracker;

    [Header("Game Settings")]
    public int questionsPerCategory = 2; // Minimum questions per category per game
    public float timerDuration = 30f;
    public string selectedLanguage = "EN"; // Default language
    public int questionsBeforeSwitch = 2; // Questions before switching turn (adjustable in Editor)
    public int totalQuestions = 40; // Total questions before the game ends
    public float maxGameTime = 1200f; // Maximum game time in seconds (20 minutes by default)
    public string sceneToLoadOnReset = "MainMenu"; // Scene to load on game reset
    public float instructionsDisplayTime = 6f; // Time to display instructions before play

    [Header("Sounds")]
    public AudioSource buttonClickAudioSource; // AudioSource for button click sound
    public AudioSource turnChangeAudioSource; // AudioSource for turn change sound
    public AudioSource finalSecondsAudioSource; // AudioSource for final seconds sound
    public AudioSource correctAnswerAudioSource; // AudioSource for correct answer sound
    public AudioSource wrongAnswerAudioSource; // AudioSource for wrong answer sound

    [Header("Button Colors")]
    public Color correctAnswerColor = Color.green; // Color for correct answer
    public Color wrongAnswerColor = Color.red; // Color for wrong answer
    public float colorDisplayDuration = 2f; // Duration to display the color

    [Header("Color Dice Mechanic")]
    public Button rollDiceButton; // Assign in editor
    public GameObject dicePanel; // Panel containing dice UI
    public GameObject[] diceFaceImages; // 6 images for dice faces (Green, Red, Yellow, Black, Purple, Orange)
    public GameObject diceBackground; // Assign in editor: background image for dice roll

    private readonly string[] diceColors = { "Green", "Red", "Yellow", "Black", "Purple", "Orange" };
    private Dictionary<string, bool> colorActive; // color -> isActive
    private string lastRolledColor = null;

    private float timer;
    private float totalTime = 0f; // Total time played
    private bool isPlayer1Turn = true;
    private bool isAnswering = false;
    private bool finalSecondsPlaying = false;
    private bool isRollingDice = false;

    private int player1Score = 0;
    private int player2Score = 0;
    private int totalQuestionsAnswered = 0; // Added definition for totalQuestionsAnswered

    private const string AvatarKey = "Avatar"; // Custom Property key for avatars
    private const byte UpdateScoreEventCode = 2; // Custom Photon event code for updating scores
    private bool localPlayerFinished = false;
    private bool remotePlayerFinished = false;
    private const byte PlayerFinishedEventCode = 10; // Custom Photon event code for player finished

    void Start()
    {
        // Ensure the correct language is loaded on start
        SetLanguage(LanguageManager.SelectedLanguage);

        if (PhotonNetwork.IsMasterClient)
        {
            isPlayer1Turn = true;
        }

        // Initialize category tracker
        InitializeCategoryTracker();

        // Initialize UI and Timer
        timer = timerDuration;
        UpdatePanels();
        InitializeAvatarsAndNames();

        // Add skip button functionality
        skipButton.onClick.AddListener(SkipQuestion);
        resetGameButton.onClick.AddListener(ResetGame);
        exitButton.onClick.AddListener(ExitToResetScene); // Add exit button functionality

        // Register Photon event
        PhotonNetwork.NetworkingClient.EventReceived += OnEvent;

        // Display instructions before play
        StartCoroutine(DisplayInstructionsAndStartGame());

        InitializeCategoryColorMapping();

        rollDiceButton.onClick.AddListener(RollDice);

        dicePanel.SetActive(true);
        rollDiceButton.gameObject.SetActive(true);
        ShowDiceFace(-1); // Hide all faces initially
    }

    IEnumerator DisplayInstructionsAndStartGame()
    {
        // --- Ensure the instructions panel is enabled before showing ---
        if (instructionsBeforePlay != null)
            instructionsBeforePlay.SetActive(true);

        yield return new WaitForSeconds(instructionsDisplayTime);

        if (instructionsBeforePlay != null)
            instructionsBeforePlay.SetActive(false);

        LoadQuestion();
    }

    void OnDestroy()
    {
        PhotonNetwork.NetworkingClient.EventReceived -= OnEvent;
    }

    void Update()
    {
        if (!finalScorePanel.activeSelf) // If the game is not over
        {
            totalTime += Time.deltaTime; // Update total time played

            if (!isAnswering)
            {
                timer -= Time.deltaTime;
                if (timer <= 10f && !finalSecondsPlaying)
                {
                    finalSecondsPlaying = true;
                    PlaySound(finalSecondsAudioSource, true); // Loop the final seconds sound
                }

                if (timer <= 0f)
                {
                    OnTimeOver();
                }
            }

            UpdateTotalTimeText();
            UpdateTimerUI();

            if (totalTime >= maxGameTime || totalQuestionsAnswered >= totalQuestions)
            {
                EndGame();
            }
        }
        // DO NOT set playerWaitingForOpponentPanel.SetActive(false) here!
    }

    void InitializeCategoryTracker()
    {
        categoryQuestionTracker = new Dictionary<string, int>();
        foreach (var category in languageCategories)
        {
            foreach (var cat in category.categories)
            {
                categoryQuestionTracker[cat.name] = 0;
            }
        }
    }

    void InitializeCategoryColorMapping()
    {
        // Only keep colorActive initialization
        colorActive = new Dictionary<string, bool>();
        foreach (var color in diceColors)
            colorActive[color] = true;
    }

    void ShowDiceFace(int index)
    {
        for (int i = 0; i < diceFaceImages.Length; i++)
            diceFaceImages[i].SetActive(i == index);
    }

    void RollDice()
    {
        if (isRollingDice) return;
        rollDiceButton.gameObject.SetActive(false); // Hide button immediately
        StartCoroutine(RollDiceAnimation());
    }

    IEnumerator RollDiceAnimation()
    {
        isRollingDice = true;
        // rollDiceButton.interactable = false; // No need, button is hidden

        int finalIdx = -1;
        List<int> activeColorIndices = new List<int>();
        for (int i = 0; i < diceColors.Length; i++)
            if (colorActive[diceColors[i]]) activeColorIndices.Add(i);

        if (activeColorIndices.Count == 0)
        {
            messageText.text = "No categories/colors are active!";
            isRollingDice = false;
            rollDiceButton.gameObject.SetActive(true); // Show button again for retry
            yield break;
        }

        float t = 0f;
        float duration = 1.5f;
        float interval = 0.05f;
        int shownIdx = 0;

        // Fast roll
        while (t < duration)
        {
            shownIdx = activeColorIndices[Random.Range(0, activeColorIndices.Count)];
            ShowDiceFace(shownIdx);
            yield return new WaitForSeconds(interval);
            t += interval;
            interval *= 1.08f; // Gradually slow down
        }

        // Final result
        finalIdx = activeColorIndices[Random.Range(0, activeColorIndices.Count)];
        ShowDiceFace(finalIdx);
        lastRolledColor = diceColors[finalIdx];

        yield return new WaitForSeconds(0.5f);

        isRollingDice = false;
        dicePanel.SetActive(false);

        // Now load question for this color
        LoadQuestionForColor(lastRolledColor);
    }

    // Modified: Only called after dice roll
    void LoadQuestion()
    {
        // Instead of loading directly, require dice roll first
        dicePanel.SetActive(true);
        if (diceBackground != null) diceBackground.SetActive(true);
        rollDiceButton.gameObject.SetActive(true); // Ensure button is visible for each new roll
        rollDiceButton.interactable = true;
        ShowDiceFace(-1);
        messageText.text = "Roll the dice to select a category!";

        // Clear question and answers
        questionText.text = "";
        foreach (var btn in answerButtons)
        {
            btn.gameObject.SetActive(false);
        }
    }

    void LoadQuestionForColor(string color)
    {
        isAnswering = false;
        finalSecondsPlaying = false;
        StopSound(finalSecondsAudioSource);

        if (diceBackground != null) diceBackground.SetActive(false);

        LanguageCategories languageCategory = System.Array.Find(languageCategories, lang => lang.language == selectedLanguage);
        if (languageCategory == null)
        {
            Debug.LogError($"No data available for language: {selectedLanguage}.");
            return;
        }

        // Convert rolled color string to CategoryColor enum
        if (!System.Enum.TryParse<CategoryColor>(color, true, out var rolledColorEnum))
        {
            Debug.LogError($"Could not parse dice color '{color}' to CategoryColor enum.");
            messageText.text = $"Error: Invalid color '{color}'";
            return;
        }

        // Directly check category.color for each category in the selected language
        List<Category> colorCategories = new List<Category>();
        foreach (var category in languageCategory.categories)
        {
            if (category.color == rolledColorEnum)
                colorCategories.Add(category);
        }

        List<Category> available = new List<Category>();
        foreach (var cat in colorCategories)
        {
            if (!cat.IsExhausted())
                available.Add(cat);
        }

        if (available.Count == 0)
        {
            messageText.text = $"No questions left for {color} category!";
            dicePanel.SetActive(true);
            if (diceBackground != null) diceBackground.SetActive(true);
            rollDiceButton.gameObject.SetActive(true); // Ensure button is visible for reroll
            rollDiceButton.interactable = true;
            questionText.text = "";
            foreach (var btn in answerButtons)
            {
                btn.gameObject.SetActive(false);
            }
            return;
        }

        Category chosenCategory = available[Random.Range(0, available.Count)];
        var question = chosenCategory.GetNextQuestion();

        questionText.text = question.text;
        currentQuestionIndex++;

        for (int i = 0; i < answerButtons.Length; i++)
        {
            if (i < question.answers.Length)
            {
                answerButtons[i].GetComponentInChildren<TextMeshProUGUI>().text = question.answers[i];
                answerButtons[i].onClick.RemoveAllListeners();
                int answerIndex = i;
                answerButtons[i].onClick.AddListener(() => CheckAnswer(answerIndex, question.correctAnswer));
                answerButtons[i].gameObject.SetActive(true);
            }
            else
            {
                answerButtons[i].gameObject.SetActive(false);
            }
        }

        timer = timerDuration;
        messageText.text = "";
        UpdateTimerForAllPlayers(timer);

        questionsLeftText.text = $"Questions Left: {totalQuestions - totalQuestionsAnswered}";
    }

    void CheckAnswer(int chosenIndex, int correctIndex)
    {
        if (isAnswering) return;
        isAnswering = true;
        Button selectedButton = answerButtons[chosenIndex];

        if (chosenIndex == correctIndex)
        {
            messageText.text = "Correct Answer!";
            selectedButton.image.color = correctAnswerColor;
            PlaySound(correctAnswerAudioSource);
            if (isPlayer1Turn) player1Score++;
            else player2Score++;
            UpdateScores();

            // Send score update to the other player
            RaiseScoreEvent();
        }
        else
        {
            messageText.text = "Wrong Answer!";
            selectedButton.image.color = wrongAnswerColor;
            PlaySound(wrongAnswerAudioSource);
            answerButtons[correctIndex].image.color = correctAnswerColor; // Highlight correct answer
        }

        questionsAnsweredInTurn++;
        totalQuestionsAnswered++;
        if (questionsAnsweredInTurn >= questionsBeforeSwitch)
        {
            questionsAnsweredInTurn = 0;
            Invoke(nameof(SwitchTurn), 3f); // Delay switch turn by 3 seconds
        }
        else
        {
            Invoke(nameof(PrepareNextQuestion), colorDisplayDuration);
        }

        StartCoroutine(ResetButtonColors());
    }

    IEnumerator ResetButtonColors()
    {
        yield return new WaitForSeconds(colorDisplayDuration);
        foreach (Button btn in answerButtons)
        {
            btn.image.color = Color.white; // Reset button color
        }
    }

    void OnTimeOver()
    {
        messageText.text = "Time Over! Question Passed.";
        StopSound(finalSecondsAudioSource); // Stop any looping sounds
        questionsAnsweredInTurn++;
        totalQuestionsAnswered++;
        if (questionsAnsweredInTurn >= questionsBeforeSwitch)
        {
            questionsAnsweredInTurn = 0;
            Invoke(nameof(SwitchTurn), 3f); // Delay switch turn by 3 seconds
        }
        else
        {
            PrepareNextQuestion();
        }
    }

    void SkipQuestion()
    {
        messageText.text = "Question Skipped!";
        StopSound(finalSecondsAudioSource); // Stop any looping sounds
        questionsAnsweredInTurn++;
        totalQuestionsAnswered++;
        if (questionsAnsweredInTurn >= questionsBeforeSwitch)
        {
            questionsAnsweredInTurn = 0;
            Invoke(nameof(SwitchTurn), 3f); // Delay switch turn by 3 seconds
        }
        else
        {
            PrepareNextQuestion();
        }
    }

    void PrepareNextQuestion()
    {
        // Require dice roll before next question
        // Clear question and answers before dice roll
        questionText.text = "";
        foreach (var btn in answerButtons)
        {
            btn.gameObject.SetActive(false);
        }
        LoadQuestion();
    }

    void UpdateTimerUI()
    {
        timerText.text = $"Time Left: {Mathf.Ceil(timer)}";
    }

    void UpdateTotalTimeText()
    {
        int hours = Mathf.FloorToInt(totalTime / 3600);
        int minutes = Mathf.FloorToInt((totalTime % 3600) / 60);
        int seconds = Mathf.FloorToInt(totalTime % 60);
        totalTimeText.text = $"{hours:D2}:{minutes:D2}:{seconds:D2}sec";
    }

    void UpdateScores()
    {
        player1ScoreText.text = player1Score.ToString();
        player2ScoreText.text = player2Score.ToString();
    }

    void UpdatePanels()
    {
        bool isCurrentPlayer = PhotonNetwork.IsMasterClient == isPlayer1Turn;

        answeringPanel.SetActive(isCurrentPlayer);
        waitingPanel.SetActive(!isCurrentPlayer);

        player1Shadow.SetActive(!isPlayer1Turn);
        player2Shadow.SetActive(isPlayer1Turn);

        foreach (Button btn in answerButtons)
        {
            btn.interactable = isCurrentPlayer;
        }

        skipButton.interactable = isCurrentPlayer;
    }

    void SwitchTurn()
    {
        isPlayer1Turn = !isPlayer1Turn;
        PlaySound(turnChangeAudioSource);

        PhotonNetwork.RaiseEvent(1, isPlayer1Turn, RaiseEventOptions.Default, SendOptions.SendReliable);
        UpdatePanels();
        PrepareNextQuestion();
    }

    private void RaiseScoreEvent()
    {
        object[] data = { player1Score, player2Score };
        PhotonNetwork.RaiseEvent(UpdateScoreEventCode, data, RaiseEventOptions.Default, SendOptions.SendReliable);
    }

    private void OnEvent(EventData photonEvent)
    {
        if (photonEvent.Code == 1)
        {
            isPlayer1Turn = (bool)photonEvent.CustomData;
            UpdatePanels();
        }
        else if (photonEvent.Code == UpdateScoreEventCode)
        {
            object[] data = (object[])photonEvent.CustomData;
            player1Score = (int)data[0];
            player2Score = (int)data[1];
            UpdateScores();
        }
        else if (photonEvent.Code == 3) // Timer update event
        {
            timer = (float)photonEvent.CustomData;
            UpdateTimerUI();
        }
        else if (photonEvent.Code == PlayerFinishedEventCode)
        {
            remotePlayerFinished = true;
            // If local player is also finished, show final panel
            if (localPlayerFinished)
            {
                ShowFinalScorePanel();
            }
        }
    }

    private void UpdateTimerForAllPlayers(float newTimer)
    {
        PhotonNetwork.RaiseEvent(3, newTimer, RaiseEventOptions.Default, SendOptions.SendReliable);
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        playerLeftGameObject.SetActive(true);
        StartCoroutine(HandlePlayerLeft());
    }

    private IEnumerator HandlePlayerLeft()
    {
        yield return new WaitForSeconds(7f);
        ResetGame();
    }

    void InitializeAvatarsAndNames()
    {
        if (PhotonNetwork.PlayerList.Length > 0)
        {
            player1Avatar.sprite = GetAvatarSpriteFromCustomProperties(PhotonNetwork.PlayerList[0]);
            player1NameText.text = PhotonNetwork.PlayerList[0].NickName;

            if (PhotonNetwork.PlayerList.Length > 1)
            {
                player2Avatar.sprite = GetAvatarSpriteFromCustomProperties(PhotonNetwork.PlayerList[1]);
                player2NameText.text = PhotonNetwork.PlayerList[1].NickName;
            }
        }
    }

    Sprite GetAvatarSpriteFromCustomProperties(Player player)
    {
        if (player.CustomProperties.TryGetValue(AvatarKey, out object avatar))
        {
            string avatarName = avatar as string;
            return Resources.Load<Sprite>($"Avatars/{avatarName}"); // Ensure sprites are stored in a Resources/Avatars folder
        }
        return null; // Default sprite if not set
    }

    void PlaySound(AudioSource audioSource, bool loop = false)
    {
        if (audioSource)
        {
            audioSource.loop = loop;
            audioSource.Play();
        }
    }

    void StopSound(AudioSource audioSource)
    {
        if (audioSource)
        {
            audioSource.loop = false;
            audioSource.Stop();
        }
    }

    void EndGame()
    {
        // Save scores for FinalScore.cs to access
        PlayerPrefs.SetInt("Player1Score", player1Score);
        PlayerPrefs.SetInt("Player2Score", player2Score);
        PlayerPrefs.Save();

        localPlayerFinished = true;

        // Notify the other player that this player has finished
        PhotonNetwork.RaiseEvent(PlayerFinishedEventCode, null, RaiseEventOptions.Default, SendOptions.SendReliable);

        // Hide all panels except the waiting panel if the other player hasn't finished
        if (!remotePlayerFinished)
        {
            if (finalScorePanel != null)
                finalScorePanel.SetActive(false);
            if (playerWinnerPanel != null)
                playerWinnerPanel.SetActive(false);
            if (quizPanel != null)
                quizPanel.SetActive(false);
            // REMOVE or COMMENT OUT the next line:
            // if (WaitingLobbyPanel != null)
            //     WaitingLobbyPanel.SetActive(false);

            if (answeringPanel != null)
                answeringPanel.SetActive(false);
            if (waitingPanel != null)
                waitingPanel.SetActive(false);

            if (playerWaitingForOpponentPanel != null)
                playerWaitingForOpponentPanel.SetActive(true);

            StopSound(finalSecondsAudioSource);
            return;
        }

        // Both players finished, show final panel
        ShowFinalScorePanel();
    }

    private void ShowFinalScorePanel()
    {
        finalScorePanel.SetActive(true);
        quizPanel.SetActive(false);
        // Only disable WaitingLobbyPanel here, when both are finished:
        if (WaitingLobbyPanel != null)
            WaitingLobbyPanel.SetActive(false);
        if (playerWaitingForOpponentPanel != null)
            playerWaitingForOpponentPanel.SetActive(false);

        // --- CRITICAL: Also hide answering/waiting panels here ---
        if (answeringPanel != null)
            answeringPanel.SetActive(false);
        if (waitingPanel != null)
            waitingPanel.SetActive(false);
        // --------------------------------------------------------

        StopSound(finalSecondsAudioSource);
        if (playerWinnerPanel != null)
            playerWinnerPanel.SetActive(true);
    }

    void ResetGame()
    {
        PhotonNetwork.LeaveRoom();
        UnityEngine.SceneManagement.SceneManager.LoadScene(sceneToLoadOnReset);
    }

    void ExitToResetScene()
    {
        PhotonNetwork.LeaveRoom();
        UnityEngine.SceneManagement.SceneManager.LoadScene(sceneToLoadOnReset);
    }

    public void SetLanguage(string languageName)
    {
        selectedLanguage = languageName;
        InitializeCategoryTracker();
        InitializeCategoryColorMapping();
        ResetAllCategoryQuestionIndices(); // Reset question indices for all categories
        LoadQuestion();
    }

    void ResetAllCategoryQuestionIndices()
    {
        foreach (var lang in languageCategories)
        {
            foreach (var cat in lang.categories)
            {
                cat.ResetQuestionIndex();
            }
        }
    }
}

[System.Serializable]
public class LanguageCategories
{
    public string language; // Language code (e.g., "EN", "SW", "IT", "GR", "RO")
    public Category[] categories; // Categories for the language
}

[System.Serializable]
public class Category
{
    public string name; // Category name
    public CategoryColor color; // Select in Inspector
    public Question[] questions; // Array of questions in the category
    private int currentQuestionIndex = 0;

    public Question GetNextQuestion()
    {
        if (currentQuestionIndex < questions.Length)
        {
            return questions[currentQuestionIndex++];
        }
        return null; // No more questions
    }

    public void ResetQuestionIndex()
    {
        currentQuestionIndex = 0;
    }

    public bool IsExhausted()
    {
        return currentQuestionIndex >= questions.Length;
    }
}

[System.Serializable]
public class Question
{
    public string text; // Question text
    public string[] answers; // Array of answers
    public int correctAnswer; // Index of the correct answer
}

public enum CategoryColor
{
    Green,
    Red,
    Yellow,
    Black,
    Purple,
    Orange
}