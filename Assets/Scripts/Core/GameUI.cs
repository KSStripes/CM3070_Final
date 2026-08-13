using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Shared canvas bridge for scene flow, buttons, day text, and Resolve display.
namespace CM3070.Dungeon1
{
    public sealed class GameUI : MonoBehaviour
    {
        [SerializeField] private GameObject startPanel;
        [SerializeField] private GameObject levelCompletePanel;
        [SerializeField] private GameObject gameWonPanel;
        [SerializeField] private GameObject gameOverPanel;
        [SerializeField] private GameObject hudPanel;
        [SerializeField] private Button startButton;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text levelCompleteText;
        [SerializeField] private TMP_Text gameWonTitleText;
        [SerializeField] private TMP_Text gameWonText;
        [SerializeField] private TMP_Text creditsText;
        [SerializeField] private Button gameWonButton;
        [SerializeField] private Button nextLevelButton;
        [SerializeField] private TMP_Text gameOverText;
        [SerializeField] private Button newGameButton;
        [SerializeField] private TMP_Text levelText;
        [SerializeField] private TMP_Text healthText;
        [SerializeField] private Slider healthBar;

        [Header("Player Avatar Choice")]
        [SerializeField] private Button FButton;
        [SerializeField] private Button MButton;
        [SerializeField] private TMP_Text playAsText;

        [Header("Display Text")]
        [SerializeField] private string titleLabel = "End of Shift";
        [SerializeField] private string dayLabel = "Shift";
        [SerializeField] private string dayCompleteLabel = "Shift Complete";
        [SerializeField] private string gameWonLabel = "Probation Complete";
        [TextArea]
        [SerializeField] private string gameWonMessage = "Congratulations!\nYou made it through the week.\nThis completes your probation!";
        [TextArea]
        [SerializeField] private string creditsMessage = "Credits\nDesign, code, and burnout:\nKristin Schumann #210569373\nA University of London CM3070 Final Project";
        [SerializeField] private string gameOverLabel = "Resolve Depleted\nTake a breath and start a new shift.";

        private GameState currentState;

        private void Awake()
        {
            if (startButton != null)
            {
                startButton.onClick.AddListener(OnStartButtonPressed);
            }

            if (nextLevelButton != null)
            {
                nextLevelButton.onClick.AddListener(OnNextDayButtonPressed);
            }

            if (newGameButton != null)
            {
                newGameButton.onClick.AddListener(OnNewGameButtonPressed);
            }

            if (gameWonButton != null)
            {
                gameWonButton.onClick.AddListener(OnNewGameButtonPressed);
            }

            if (FButton != null)
            {
                FButton.onClick.AddListener(OnFemaleAvatarButtonPressed);
            }

            if (MButton != null)
            {
                MButton.onClick.AddListener(OnMaleAvatarButtonPressed);
            }
        }

        public void ShowState(GameState state, int day, string dayName, int totalDays)
        {
            currentState = state;
            bool useGameWonPanel = gameWonPanel != null;

            if (startPanel != null) startPanel.SetActive(state == GameState.StartScreen);
            if (levelCompletePanel != null)
            {
                levelCompletePanel.SetActive(state == GameState.DayComplete || (state == GameState.GameWon && !useGameWonPanel));
            }
            if (gameWonPanel != null) gameWonPanel.SetActive(state == GameState.GameWon);
            if (gameOverPanel != null) gameOverPanel.SetActive(state == GameState.GameOver);
            if (hudPanel != null) hudPanel.SetActive(state == GameState.Playing);

            if (titleText != null)
            {
                titleText.text = titleLabel;
            }

            if (gameWonTitleText != null)
            {
                gameWonTitleText.text = titleLabel;
            }

            if (levelCompleteText != null)
            {
                levelCompleteText.text = state == GameState.GameWon
                    ? gameWonMessage
                    : $"{dayCompleteLabel}\n{dayName} survived";
            }

            if (gameWonText != null)
            {
                gameWonText.text = $"{gameWonLabel}\n{gameWonMessage}";
            }

            if (creditsText != null)
            {
                creditsText.text = creditsMessage;
            }

            if (gameOverText != null)
            {
                gameOverText.text = gameOverLabel;
            }

            if (nextLevelButton != null)
            {
                nextLevelButton.interactable = state == GameState.DayComplete;
                nextLevelButton.gameObject.SetActive(state == GameState.DayComplete);
            }

            if (newGameButton != null)
            {
                newGameButton.interactable = state == GameState.GameOver;
            }

            if (gameWonButton != null)
            {
                gameWonButton.interactable = state == GameState.GameWon;
            }

            RefreshAvatarChoiceLabel(GameManager.Instance != null
                ? GameManager.Instance.SelectedPlayerChoice
                : PlayerChoice.Female);

            SetDay(day, dayName, totalDays);
        }

        public void OnStartButtonPressed()
        {
            AudioManager.Instance?.PlayButtonClick();
            GameManager.Instance?.StartGame();
        }

        public void OnFemaleAvatarButtonPressed()
        {
            AudioManager.Instance?.PlayButtonClick();
            GameManager.Instance?.SetPlayerChoice(PlayerChoice.Female);
            RefreshAvatarChoiceLabel(PlayerChoice.Female);
        }

        public void OnMaleAvatarButtonPressed()
        {
            AudioManager.Instance?.PlayButtonClick();
            GameManager.Instance?.SetPlayerChoice(PlayerChoice.Male);
            RefreshAvatarChoiceLabel(PlayerChoice.Male);
        }

        public void OnNextDayButtonPressed()
        {
            if (currentState != GameState.DayComplete) return;

            AudioManager.Instance?.PlayButtonClick();
            GameManager.Instance?.NextDay();
        }

        public void OnNewGameButtonPressed()
        {
            if (currentState != GameState.GameOver && currentState != GameState.GameWon) return;

            AudioManager.Instance?.PlayButtonClick();
            GameManager.Instance?.NewGame();
        }

        public void SetDay(int day, string dayName, int totalDays)
        {
            if (levelText != null)
            {
                levelText.text = $"{dayLabel}: {dayName}";
            }
        }

        public void SetHealth(int current, int max)
        {
            if (healthBar != null)
            {
                healthBar.maxValue = max;
                healthBar.value = current;
            }

            if (healthText != null)
            {
                healthText.text = $"Resolve: {current}/{max}";
            }
        }

        private void RefreshAvatarChoiceLabel(PlayerChoice choice)
        {
            if (playAsText != null)
            {
                playAsText.text = $"Play as: {choice}";
            }
        }
    }
}
