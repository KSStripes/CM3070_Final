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
        [SerializeField] private GameObject probationCompletePanel;
        [SerializeField] private GameObject gameOverPanel;
        [SerializeField] private GameObject hudPanel;
        [SerializeField] private Button startButton;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text levelCompleteText;
        [SerializeField] private TMP_Text probationCompleteText;
        [SerializeField] private TMP_Text probationCreditsText;
        [SerializeField] private Button nextLevelButton;
        [SerializeField] private TMP_Text gameOverText;
        [SerializeField] private Button newGameButton;
        [SerializeField] private TMP_Text levelText;
        [SerializeField] private TMP_Text healthText;
        [SerializeField] private Slider healthBar;

        [Header("Display Text")]
        [SerializeField] private string titleLabel = "Dungeon Crawler";
        [SerializeField] private string levelLabel = "Day";
        [SerializeField] private string levelCompleteLabel = "Level Complete";
        [SerializeField] private string probationCompleteLabel = "Probation Complete";
        [TextArea]
        [SerializeField] private string probationCompleteMessage = "You managed your probation period.\nYou're now hired.";
        [TextArea]
        [SerializeField] private string probationCreditsMessage = "Credits\nDesign, code, and emotional damage: Kristin";
        [SerializeField] private string gameOverLabel = "Game Over";

        private GameState currentState;

        private void Awake()
        {
            if (startButton != null)
            {
                startButton.onClick.AddListener(OnStartButtonPressed);
            }

            if (nextLevelButton != null)
            {
                nextLevelButton.onClick.AddListener(OnNextLevelButtonPressed);
            }

            if (newGameButton != null)
            {
                newGameButton.onClick.AddListener(OnNewGameButtonPressed);
            }
        }

        public void ShowState(GameState state, int level)
        {
            ShowState(state, level, level.ToString(), level);
        }

        public void ShowState(GameState state, int level, string dayName, int totalDays)
        {
            currentState = state;
            bool useSeparateProbationPanel = probationCompletePanel != null;

            if (startPanel != null) startPanel.SetActive(state == GameState.StartScreen);
            if (levelCompletePanel != null)
            {
                levelCompletePanel.SetActive(state == GameState.LevelComplete || (state == GameState.GameComplete && !useSeparateProbationPanel));
            }
            if (probationCompletePanel != null) probationCompletePanel.SetActive(state == GameState.GameComplete);
            if (gameOverPanel != null) gameOverPanel.SetActive(state == GameState.GameOver);
            if (hudPanel != null) hudPanel.SetActive(state == GameState.Playing);

            if (titleText != null)
            {
                titleText.text = titleLabel;
            }

            if (levelCompleteText != null)
            {
                levelCompleteText.text = state == GameState.GameComplete
                    ? probationCompleteMessage
                    : $"{levelCompleteLabel}\n{dayName} complete";
            }

            if (probationCompleteText != null)
            {
                probationCompleteText.text = $"{probationCompleteLabel}\n{probationCompleteMessage}";
            }

            if (probationCreditsText != null)
            {
                probationCreditsText.text = probationCreditsMessage;
            }

            if (gameOverText != null)
            {
                gameOverText.text = gameOverLabel;
            }

            if (nextLevelButton != null)
            {
                nextLevelButton.interactable = state == GameState.LevelComplete;
                nextLevelButton.gameObject.SetActive(state == GameState.LevelComplete);
            }

            if (newGameButton != null)
            {
                newGameButton.interactable = state == GameState.GameOver;
            }

            SetLevel(level, dayName, totalDays);
        }

        public void OnStartButtonPressed()
        {
            GameManager.Instance?.StartGame();
        }

        public void OnNextLevelButtonPressed()
        {
            if (currentState != GameState.LevelComplete) return;

            GameManager.Instance?.NextLevel();
        }

        public void OnNewGameButtonPressed()
        {
            if (currentState != GameState.GameOver) return;

            GameManager.Instance?.NewGame();
        }

        public void SetLevel(int level)
        {
            SetLevel(level, level.ToString(), level);
        }

        public void SetLevel(int level, string dayName, int totalDays)
        {
            if (levelText != null)
            {
                levelText.text = $"{levelLabel}: {dayName}";
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
                healthText.text = $"Health: {current}/{max}";
            }
        }
    }
}
