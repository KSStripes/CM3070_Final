using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Canvas bridge for start, level-complete, and game-over screens.
// Buttons call GameManager; gameplay systems do not talk directly to UI.
namespace CM3070.Dungeon1
{
    public sealed class GameUI : MonoBehaviour
    {
        [SerializeField] private GameObject startPanel;
        [SerializeField] private GameObject levelCompletePanel;
        [SerializeField] private GameObject gameOverPanel;
        [SerializeField] private GameObject hudPanel;
        [SerializeField] private Button startButton;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text levelCompleteText;
        [SerializeField] private Button nextLevelButton;
        [SerializeField] private TMP_Text gameOverText;
        [SerializeField] private Button newGameButton;
        [SerializeField] private TMP_Text levelText;
        [SerializeField] private TMP_Text coinText;
        [SerializeField] private TMP_Text healthText;
        [SerializeField] private Slider healthBar;

        [Header("Display Text")]
        [SerializeField] private string titleLabel = "Dungeon Crawler";
        [SerializeField] private string levelLabel = "Level";
        [SerializeField] private string currencyLabel = "Coins";
        [SerializeField] private string levelCompleteLabel = "Level Complete";
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
            currentState = state;

            if (startPanel != null) startPanel.SetActive(state == GameState.StartScreen);
            if (levelCompletePanel != null) levelCompletePanel.SetActive(state == GameState.LevelComplete);
            if (gameOverPanel != null) gameOverPanel.SetActive(state == GameState.GameOver);
            if (hudPanel != null) hudPanel.SetActive(state == GameState.Playing);

            if (titleText != null)
            {
                titleText.text = titleLabel;
            }

            if (levelCompleteText != null)
            {
                levelCompleteText.text = levelCompleteLabel;
            }

            if (gameOverText != null)
            {
                gameOverText.text = gameOverLabel;
            }

            if (nextLevelButton != null)
            {
                nextLevelButton.interactable = state == GameState.LevelComplete;
            }

            if (newGameButton != null)
            {
                newGameButton.interactable = state == GameState.GameOver;
            }

            SetLevel(level);
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
            if (levelText != null)
            {
                levelText.text = $"{levelLabel}: {level}";
            }
        }

        public void SetCoinCount(int count)
        {
            if (coinText != null)
            {
                coinText.text = $"{currencyLabel}: {count}";
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
