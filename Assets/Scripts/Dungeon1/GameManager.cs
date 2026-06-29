using UnityEngine;

// Central gameplay notification point.
// Currently logs events for testing; later this can forward the same events to HUD/UI systems.
namespace CM3070.Dungeon1
{
    public enum GameState
    {
        StartScreen,
        Playing,
        LevelComplete,
        GameOver
    }

    public sealed class GameManager : MonoBehaviour
    {
        [SerializeField] private DungeonController dungeonController;
        [SerializeField] private GameUI gameUI;

        public static GameManager Instance { get; private set; }
        public GameState CurrentState { get; private set; } = GameState.StartScreen;
        public int CurrentLevel { get; private set; } = 1;

        private void Awake()
        {
            // Singleton pattern: keep one scene-level GameManager available through Instance.
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void Start()
        {
            dungeonController ??= FindFirstObjectByType<DungeonController>();
            gameUI ??= FindFirstObjectByType<GameUI>();
            SetState(GameState.StartScreen);
        }

        public void StartGame()
        {
            CurrentLevel = 1;
            SetState(GameState.Playing);
            if (dungeonController == null) return;
            dungeonController.StartNewGame();
            Debug.Log($"Game started. Level={CurrentLevel}");
        }

        public void NextLevel()
        {
            CurrentLevel++;
            SetState(GameState.Playing);
            if (dungeonController == null) return;
            dungeonController.StartNextLevel();
            Debug.Log($"Next level. Level={CurrentLevel}");
        }

        public void NewGame()
        {
            CurrentLevel = 1;
            SetState(GameState.Playing);
            if (dungeonController == null) return;
            dungeonController.StartNewGame();
            Debug.Log("New game.");
        }

        public void NotifyCoinsChanged(int coinCount)
        {
            gameUI?.SetCoinCount(coinCount);
            Debug.Log($"Coins: {coinCount}");
        }

        public void NotifyArmourCollected(string armourName, string armourType, int maxHealth)
        {
            Debug.Log($"Armour: {armourName} ({armourType}). Max health={maxHealth}");
        }

        public void NotifyWeaponCollected(LootProperties weapon, PlayerInventory inventory)
        {
            Debug.Log($"Weapon: {weapon.WeaponName} ({weapon.WeaponType}). Attack={inventory.Attack}");
        }

        public void NotifyHealthChanged(int currentHealth, int maxHealth)
        {
            gameUI?.SetHealth(currentHealth, maxHealth);
            Debug.Log($"Health: {currentHealth}/{maxHealth}");
        }

        public void NotifyPlayerDied()
        {
            if (CurrentState != GameState.Playing)
            {
                return;
            }

            SetState(GameState.GameOver);
            Debug.Log("Player died.");
        }

        public void NotifyExitReached()
        {
            if (CurrentState != GameState.Playing)
            {
                return;
            }

            SetState(GameState.LevelComplete);
            Debug.Log($"Level complete. Level={CurrentLevel}");
        }

        private void SetState(GameState state)
        {
            CurrentState = state;
            Time.timeScale = CurrentState == GameState.Playing ? 1f : 0f;
            gameUI?.ShowState(CurrentState, CurrentLevel);
        }
    }
}
