using UnityEngine;
using Fusion;
using UnityEngine.SceneManagement;
using Fusion.Sockets;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;
using TMPro;

public enum GameState
{
    Lobby,
    WaitingRoom,
    InGame
}

public class SpawnPlayer : MonoBehaviour, INetworkRunnerCallbacks
{
    private NetworkRunner networkRunner;
    private NetworkRunner lobbyRunner;
    [SerializeField] private NetworkObject gameManagerPrefab;
    private bool gameManagerSpawned = false;

    [SerializeField] public GameObject PlayerPrefab;
    [SerializeField] private SessionListUIhandler sessionListUI;
    [SerializeField] private GameObject lobbyUI;
    [SerializeField] private GameObject gameUI;
    [SerializeField] private GameObject roomUI;
    [SerializeField] private GameObject inRoomUI;
    [SerializeField] private GameObject startGameButton;

    // Scene referanslarý
    [SerializeField] private int waitingRoomSceneIndex = 1; // Bekleme alaný scene'i
    [SerializeField] private int gameSceneIndex = 2; // Oyun scene'i

    // Bekleme alaný spawn pozisyonlarý
    [SerializeField] private Transform[] waitingAreaSpawnPoints;
    [SerializeField] private Vector3 defaultWaitingPosition = new Vector3(0, 1f, 0);

    [Header("Room UI Elements")]
    [SerializeField] private TextMeshProUGUI roomNameText;
    [SerializeField] private TextMeshProUGUI playerCountText;
    private string currentRoomName = "";

    // Game State Management
    private GameState currentGameState = GameState.Lobby;
    private bool isHost = false;

    // Oyuncularýn karakterlerini tutmak için
    private Dictionary<PlayerRef, NetworkObject> spawnedCharacters = new Dictionary<PlayerRef, NetworkObject>();

    private async void Start()
    {
        DontDestroyOnLoad(this.gameObject);
        SetGameState(GameState.Lobby);
        await StartSessionDiscovery();
    }

    private void SetGameState(GameState newState)
    {
        currentGameState = newState;
        UpdateUIBasedOnGameState();
        Debug.Log($"Game state changed to: {currentGameState}");
    }

    private void UpdateUIBasedOnGameState()
    {
        // Tüm UI'larý kapat
        if (lobbyUI != null) lobbyUI.SetActive(false);
        if (gameUI != null) gameUI.SetActive(false);
        if (roomUI != null) roomUI.SetActive(false);
        if (inRoomUI != null) inRoomUI.SetActive(false);
        if (startGameButton != null) startGameButton.SetActive(false);

        // Current state'e göre UI'larý aç
        switch (currentGameState)
        {
            case GameState.Lobby:
                if (lobbyUI != null) lobbyUI.SetActive(true);
                break;
            case GameState.WaitingRoom:
                if (inRoomUI != null) inRoomUI.SetActive(true);
                if (isHost && startGameButton != null) startGameButton.SetActive(true);
                break;
            case GameState.InGame:
                if (gameUI != null) gameUI.SetActive(true);
                break;
        }
    }

    private async Task StartSessionDiscovery()
    {
        if (lobbyRunner != null && lobbyRunner.IsRunning)
        {
            await lobbyRunner.Shutdown();
        }

        GameObject lobbyObject = new GameObject("LobbyRunner");
        lobbyObject.transform.SetParent(this.transform);
        lobbyRunner = lobbyObject.AddComponent<NetworkRunner>();
        lobbyRunner.AddCallbacks(this);

        try
        {
            await lobbyRunner.JoinSessionLobby(SessionLobby.Shared);
            Debug.Log("Session discovery started successfully");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to start session discovery: {e.Message}");
        }
    }

    public async void CreateRoom(string roomName, int maxPlayers = 4)
    {
        if (lobbyRunner != null && lobbyRunner.IsRunning)
        {
            await lobbyRunner.Shutdown();
        }

        isHost = true;
        SetGameState(GameState.WaitingRoom);

        await StartNetworkRunner(roomName, maxPlayers, true);
    }

    public async void JoinRoom(SessionInfo sessionInfo)
    {
        if (lobbyRunner != null && lobbyRunner.IsRunning)
        {
            await lobbyRunner.Shutdown();
        }

        isHost = false;
        SetGameState(GameState.WaitingRoom);

        await StartNetworkRunner(sessionInfo.Name, 4, false);
    }

    private async Task StartNetworkRunner(string sessionName, int maxPlayers, bool isCreating)
    {
        networkRunner = gameObject.GetComponent<NetworkRunner>();
        if (networkRunner == null)
        {
            networkRunner = gameObject.AddComponent<NetworkRunner>();
        }

        networkRunner.ProvideInput = (currentGameState == GameState.InGame);
        networkRunner.AddCallbacks(this);

        // Bekleme alaný scene'inde baþlat
        var scene = SceneRef.FromIndex(waitingRoomSceneIndex);
        currentRoomName = sessionName;

        try
        {
            await networkRunner.StartGame(new StartGameArgs()
            {
                GameMode = GameMode.Shared,
                SessionName = sessionName,
                PlayerCount = maxPlayers,
                Scene = scene,
                SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>(),
                IsVisible = true,
                IsOpen = true
            });

            Debug.Log($"Network runner started in waiting room: {sessionName}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to start network runner: {e.Message}");
            SetGameState(GameState.Lobby);
            await StartSessionDiscovery();
        }
    }

    // Host tarafýndan oyunu baþlat
    public void StartGame()
    {
        if (!isHost || currentGameState != GameState.WaitingRoom) return;

        Debug.Log("Host requesting game start via GameManager");

        // GameManager'a oyun baþlatma isteði gönder
        GameManager gm = FindObjectOfType<GameManager>();
        if (gm != null)
        {
            gm.StartGame(); // GameManager countdown'u baþlatacak
        }
        else
        {
            Debug.LogError("GameManager not found!");
        }
    }

    // GameManager tarafýndan çaðrýlýr
    public async void TransitionToGameScene()
    {
        if (!isHost) return;

        Debug.Log("Transitioning to game scene...");

        // Game state'i deðiþtir
        SetGameState(GameState.InGame);

        // Input handling'i aktif et
        networkRunner.ProvideInput = true;

        // Oyun scene'ine geç
        var gameScene = SceneRef.FromIndex(gameSceneIndex);

        try
        {
            // Scene geçiþi için NetworkSceneManager kullan
            await networkRunner.LoadScene(gameScene);
            Debug.Log("Successfully transitioned to game scene");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to load game scene: {e.Message}");
        }
    }

    private void NotifyGameManagerStarted()
    {
        GameManager gm = FindObjectOfType<GameManager>();
        if (gm != null)
        {
            gm.gameStarted = true;
            Debug.Log("Game started - GameManager notified!");
        }
    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        if (runner != networkRunner) return;

        Debug.Log($"Player joined: {player} in state: {currentGameState}");

        // GameManager spawn et (sadece bir kez)
        if (isHost && !gameManagerSpawned)
        {
            runner.Spawn(gameManagerPrefab, Vector3.zero, Quaternion.identity);
            gameManagerSpawned = true;
        }

        // Player karakterini spawn et
        if (player == runner.LocalPlayer)
        {
            SpawnPlayerCharacter(player, runner);
        }

        UpdateRoomUI();
    }

    private void SpawnPlayerCharacter(PlayerRef player, NetworkRunner runner)
    {
        if (PlayerPrefab == null)
        {
            Debug.LogError("PlayerPrefab is null!");
            return;
        }

        Vector3 spawnPos;

        // Game state'e göre spawn pozisyonu belirle
        switch (currentGameState)
        {
            case GameState.WaitingRoom:
                spawnPos = GetWaitingAreaPosition(player);
                break;
            case GameState.InGame:
                spawnPos = GetGameSpawnPosition(player);
                break;
            default:
                spawnPos = Vector3.zero;
                break;
        }

        NetworkObject networkObject = runner.Spawn(
            PlayerPrefab,
            spawnPos,
            Quaternion.identity,
            player
        );

        if (networkObject != null)
        {
            spawnedCharacters[player] = networkObject;

            // Player controller'ý ayarla
            ConfigurePlayerController(networkObject, currentGameState);

            Debug.Log($"Player spawned at {spawnPos} in {currentGameState} state");
        }
        else
        {
            Debug.LogError("Failed to spawn player!");
        }
    }

    private void ConfigurePlayerController(NetworkObject playerObject, GameState gameState)
    {
        MyCam camController = playerObject.GetComponentInChildren<MyCam>();
        if (camController == null) return;

        switch (gameState)
        {
            case GameState.WaitingRoom:
                // Bekleme modunda hareket kýsýtlý
                camController.SetWaitingMode(true);
                break;
            case GameState.InGame:
                // Oyun modunda tam kontrol
                camController.SetWaitingMode(false);
                break;
        }
    }

    private Vector3 GetWaitingAreaPosition(PlayerRef player)
    {
        if (waitingAreaSpawnPoints != null && waitingAreaSpawnPoints.Length > 0)
        {
            int index = player.RawEncoded % waitingAreaSpawnPoints.Length;
            return waitingAreaSpawnPoints[index].position;
        }
        return defaultWaitingPosition + new Vector3(player.RawEncoded * 2f, 0, 0);
    }

    private Vector3 GetGameSpawnPosition(PlayerRef player)
    {
        // Oyun spawn pozisyonlarý - bu oyun scene'indeki spawn noktalarýndan alýnabilir
        // Þimdilik basit bir implementasyon
        return new Vector3(
            UnityEngine.Random.Range(-10f, 10f),
            1f,
            UnityEngine.Random.Range(-10f, 10f)
        );
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        if (runner != networkRunner) return;

        Debug.Log($"Player left: {player}");

        if (spawnedCharacters.TryGetValue(player, out NetworkObject networkObject))
        {
            if (networkObject != null)
            {
                runner.Despawn(networkObject);
            }
            spawnedCharacters.Remove(player);
        }

        UpdateRoomUI();
    }

    public void OnSceneLoadDone(NetworkRunner runner)
    {
        Debug.Log($"Scene load done. Current state: {currentGameState}");

        // Eðer oyun scene'ine geçtiyse, mevcut oyuncularý yeniden spawn et
        if (currentGameState == GameState.InGame)
        {
            RespawnAllPlayersInGameScene();
        }
    }

    private void RespawnAllPlayersInGameScene()
    {
        Debug.Log("Respawning all players in game scene");

        // Mevcut karakterleri temizle
        foreach (var kvp in spawnedCharacters)
        {
            if (kvp.Value != null)
            {
                networkRunner.Despawn(kvp.Value);
            }
        }
        spawnedCharacters.Clear();

        // Sadece local player'ý yeniden spawn et (diðerleri kendi callback'lerinde spawn olacak)
        if (networkRunner.LocalPlayer != null)
        {
            SpawnPlayerCharacter(networkRunner.LocalPlayer, networkRunner);
        }
    }

    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        if (runner != networkRunner) return;

        // Sadece oyun durumunda input al
        if (currentGameState != GameState.InGame) return;

        NetworkInputData data = new NetworkInputData
        {
            move = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")),
            jump = Input.GetButtonDown("Jump"),
            crouch = Input.GetKey(KeyCode.LeftControl),
            run = Input.GetKey(KeyCode.LeftShift),
        };
        input.Set(data);
    }

    private void UpdateRoomUI()
    {
        if (roomNameText != null)
            roomNameText.text = currentRoomName;

        if (playerCountText != null)
            playerCountText.text = $"{spawnedCharacters.Count} / {networkRunner?.Config.Simulation.PlayerCount ?? 4}";
    }
    public void BackToLobby()
    {
        if (lobbyUI != null) lobbyUI.SetActive(true);
        if (roomUI != null) roomUI.SetActive(false);
        if (inRoomUI != null) inRoomUI.SetActive(false);
    }
    public async void LeaveRoom()
    {
        if (networkRunner != null && networkRunner.IsRunning)
        {
            await networkRunner.Shutdown();
        }

        SetGameState(GameState.Lobby);
        isHost = false;
        spawnedCharacters.Clear();

        await StartSessionDiscovery();
    }

    // UI Methods
    public void onCreateRoom()
    {
        if (roomUI != null) roomUI.SetActive(true);
        if (lobbyUI != null) lobbyUI.SetActive(false);
    }

    public async void RefreshSessionList()
    {
        if (lobbyRunner != null && lobbyRunner.IsRunning)
        {
            await lobbyRunner.Shutdown();
            await Task.Delay(500);
        }
        await StartSessionDiscovery();
    }

    // Session List Handling
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
    {
        if (runner != lobbyRunner) return;

        Debug.Log($"Session list updated: {sessionList.Count} sessions found");

        if (sessionListUI != null)
        {
            sessionListUI.ClearList();

            if (sessionList.Count == 0)
            {
                sessionListUI.OnNoSessionsFound();
            }
            else
            {
                foreach (var session in sessionList)
                {
                    bool canJoin = session.IsOpen &&
                                   session.PlayerCount < session.MaxPlayers &&
                                   session.IsVisible;

                    if (canJoin)
                    {
                        sessionListUI.AddToList(session);
                    }
                }
            }
        }
    }

    public void OnJoinSessionRequested(SessionInfo sessionInfo)
    {
        JoinRoom(sessionInfo);
    }

    // Cleanup
    private async void OnDestroy()
    {
        if (lobbyRunner != null && lobbyRunner.IsRunning)
        {
            await lobbyRunner.Shutdown();
        }

        if (networkRunner != null && networkRunner.IsRunning)
        {
            await networkRunner.Shutdown();
        }
    }

    #region Other Network Callbacks
    public void OnConnectedToServer(NetworkRunner runner)
    {
        if (runner == lobbyRunner)
        {
            Debug.Log("Connected to lobby");
        }
        else if (runner == networkRunner)
        {
            Debug.Log("Connected to game session");
        }
    }

    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
    {
        Debug.LogError($"Connection failed: {reason}");
    }

    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
    {
        Debug.Log($"Disconnected: {reason}");

        if (runner == networkRunner)
        {
            SetGameState(GameState.Lobby);
            isHost = false;
            _ = StartSessionDiscovery();
        }
    }

    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        Debug.Log($"Runner shutdown: {shutdownReason}");
    }

    // Empty implementations
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    #endregion
}