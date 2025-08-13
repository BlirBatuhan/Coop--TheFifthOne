using UnityEngine;
using Fusion;
using System.Collections;
using System.Linq;

public class GameManager : NetworkBehaviour
{
    [Header("Game State")]
    [Networked] public bool IsGameActive { get; set; } = false;
    [Networked] public float GameStartTime { get; set; } = 0f;
    [Networked] public int ConnectedPlayers { get; set; } = 0;

    [Header("Game Settings")]
    [SerializeField] private float countdownDuration = 5f;
    [SerializeField] private int minPlayersToStart = 2;
    [SerializeField] private int maxPlayers = 4;

    [Header("Spawn Settings")]
    [SerializeField] private Transform[] playerSpawnPoints;
    [SerializeField] private Vector3 defaultSpawnPosition = Vector3.zero;

    // Events
    public System.Action<float> OnCountdownTick;
    public System.Action OnGameStarted;
    public System.Action OnGameEnded;
    public System.Action<int> OnPlayerCountChanged;

    // Internal state
    private bool isCountdownActive = false;
    private Coroutine countdownCoroutine;

    public override void Spawned()
    {
        // Bu GameManager scene'de zaten var, sadece ownership alýyor
        Debug.Log($"[GameManager] Activated - Owner: {Object.InputAuthority}, HasAuthority: {Object.HasStateAuthority}");

        if (Object.HasStateAuthority)
        {
            // Host olarak baþlangýç ayarlarý
            IsGameActive = false;
            GameStartTime = 0f;
            ConnectedPlayers = Runner.ActivePlayers.Count();

            Debug.Log("[GameManager] Host initialized game state");
        }
    }

    public override void FixedUpdateNetwork()
    {
        // Player count'u güncel tut
        if (Object.HasStateAuthority)
        {
            int currentCount = Runner.ActivePlayers.Count();
            if (currentCount != ConnectedPlayers)
            {
                ConnectedPlayers = currentCount;
                OnPlayerCountChanged?.Invoke(ConnectedPlayers);
            }
        }
    }

    // Host oyunu baþlatýr
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_StartGameCountdown()
    {
        if (!Object.HasStateAuthority) return;

        if (isCountdownActive)
        {
            Debug.LogWarning("[GameManager] Countdown already active!");
            return;
        }

        if (ConnectedPlayers < minPlayersToStart)
        {
            Debug.LogWarning($"[GameManager] Need at least {minPlayersToStart} players to start!");
            return;
        }

        Debug.Log("[GameManager] Starting countdown...");
        StartCountdown();
    }

    private void StartCountdown()
    {
        if (countdownCoroutine != null)
            StopCoroutine(countdownCoroutine);

        countdownCoroutine = StartCoroutine(CountdownCoroutine());
    }

    private IEnumerator CountdownCoroutine()
    {
        isCountdownActive = true;
        float timeRemaining = countdownDuration;

        while (timeRemaining > 0)
        {
            OnCountdownTick?.Invoke(timeRemaining);
            yield return new WaitForSeconds(1f);
            timeRemaining -= 1f;
        }

        // Countdown bitti, oyunu baþlat
        StartGame();
    }

    private void StartGame()
    {
        if (!Object.HasStateAuthority) return;

        isCountdownActive = false;
        IsGameActive = true;
        GameStartTime = Runner.SimulationTime;

        Debug.Log("[GameManager] Game started!");
        OnGameStarted?.Invoke();

        // Tüm clientlara oyun baþladýðýný bildir
        RPC_NotifyGameStarted();
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_NotifyGameStarted()
    {
        OnGameStarted?.Invoke();
        Debug.Log("[GameManager] Game start notification received");
    }

    // Oyunu sonlandýr
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_EndGame()
    {
        if (!Object.HasStateAuthority) return;

        IsGameActive = false;
        isCountdownActive = false;

        if (countdownCoroutine != null)
        {
            StopCoroutine(countdownCoroutine);
            countdownCoroutine = null;
        }

        Debug.Log("[GameManager] Game ended");
        OnGameEnded?.Invoke();
    }

    // Spawn pozisyonu al
    public Vector3 GetSpawnPosition(int playerIndex)
    {
        if (playerSpawnPoints != null && playerSpawnPoints.Length > 0)
        {
            int spawnIndex = playerIndex % playerSpawnPoints.Length;
            return playerSpawnPoints[spawnIndex].position;
        }

        // Default spawn with offset
        return defaultSpawnPosition + new Vector3(playerIndex * 3f, 0, 0);
    }

    // Utility methods
    public float GetGameDuration()
    {
        if (!IsGameActive || GameStartTime == 0f) return 0f;
        return Runner.SimulationTime - GameStartTime;
    }

    public bool CanStartGame()
    {
        return Object.HasStateAuthority &&
               !IsGameActive &&
               !isCountdownActive &&
               ConnectedPlayers >= minPlayersToStart;
    }

    public bool IsCountdownActive()
    {
        return isCountdownActive;
    }

    public bool IsOwner()
    {
        return Object != null && Object.HasStateAuthority;
    }

    public int GetConnectedPlayerCount()
    {
        return ConnectedPlayers;
    }

    public int GetMaxPlayers()
    {
        return maxPlayers;
    }

    public int GetMinPlayersToStart()
    {
        return minPlayersToStart;
    }

    private void UpdatePlayerCount(int count)
    {
        // Bu metod artýk gereksiz, FixedUpdateNetwork'te otomatik güncelleniyor
        Debug.Log($"[GameManager] Player count updated: {count}");
    }

    // Debug methods
    [ContextMenu("Force Start Game")]
    private void Debug_ForceStartGame()
    {
        if (Application.isPlaying && Object.HasStateAuthority)
        {
            StartGame();
        }
    }

    [ContextMenu("Start Countdown")]
    private void Debug_StartCountdown()
    {
        if (Application.isPlaying && Object.HasStateAuthority)
        {
            RPC_StartGameCountdown();
        }
    }

    [ContextMenu("End Game")]
    private void Debug_EndGame()
    {
        if (Application.isPlaying && Object.HasStateAuthority)
        {
            RPC_EndGame();
        }
    }

    // Cleanup
    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (countdownCoroutine != null)
        {
            StopCoroutine(countdownCoroutine);
        }

        OnCountdownTick = null;
        OnGameStarted = null;
        OnGameEnded = null;
        OnPlayerCountChanged = null;

        Debug.Log("[GameManager] Despawned");
    }

    private void OnDestroy()
    {
        if (countdownCoroutine != null)
        {
            StopCoroutine(countdownCoroutine);
        }
    }
}