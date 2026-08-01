using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Shared canvas bridge for scene flow: start, level-complete, game-over, HUD visibility,
// buttons, basic health, and the existing overview minimap panel wiring.
// OfficeScene can keep this component and add OfficeHUD to the same GameUI object.
// Keep office-specific objectives, inventory, feedback, and debug readouts in OfficeHUD.
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
        // Optional start-menu controls for selecting which player visual to enable at spawn.
        [SerializeField] private Button femaleAvatarButton;
        [SerializeField] private Button maleAvatarButton;
        [SerializeField] private TMP_Text selectedAvatarText;

        [Header("Display Text")]
        [SerializeField] private string titleLabel = "EndOfShift";
        [SerializeField] private string dayLabel = "Day";
        [SerializeField] private string dayCompleteLabel = "Shift Done";
        [SerializeField] private string gameWonLabel = "Probation Period Finished";
        [TextArea]
        [SerializeField] private string gameWonMessage = "Congratulations!\nYou're now hired.";
        [TextArea]
        [SerializeField] private string creditsMessage = "Credits\nDesign, code, and burnout:\nKristin Schumann #210569373\nA University of London CM3070 Final Project";
        [SerializeField] private string gameOverLabel = "Game Over";

        private GameState currentState;

        private void Awake()
        {
            // Button callbacks stay here so UI buttons only need Inspector references to this component.
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

            if (femaleAvatarButton != null)
            {
                femaleAvatarButton.onClick.AddListener(OnFemaleAvatarButtonPressed);
            }

            if (maleAvatarButton != null)
            {
                maleAvatarButton.onClick.AddListener(OnMaleAvatarButtonPressed);
            }
        }

        public void ShowState(GameState state, int day)
        {
            ShowState(state, day, day.ToString(), day);
        }

        public void ShowState(GameState state, int day, string dayName, int totalDays)
        {
            currentState = state;
            // If no separate win panel is assigned, reuse the normal completion panel for the final day.
            bool useGameWonPanel = gameWonPanel != null;

            // Only one high-level screen should be visible at a time.
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
                // The final fallback panel uses the game-won message; normal days show day completion.
                levelCompleteText.text = state == GameState.GameWon
                    ? gameWonMessage
                    : $"{dayCompleteLabel}\n{dayName} complete";
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
                // There is no "next day" button on game over or final victory.
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
                ? GameManager.Instance.SelectedPlayerAvatar
                : PlayerAvatarChoice.Female);

            SetDay(day, dayName, totalDays);
        }

        public void OnStartButtonPressed()
        {
            GameManager.Instance?.StartGame();
        }

        public void OnFemaleAvatarButtonPressed()
        {
            // Store the choice on GameManager so the office EntitySpawner can read it later.
            GameManager.Instance?.SetPlayerAvatarChoice(PlayerAvatarChoice.Female);
            RefreshAvatarChoiceLabel(PlayerAvatarChoice.Female);
        }

        public void OnMaleAvatarButtonPressed()
        {
            // Store the choice on GameManager so the office EntitySpawner can read it later.
            GameManager.Instance?.SetPlayerAvatarChoice(PlayerAvatarChoice.Male);
            RefreshAvatarChoiceLabel(PlayerAvatarChoice.Male);
        }

        public void OnNextDayButtonPressed()
        {
            // Prevent accidental button calls when the panel is not in the day-complete state.
            if (currentState != GameState.DayComplete) return;

            GameManager.Instance?.NextDay();
        }

        public void OnNewGameButtonPressed()
        {
            // New game is offered from both failure and final victory panels.
            if (currentState != GameState.GameOver && currentState != GameState.GameWon) return;

            GameManager.Instance?.NewGame();
        }

        public void SetDay(int day)
        {
            SetDay(day, day.ToString(), day);
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
            // Health remains in GameUI because it is shared by Dungeon1 and OfficeScene.
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

        private void RefreshAvatarChoiceLabel(PlayerAvatarChoice avatarChoice)
        {
            if (selectedAvatarText != null)
            {
                // Keeps the start menu clear without requiring extra button-state styling.
                selectedAvatarText.text = $"Player: {avatarChoice}";
            }
        }
    }
}
