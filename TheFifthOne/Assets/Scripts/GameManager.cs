using UnityEngine;
using Fusion;
using System.Collections.Generic;
using System.Collections;

public class GameManager : NetworkBehaviour
{
    [Header("Game State")]
    [Networked] public GameState CurrentGameState { get; set; } = GameState.WaitingRoom;
    [Networked] public bool gameStarted { get; set; } = false;
    [Networked] public float gameStartTime { get; set; } = 0f;

    [Header("Game Settings")]
    [SerializeField] private float gameStartCountdown = 3f;
    [SerializeField] private int minPlayersToStart = 2;

    [Header("Scene References")]
    [SerializeField] private int gameSceneIndex = 2;

    // Events
    public System.Action<GameState> OnGameStateChanged;
    public System.Action<float> OnCountdownUpdate;
    public System.Action OnGameStarted;

    // Internal state
    private GameState previousState;
    private SpawnPlayer spawnPlayerManager;
    private bool isCountingDown = false;

    public override void Spawned()
    {
        Debug.Log("[GameManager] Spawned - Finding SpawnPlayer reference");

        spawnPlayerManager = FindObjectOfType<SpawnPlayer>();
        previousState = CurrentGameState;

        // Host olarak spawn olduysak, initial state'i set et
        if (Object.HasStateAuthority)
        {
            CurrentGameState = GameState.WaitingRoom;
            gameStarted = false;
            Debug.Log("[GameManager] Initialized by host");
        }
    }

    public override void FixedUpdateNetwork()
    {
        // State deðiþikliklerini kontrol et
        if (previousState != CurrentGameState)
        {
            HandleGameStateChange();
            previousState = CurrentGameState;
        }

        // Countdown logic
        HandleCountdownLogic();
    }

    private void HandleGameStateChange()
    {
        Debug.Log($"[GameManager] Game state changed: {previousState} ? {CurrentGameState}");

        // Event'i fire et
        OnGameStateChanged?.Invoke(CurrentGameState);

        // State'e göre özel iþlemler
        switch (CurrentGameState)
        {
            case GameState.WaitingRoom:
                HandleWaitingRoomState();
                break;
            case GameState.InGame:
                HandleInGameState();
                break;
        }
    }

    private void HandleWaitingRoomState()
    {
        Debug.Log("[GameManager] Entered Waiting Room state");
        gameStarted = false;
        isCountingDown = false;
    }

    private void HandleInGameState()
    {
        Debug.Log("[GameManager] Entered In Game state");
        gameStarted = true;

        // Game start time'ý kaydet
        if (Object.HasStateAuthority)
        {
            gameStartTime = Runner.SimulationTime;
        }

        OnGameStarted?.Invoke();
    }

    private void HandleCountdownLogic()
    {
        // Sadece countdown sýrasýnda çalýþýr
        if (!isCountingDown || !Object.HasStateAuthority) return;

        float elapsedTime = Runner.SimulationTime - gameStartTime;
        float remainingTime = gameStartCountdown - elapsedTime;

        // Countdown update event'i
        OnCountdownUpdate?.Invoke(remainingTime);

        // Countdown bitti, oyunu baþlat
        if (remainingTime <= 0f)
        {
            isCountingDown = false;
            StartGameImmediately();
        }
    }

    // Host tarafýndan çaðrýlýr (SpawnPlayer'dan)
    public void StartGame()
    {
        if (!Object.HasStateAuthority)
        {
            Debug.LogWarning("[GameManager] Only host can start the game!");
            return;
        }

        if (CurrentGameState != GameState.WaitingRoom)
        {
            Debug.LogWarning("[GameManager] Game can only be started from Waiting Room!");
            return;
        }

        // Player sayýsýný kontrol et
        int playerCount = GetActivePlayerCount();
        if (playerCount < minPlayersToStart)
        {
            Debug.LogWarning($"[GameManager] Need at least {minPlayersToStart} players to start! Current: {playerCount}");
            return;
        }

        Debug.Log("[GameManager] Starting game countdown...");

        // Countdown baþlat
        StartCountdown();
    }

    private void StartCountdown()
    {
        if (!Object.HasStateAuthority) return;

        isCountingDown = true;
        gameStartTime = Runner.SimulationTime;

        Debug.Log($"[GameManager] Countdown started: {gameStartCountdown} seconds");
    }

    private void StartGameImmediately()
    {
        if (!Object.HasStateAuthority) return;

        Debug.Log("[GameManager] Starting game immediately - transitioning to game scene");

        // Game state'i deðiþtir
        CurrentGameState = GameState.InGame;

        // SpawnPlayer'a scene geçiþini bildir
        if (spawnPlayerManager != null)
        {
            spawnPlayerManager.TransitionToGameScene();
        }
        else
        {
            Debug.LogError("[GameManager] SpawnPlayer reference is null!");
        }
    }

    // RPC for immediate game start (debug/admin purposes)
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_ForceStartGame()
    {
        if (Object.HasStateAuthority)
        {
            StartGameImmediately();
        }
    }

    // Utility methods
    private int GetActivePlayerCount()
    {
        if (Runner == null) return 0;

        int count = 0;
        foreach (var player in Runner.ActivePlayers)
        {
            if (Runner.GetPlayerObject(player))
                count++;
        }
        return count;
    }

    public float GetGameDuration()
    {
        if (!gameStarted) return 0f;
        return Runner.SimulationTime - gameStartTime;
    }

    public bool CanStartGame()
    {
        return Object.HasStateAuthority &&
               CurrentGameState == GameState.WaitingRoom &&
               GetActivePlayerCount() >= minPlayersToStart &&
               !isCountingDown;
    }

    public bool IsCountingDown()
    {
        return isCountingDown;
    }

    public float GetCountdownRemaining()
    {
        if (!isCountingDown) return 0f;

        float elapsed = Runner.SimulationTime - gameStartTime;
        return Mathf.Max(0f, gameStartCountdown - elapsed);
    }

    // Debug methods
    [ContextMenu("Force Start Game")]
    private void Debug_ForceStartGame()
    {
        if (Application.isPlaying && Object.HasStateAuthority)
        {
            StartGameImmediately();
        }
    }

    [ContextMenu("Reset to Waiting Room")]
    private void Debug_ResetToWaitingRoom()
    {
        if (Application.isPlaying && Object.HasStateAuthority)
        {
            CurrentGameState = GameState.WaitingRoom;
            gameStarted = false;
            isCountingDown = false;
        }
    }

    // Public getters
    public bool IsHost()
    {
        return Object != null && Object.HasStateAuthority;
    }

    public bool IsGameActive()
    {
        return CurrentGameState == GameState.InGame && gameStarted;
    }

    public bool IsWaitingRoom()
    {
        return CurrentGameState == GameState.WaitingRoom;
    }

    // Cleanup
    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        Debug.Log("[GameManager] Despawned");
        OnGameStateChanged = null;
        OnCountdownUpdate = null;
        OnGameStarted = null;
    }
}