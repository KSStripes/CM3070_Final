using System;
using UnityEngine;
using CM3070.Office;

// Owns the shared game state, day progression, and scene-level UI updates.
namespace CM3070.Dungeon1
{
    public enum GameState
    {
        StartScreen,
        Playing,
        DayComplete,
        GameWon,
        GameOver
    }

    // Start-menu choice used by the office player prefab to show the matching visual child.
    public enum PlayerChoice
    {
        Female,
        Male
    }

    public sealed class GameManager : MonoBehaviour
    {
        [SerializeField] private DungeonController dungeonController;
        [SerializeField] private OfficeController officeController;
        [SerializeField] private GameUI gameUI;
        [SerializeField] private string[] workdayNames =
        {
            "Monday",
            "Tuesday",
            "Wednesday",
            "Thursday",
            "Friday",
            "Saturday"
        };

        public static GameManager Instance { get; private set; }
        public event Action<int, int> HealthChanged;

        public GameState CurrentState { get; private set; } = GameState.StartScreen;
        public int CurrentDay { get; private set; } = 1;
        // Safe default if the player starts without choosing an avatar.
        public PlayerChoice SelectedPlayerChoice { get; private set; } = PlayerChoice.Female;
        public string CurrentDayName => DayNameFor(CurrentDay);
        public bool IsFinalDay => CurrentDay >= TotalDays;
        public int TotalDays => Mathf.Max(1, workdayNames != null ? workdayNames.Length : 0);

        private int lastHealth = -1;

        private void Awake()
        {
            // One scene-level manager is accessed by spawned gameplay objects.
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
            officeController ??= FindFirstObjectByType<OfficeController>();
            gameUI ??= FindFirstObjectByType<GameUI>();
            SetState(GameState.StartScreen);
        }

        public void StartGame()
        {
            CurrentDay = 1;
            SetState(GameState.Playing);
            StartNewControllerRun();
        }

        public void SetPlayerChoice(PlayerChoice choice)
        {
            SelectedPlayerChoice = choice;
        }

        public void NextDay()
        {
            if (CurrentState == GameState.GameWon)
            {
                return;
            }

            CurrentDay++;
            SetState(GameState.Playing);
            StartNextControllerRun();
        }

        public void NewGame()
        {
            CurrentDay = 1;
            SetState(GameState.Playing);
            StartNewControllerRun();
        }

        public void NotifyCoinsChanged(int coinCount)
        {
            // Legacy Dungeon1 hook; Office readouts live in OfficeHUD.
        }

        public void NotifyArmourCollected(string armourName, string armourType, int maxHealth)
        {
        }

        public void NotifyWeaponCollected(LootProperties weapon, PlayerInventory inventory)
        {
        }

        public void NotifyHealthChanged(int currentHealth, int maxHealth)
        {
            gameUI?.SetHealth(currentHealth, maxHealth);
            HealthChanged?.Invoke(currentHealth, maxHealth);

            if (CurrentState == GameState.Playing && lastHealth >= 0 && currentHealth < lastHealth)
            {
                AudioManager.Instance?.PlayResolveDamage();
            }

            lastHealth = currentHealth;
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

            if (IsFinalDay)
            {
                SetState(GameState.GameWon);
                Debug.Log("Probation period complete. Player is now hired.");
                return;
            }

            SetState(GameState.DayComplete);
            Debug.Log($"Day complete. Day={CurrentDayName}");
        }

        private void SetState(GameState state)
        {
            CurrentState = state;
            Time.timeScale = CurrentState == GameState.Playing ? 1f : 0f;
            gameUI?.ShowState(CurrentState, CurrentDay, CurrentDayName, TotalDays);
            PlayStateSound(state, CurrentDay);
        }

        private static void PlayStateSound(GameState state, int day)
        {
            if (state == GameState.Playing) AudioManager.Instance?.PlayGameplayMusic(day);
            else AudioManager.Instance?.PlayMenuMusic();

            if (state == GameState.DayComplete) AudioManager.Instance?.PlayDayComplete();
            else if (state == GameState.GameOver) AudioManager.Instance?.PlayGameOver();
            else if (state == GameState.GameWon) AudioManager.Instance?.PlayGameWon();
        }

        private string DayNameFor(int day)
        {
            if (workdayNames == null || workdayNames.Length == 0)
            {
                return $"Day {day}";
            }

            int index = Mathf.Clamp(day - 1, 0, workdayNames.Length - 1);
            return workdayNames[index];
        }

        // OfficeScene uses OfficeController; Dungeon1 keeps its original controller.
        private void StartNewControllerRun()
        {
            if (officeController != null)
            {
                officeController.StartNewGame();
                return;
            }

            dungeonController?.StartNewGame();
        }

        private void StartNextControllerRun()
        {
            if (officeController != null)
            {
                officeController.StartNextLevel();
                return;
            }

            dungeonController?.StartNextLevel();
        }
    }
}
