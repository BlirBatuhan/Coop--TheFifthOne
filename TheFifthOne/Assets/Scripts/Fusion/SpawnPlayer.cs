using UnityEngine;
using Fusion;
using UnityEngine.SceneManagement;
using Fusion.Sockets;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;
using TMPro;

public class SpawnPlayer : MonoBehaviour, INetworkRunnerCallbacks
{
    private NetworkRunner networkRunner;
    private NetworkRunner lobbyRunner;

    [SerializeField] public GameObject PlayerPrefab;
    [SerializeField] private SessionListUIhandler sessionListUI;
    [SerializeField] private GameObject lobbyUI;
    [SerializeField] private GameObject gameUI;
    [SerializeField] private GameObject roomUI;
    [SerializeField] private GameObject startGameButton; // Host için start butonu
    [SerializeField] private Camera lobbyCamera;

    // Bekleme alaný spawn pozisyonlarý
    [SerializeField] private Transform[] waitingAreaSpawnPoints;
    [SerializeField] private Vector3 defaultWaitingPosition = new Vector3(0, 1f, 0);

    [Header("Room UI Elements")]
    [SerializeField] private TextMeshProUGUI roomNameText;
    [SerializeField] private TextMeshProUGUI playerCountText;
    private string currentRoomName = "";

    // Oyun durumu - sadece yerel kontrol
    private bool gameStarted = false;
    private bool isHost = false;

    // Oyuncularýn karakterlerini tutmak için bir sözlük
    private Dictionary<PlayerRef, NetworkObject> spawnedCharacters = new Dictionary<PlayerRef, NetworkObject>();

    private async void Start()
    {
        if (lobbyUI != null) lobbyUI.SetActive(true);
        if (gameUI != null) gameUI.SetActive(false);
        if (startGameButton != null) startGameButton.SetActive(false);

        // Session discovery'yi baþlat
        await StartSessionDiscovery();
    }

    private async Task StartSessionDiscovery()
    {
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

    public async void RefreshSessionList()
    {
        if (lobbyRunner != null && lobbyRunner.IsRunning)
        {
            await lobbyRunner.Shutdown();
            await Task.Delay(500);
        }

        await StartSessionDiscovery();
    }

    public async void CreateRoom(string roomName, int maxPlayers = 4)
    {
        if (lobbyRunner != null && lobbyRunner.IsRunning)
        {
            await lobbyRunner.Shutdown();
        }

        lobbyUI.SetActive(false);
        gameUI.SetActive(true);
        roomUI.SetActive(false);
        isHost = true; // Oda kuruyoruz, host oluyoruz

        networkRunner = gameObject.GetComponent<NetworkRunner>();
        if (networkRunner == null)
        {
            networkRunner = gameObject.AddComponent<NetworkRunner>();
        }

        networkRunner.ProvideInput = true;
        networkRunner.AddCallbacks(this);

        var scene = SceneRef.FromIndex(SceneManager.GetActiveScene().buildIndex);
        var sceneInfo = new NetworkSceneInfo();
        if (scene.IsValid)
        {
            sceneInfo.AddSceneRef(scene, LoadSceneMode.Additive);
        }

        try
        {
            await networkRunner.StartGame(new StartGameArgs()
            {
                GameMode = GameMode.Shared,
                SessionName = roomName,
                PlayerCount = maxPlayers,
                Scene = scene,
                SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>(),
                IsVisible = true,
                IsOpen = true
            });
            currentRoomName = roomName;
            UpdateRoomLobbyUI();

            Debug.Log($"Room created: {roomName} with {maxPlayers} max players");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to create room: {e.Message}");
            lobbyUI.SetActive(true);
            gameUI.SetActive(false);
            isHost = false;
            await StartSessionDiscovery();
        }
    }

    public async void JoinRoom(SessionInfo sessionInfo)
    {
        if (lobbyRunner != null && lobbyRunner.IsRunning)
        {
            await lobbyRunner.Shutdown();
        }

        lobbyUI.SetActive(false);
        gameUI.SetActive(true);
        isHost = false; // Odaya katýlýyoruz, host deðiliz

        networkRunner = gameObject.GetComponent<NetworkRunner>();
        if (networkRunner == null)
        {
            networkRunner = gameObject.AddComponent<NetworkRunner>();
        }

        networkRunner.ProvideInput = true;
        networkRunner.AddCallbacks(this);

        try
        {
            await networkRunner.StartGame(new StartGameArgs()
            {
                GameMode = GameMode.Shared,
                SessionName = sessionInfo.Name,
                Scene = SceneRef.FromIndex(SceneManager.GetActiveScene().buildIndex),
                SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>()
            });

            Debug.Log($"Joined room: {sessionInfo.Name}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to join room: {e.Message}");
            lobbyUI.SetActive(true);
            gameUI.SetActive(false);
            isHost = false;
            await StartSessionDiscovery();
        }
    }

    // Host'un oyunu baþlatmasý için
    public void StartGame()
    {
        if (isHost)
        {
            gameStarted = true;

            // Start butonunu gizle
            if (startGameButton != null)
                startGameButton.SetActive(false);

            // Lobby kamerasýný kapat
            if (lobbyCamera != null)
                lobbyCamera.gameObject.SetActive(false);

            // Tüm oyuncularý aktif et
            ActivateAllPlayers();

            Debug.Log("Host started the game!");
        }
    }

    private void ActivateAllPlayers()
    {
        foreach (var kvp in spawnedCharacters)
        {
            if (kvp.Value != null)
            {
                MyCam camcontroller = kvp.Value.GetComponent<MyCam>();
                if (camcontroller != null)
                {
                    camcontroller.SetWaitingMode(false);

                    // Sadece local player'ýn kamerasýný aktif et
                    if (kvp.Key == networkRunner.LocalPlayer)
                    {
                        Camera playerCamera = kvp.Value.GetComponentInChildren<Camera>();
                        if (playerCamera != null)
                        {
                            playerCamera.gameObject.SetActive(true);
                        }
                    }
                }
            }
        }
    }

    public void OnJoinSessionRequested(SessionInfo sessionInfo)
    {
        JoinRoom(sessionInfo);
    }

    public async void LeaveRoom()
    {
        if (networkRunner != null && networkRunner.IsRunning)
        {
            await networkRunner.Shutdown();
        }

        if (lobbyUI != null) lobbyUI.SetActive(true);
        if (gameUI != null) gameUI.SetActive(false);

        gameStarted = false;
        isHost = false;

        await StartSessionDiscovery();
    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        if (runner != networkRunner) return;

        Debug.Log($"Player joined: {player}");

        if (lobbyUI != null) lobbyUI.SetActive(false);
        if (gameUI != null) gameUI.SetActive(true);

        // Host ise start butonunu göster
        if (isHost && startGameButton != null)
        {
            startGameButton.SetActive(true);
        }

        // Her oyuncu kendi karakterini bekleme alanýnda spawn eder
        if (player == runner.LocalPlayer)
        {
            if (PlayerPrefab == null)
            {
                Debug.LogError("PlayerPrefab is null!");
                return;
            }

            // Bekleme alanýnda spawn pozisyonu belirle
            Vector3 waitingPos = GetWaitingAreaPosition(player);

            Debug.Log($"Spawning player at waiting area position: {waitingPos}");

            NetworkObject networkObject = runner.Spawn(
                PlayerPrefab,
                waitingPos,
                Quaternion.identity,
                player
            );

            if (networkObject != null)
            {
                spawnedCharacters.Add(player, networkObject);

                // Player controller'ý al ve bekleme moduna al
                MyCam camController = networkObject.GetComponent<MyCam>();
                if (camController != null)
                {
                    camController.SetWaitingMode(true);
                }

                Debug.Log("Player spawned in waiting area!");
            }
            else
            {
                Debug.LogError("Failed to spawn player!");
            }
        }
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
    }

    private Vector3 GetWaitingAreaPosition(PlayerRef player)
    {
        // Eðer spawn noktalarý tanýmlanmýþsa onlarý kullan
        if (waitingAreaSpawnPoints != null && waitingAreaSpawnPoints.Length > 0)
        {
            int index = player.RawEncoded % waitingAreaSpawnPoints.Length;
            return waitingAreaSpawnPoints[index].position;
        }

        // Yoksa default pozisyondan hareketle daðýt
        return defaultWaitingPosition + new Vector3(player.RawEncoded * 2f, 0, 0);
    }

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
                        Debug.Log($"Added session to list: {session.Name} ({session.PlayerCount}/{session.MaxPlayers})");
                    }
                }
            }
        }
    }

    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        if (runner != networkRunner) return;

        // Sadece oyun baþladýysa input al
        if (!gameStarted) return;

        NetworkInputData data = new NetworkInputData
        {
            move = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")),
            jump = Input.GetButtonDown("Jump"),
            crouch = Input.GetKey(KeyCode.LeftControl),
            run = Input.GetKey(KeyCode.LeftShift),
        };
        input.Set(data);
    }


    public void onCreateRoom()
    {
        if(lobbyUI != null) lobbyUI.SetActive(false);
        if(roomUI != null) roomUI.SetActive(true);
    }

    private void UpdateRoomLobbyUI()
    {
        // Room lobby UI bile?enlerini güncelle
        roomNameText.text = $"{currentRoomName}";
        playerCountText.text = $"{spawnedCharacters.Count} / {networkRunner.Config.Simulation.PlayerCount}";

    }


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

    // Diðer callback'ler
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

    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }

    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
    {
        Debug.Log($"Disconnected: {reason}");

        if (runner == networkRunner)
        {
            if (lobbyUI != null) lobbyUI.SetActive(true);
            if (gameUI != null) gameUI.SetActive(false);

            gameStarted = false;
            isHost = false;

            _ = StartSessionDiscovery();
        }
    }

    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }

    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        Debug.Log($"Runner shutdown: {shutdownReason} - Runner: {(runner == lobbyRunner ? "Lobby" : "Game")}");
    }

    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
}