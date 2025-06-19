using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro; // For TextMeshPro
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace SinglePlayer
{
    public class SinglePlayerQuizManager : MonoBehaviour
    {
        [Header("UI Elements")]
        public TextMeshProUGUI questionText; // Question Text
        public TextMeshProUGUI timerText; // Timer Display
        public TextMeshProUGUI messageText; // Message Display
        public TextMeshProUGUI scoreText; // Player Score Text
        public TextMeshProUGUI totalTimeText; // Total Time Played
        public TextMeshProUGUI questionsLeftText; // Questions Left Text
        public Button[] answerButtons; // Array of Answer Buttons
        public Button skipButton; // Button to skip the question
        public Button resetGameButton; // Button to reset the game
        public Button exitButton; // Button to exit to the reset scene
        public GameObject finalScorePanel; // Final score panel
        public GameObject instructionsBeforePlay; // Instructions before play
        public Image playerAvatar; // Player's avatar
        public TextMeshProUGUI playerNameText; // Player's name
        public GameObject quizPanel; // Quiz panel to deactivate
        public TextMeshProUGUI finalScoreText; // Final score text
        public Button pauseButton; // Button to pause the game
        public Button continueButton; // Button to continue the game
        public GameObject pausePanel; // Pause panel to display when the game is paused

        [Header("Quiz Data")]
        public LanguageCategories[] languageCategories; // Categories organized by language
        private int currentQuestionIndex = 0;
        private Dictionary<string, int> categoryQuestionTracker;
        private List<Question> allQuestions; // List to store all questions

        [Header("Game Settings")]
        public int questionsPerCategory = 2; // Minimum questions per category per game
        public float timerDuration = 30f;
        public string selectedLanguage = "EN"; // Default language
        public int totalQuestions = 40; // Total questions before the game ends
        public float maxGameTimeMinutes = 20f; // Maximum game time in minutes
        public string sceneToLoadOnReset = "MainMenu"; // Scene to load on game reset
        public float instructionsDisplayTime = 6f; // Time to display instructions before play

        [Header("Sounds")]
        public AudioSource buttonClickAudioSource; // AudioSource for button click sound
        public AudioSource finalSecondsAudioSource; // AudioSource for final seconds sound
        public AudioSource correctAnswerAudioSource; // AudioSource for correct answer sound
        public AudioSource wrongAnswerAudioSource; // AudioSource for wrong answer sound

        [Header("Answer Colors")]
        public Color correctAnswerColor = Color.green; // Color for correct answer
        public Color wrongAnswerColor = Color.red; // Color for wrong answer
        public float answerColorDuration = 2f; // Duration to display answer color

        [Header("Color Dice Mechanic")]
        public Button rollDiceButton; // Assign in editor
        public GameObject dicePanel; // Panel containing dice UI
        public GameObject[] diceFaceImages; // 6 images for dice faces (Green, Red, Yellow, Black, Purple, Orange)
        public GameObject diceBackground; // Assign in editor: background image for dice roll

        private readonly string[] diceColors = { "Green", "Red", "Yellow", "Black", "Purple", "Orange" };
        private Dictionary<string, bool> colorActive; // color -> isActive
        private string lastRolledColor = null;
        private bool isRollingDice = false;

        private float timer;
        private float totalTime = 0f; // Total time played
        private bool isAnswering = false;
        private bool finalSecondsPlaying = false;
        private bool isPaused = false;
        private float pausedTimer;
        private float pausedTotalTime;

        private int playerScore = 0;
        private int totalQuestionsAnswered = 0;

        private const string AvatarKey = "Avatar"; // Custom Property key for avatars
        private const string PlayerNameKey = "PlayerName"; // Custom Property key for player name

        private float timePlayed = 0f; // Track time played from 0

        void Start()
        {
            // Ensure the correct language is loaded on start
            SetLanguage(LanguageManager.SelectedLanguage);

            // Initialize category tracker
            InitializeCategoryTracker();

            // Initialize UI and Timer
            timer = timerDuration;
            totalTime = maxGameTimeMinutes * 60f; // Convert minutes to seconds
            LoadAllQuestions();
            InitializePlayerInfo();

            // Add skip button functionality
            skipButton.onClick.AddListener(OnSkipQuestion);
            resetGameButton.onClick.AddListener(OnResetGame);
            exitButton.onClick.AddListener(OnExitToResetScene); // Add exit button functionality
            pauseButton.onClick.AddListener(OnPauseGame);
            continueButton.onClick.AddListener(OnContinueGame);

            // Initialize category color mapping
            InitializeCategoryColorMapping();

            rollDiceButton.onClick.AddListener(RollDice);

            dicePanel.SetActive(false);
            rollDiceButton.gameObject.SetActive(true);
            ShowDiceFace(-1); // Hide all faces initially

            // Show instructions before starting the game
            StartCoroutine(ShowInstructionsAndStartGame());
        }

        IEnumerator ShowInstructionsAndStartGame()
        {
            instructionsBeforePlay.SetActive(true);
            yield return new WaitForSeconds(instructionsDisplayTime);
            instructionsBeforePlay.SetActive(false);
            ShowDiceAndRoll();
        }

        void Update()
        {
            if (!finalScorePanel.activeSelf && !isPaused) // If the game is not over and not paused
            {
                // Track time played from 0 upwards
                timePlayed += Time.deltaTime;
                totalTime -= Time.deltaTime; // For timer logic

                if (!isAnswering)
                {
                    timer -= Time.deltaTime; // Update question timer
                    if (timer <= 10f && !finalSecondsPlaying)
                    {
                        finalSecondsPlaying = true;
                        PlaySound(finalSecondsAudioSource, true); // Loop the final seconds sound
                    }

                    if (timer <= 0f) // When question timer runs out
                    {
                        OnQuestionTimeOver(); // Skip to the next question
                    }
                }

                UpdateTotalTimeText();
                UpdateTimerUI();

                // End the game if total time runs out or all questions are answered
                if (totalTime <= 0f || totalQuestionsAnswered >= totalQuestions)
                {
                    EndGame();
                }
            }
        }

        void OnQuestionTimeOver()
        {
            messageText.text = "Time Over! Skipping to the next question.";
            StopSound(finalSecondsAudioSource); // Stop any looping sounds
            timer = timerDuration; // Reset the question timer for the next question
            totalQuestionsAnswered++;
            Invoke(nameof(LoadQuestion), answerColorDuration); // Skip to the next question
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

        void LoadAllQuestions()
        {
            allQuestions = new List<Question>();

            // Log available languages for debugging
            Debug.Log("Available languages in LanguageCategories:");
            foreach (var lang in languageCategories)
            {
                Debug.Log($"- {lang.language}");
            }

            // Find the selected language category
            LanguageCategories languageCategory = System.Array.Find(languageCategories, lang => lang.language == selectedLanguage);
            if (languageCategory == null)
            {
                Debug.LogError($"No data available for language: {selectedLanguage}. Ensure it matches one of the available language codes.");
                return;
            }

            foreach (var category in languageCategory.categories)
            {
                foreach (var question in category.questions)
                {
                    allQuestions.Add(question);
                }
            }

            // Shuffle the questions
            for (int i = 0; i < allQuestions.Count; i++)
            {
                Question temp = allQuestions[i];
                int randomIndex = Random.Range(i, allQuestions.Count);
                allQuestions[i] = allQuestions[randomIndex];
                allQuestions[randomIndex] = temp;
            }
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
            // Hide all dice faces after roll
            ShowDiceFace(-1);
            if (diceBackground != null) diceBackground.SetActive(false);
            if (dicePanel != null) dicePanel.SetActive(false);

            // Now load question for this color
            LoadQuestionForColor(lastRolledColor);
        }

        // Helper to show dice panel and start the roll
        void ShowDiceAndRoll()
        {
            if (dicePanel != null) dicePanel.SetActive(true);
            if (diceBackground != null) diceBackground.SetActive(true);
            rollDiceButton.gameObject.SetActive(true);
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

            // Find the selected language category
            LanguageCategories languageCategory = System.Array.Find(languageCategories, lang => lang.language == selectedLanguage);
            if (languageCategory == null)
            {
                Debug.LogError($"No data available for language: {selectedLanguage}.");
                return;
            }

            // Use enum for color matching, just like in QuizManager
            if (!System.Enum.TryParse<CategoryColor>(color, true, out var rolledColorEnum))
            {
                Debug.LogError($"Could not parse dice color '{color}' to CategoryColor enum.");
                messageText.text = $"Error: Invalid color '{color}'";
                return;
            }

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
            Question question = chosenCategory.GetNextQuestion();

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
            UpdateTimerUI();
            UpdateQuestionsLeftText();
        }

        void InitializeCategoryColorMapping()
        {
            // Only keep colorActive initialization
            colorActive = new Dictionary<string, bool>();
            foreach (var color in diceColors)
                colorActive[color] = true;
        }

        void CheckAnswer(int chosenIndex, int correctIndex)
        {
            if (isAnswering) return;

            isAnswering = true;

            Button selectedButton = answerButtons[chosenIndex];

            if (chosenIndex == correctIndex)
            {
                HighlightButton(selectedButton, correctAnswerColor);
                messageText.text = "Correct Answer!";
                playerScore++;
                UpdateScore();
                PlaySound(correctAnswerAudioSource);
            }
            else
            {
                HighlightButton(selectedButton, wrongAnswerColor);
                messageText.text = "Wrong Answer!";
                PlaySound(wrongAnswerAudioSource);

                // Highlight the correct answer button
                HighlightButton(answerButtons[correctIndex], correctAnswerColor);
            }

            totalQuestionsAnswered++;
            Invoke(nameof(PrepareNextQuestion), answerColorDuration);
            StartCoroutine(ResetButtonColorsCoroutine());
        }

        IEnumerator ResetButtonColorsCoroutine()
        {
            yield return new WaitForSeconds(answerColorDuration);
            foreach (Button btn in answerButtons)
            {
                ResetButtonColor(btn);
            }
        }

        void OnTimeOver()
        {
            messageText.text = "Time Over! Question Passed.";
            StopSound(finalSecondsAudioSource); // Stop any looping sounds
            totalQuestionsAnswered++;
            PrepareNextQuestion();
        }

        void SkipQuestion()
        {
            messageText.text = "Question Skipped!";
            StopSound(finalSecondsAudioSource); // Stop any looping sounds
            totalQuestionsAnswered++;
            PrepareNextQuestion();
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
            // Show time played from 0 upwards
            int totalSeconds = Mathf.RoundToInt(timePlayed);
            int minutes = totalSeconds / 60;
            int seconds = totalSeconds % 60;
            totalTimeText.text = $"{minutes:D2}:{seconds:D2} min";
        }

        void UpdateQuestionsLeftText()
        {
            questionsLeftText.text = $"Questions Left: {totalQuestions - totalQuestionsAnswered}";
        }

        void UpdateScore()
        {
            scoreText.text = playerScore.ToString();
        }

        void HighlightButton(Button button, Color color)
        {
            Debug.Log($"Highlighting button {button.name} with color {color}");
            ColorBlock colors = button.colors;
            colors.normalColor = color;
            colors.selectedColor = color; // Ensure the selected color is also set
            button.colors = colors;
        }

        void ResetButtonColor(Button button)
        {
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.selectedColor = Color.white; // Ensure the selected color is also reset
            button.colors = colors;
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
            finalScorePanel.SetActive(true);
            quizPanel.SetActive(false);
            StopSound(finalSecondsAudioSource);

            // Set final score and questions
            finalScoreText.text = $"Final Score: {playerScore}\nTotal Questions Answered: {totalQuestionsAnswered}";

            // Set time played in format MM:SS or HH:MM:SS if over 1 hour
            int totalSeconds = Mathf.RoundToInt(timePlayed);
            int hours = totalSeconds / 3600;
            int minutes = (totalSeconds % 3600) / 60;
            int seconds = totalSeconds % 60;
            string timePlayedStr = hours > 0
                ? $"{hours:D2}:{minutes:D2}:{seconds:D2}"
                : $"{minutes:D2}:{seconds:D2}";

            // Set time played text in the final score panel if present
            foreach (var tmp in finalScorePanel.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                if (tmp.name.ToLower().Contains("time"))
                {
                    tmp.text = $"Time Played: {timePlayedStr}";
                }
            }

            // Get player name and avatar from PlayerPrefs
            string playerName = PlayerPrefs.GetString("PlayerName", "Player");
            string avatarName = PlayerPrefs.GetString("Avatar", "");
            Sprite avatarSprite = null;
            if (!string.IsNullOrEmpty(avatarName))
            {
                avatarSprite = Resources.Load<Sprite>($"Avatars/{avatarName}");
                if (avatarSprite == null)
                    avatarSprite = Resources.Load<Sprite>(avatarName);
            }
            if (avatarSprite == null)
                avatarSprite = Resources.Load<Sprite>("DefaultAvatar");

            // Set player name and avatar in the final score panel if present (even if inactive)
            foreach (var tmp in finalScorePanel.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                if (tmp.name.ToLower().Contains("winner") && !tmp.name.ToLower().Contains("score"))
                {
                    tmp.text = playerName;
                }
            }
            foreach (var img in finalScorePanel.GetComponentsInChildren<Image>(true))
            {
                if (img.name.ToLower().Contains("avatar"))
                {
                    // --- Ensure the image is enabled before assigning the sprite ---
                    img.enabled = true;
                    img.sprite = avatarSprite;
                    img.color = Color.white; // Ensure visible
                }
            }
        }

        void InitializePlayerInfo()
        {
            // Get player name from PlayerPrefs
            string playerName = PlayerPrefs.GetString("PlayerName", "Player");
            playerNameText.text = playerName;

            // Always get avatar from PlayerPrefs (no Photon logic)
            string avatarName = PlayerPrefs.GetString("Avatar", "");
            Sprite avatarSprite = null;
            if (!string.IsNullOrEmpty(avatarName))
            {
                avatarSprite = Resources.Load<Sprite>($"Avatars/{avatarName}");
                if (avatarSprite == null)
                    avatarSprite = Resources.Load<Sprite>(avatarName);
            }
            if (avatarSprite == null)
                avatarSprite = Resources.Load<Sprite>("DefaultAvatar");

            // If the image is inactive, temporarily activate it to assign the sprite
            bool wasActive = playerAvatar.gameObject.activeSelf;
            if (!wasActive)
                playerAvatar.gameObject.SetActive(true);

            playerAvatar.sprite = avatarSprite;

            if (!wasActive)
                playerAvatar.gameObject.SetActive(false);
        }

        public void SetLanguage(string languageName)
        {
            selectedLanguage = languageName;

            // Reload questions to reflect the new language
            InitializeCategoryTracker();
            LoadAllQuestions();
            LoadQuestion();
        }

        // --- Add these methods for button listeners ---
        void OnSkipQuestion()
        {
            SkipQuestion();
        }

        void OnResetGame()
        {
            SceneManager.LoadScene(sceneToLoadOnReset);
        }

        void OnExitToResetScene()
        {
            SceneManager.LoadScene(sceneToLoadOnReset);
        }

        void OnPauseGame()
        {
            isPaused = true;
            pausedTimer = timer;
            pausedTotalTime = totalTime;
            pausePanel.SetActive(true);
            Time.timeScale = 0f; // Pause the game
        }

        void OnContinueGame()
        {
            isPaused = false;
            timer = pausedTimer;
            totalTime = pausedTotalTime;
            pausePanel.SetActive(false);
            Time.timeScale = 1f; // Resume the game
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
        public CategoryColor color; // <--- Add this line for color assignment in Inspector
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
}