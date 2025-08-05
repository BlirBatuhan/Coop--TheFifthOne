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
    [SerializeField] private GameObject odaKurUI;
    [SerializeField] private GameObject roomLobbyUI; // Oda içi lobby ekraný

    [Header("Room UI Elements")]
    [SerializeField] private TextMeshProUGUI roomNameText;
    [SerializeField] private TextMeshProUGUI playerCountText;

    // Room state management
    private bool isRoomHost = false;
    private string currentRoomName = "";
    private List<PlayerRef> playersInRoom = new List<PlayerRef>();

    // Oyuncularýn karakterlerini tutmak için bir sözlük
    private Dictionary<PlayerRef, NetworkObject> spawnedCharacters = new Dictionary<PlayerRef, NetworkObject>();

    private async void Start()
    {
        if (lobbyUI != null) lobbyUI.SetActive(true);
        if (gameUI != null) gameUI.SetActive(false);
        if (roomLobbyUI != null) roomLobbyUI.SetActive(false);

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
        Debug.Log($"Creating room: {roomName}");

        // Lobby runner'ý KAPAT (sadece oda oluþtururken)
        if (lobbyRunner != null && lobbyRunner.IsRunning)
        {
            await lobbyRunner.Shutdown();
        }

        // UI'larý güncelle - Room Lobby'ye geç
        if (lobbyUI != null) lobbyUI.SetActive(false);
        if (odaKurUI != null) odaKurUI.SetActive(false);
        if (gameUI != null) gameUI.SetActive(false);
        if (roomLobbyUI != null) roomLobbyUI.SetActive(true); // Oda içi lobby

        networkRunner = gameObject.GetComponent<NetworkRunner>();
        if (networkRunner == null)
        {
            networkRunner = gameObject.AddComponent<NetworkRunner>();
        }

        networkRunner.ProvideInput = true;
        networkRunner.AddCallbacks(this);

        var scene = SceneRef.FromIndex(SceneManager.GetActiveScene().buildIndex);

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

            // Host olarak ayarla
            isRoomHost = true;
            currentRoomName = roomName;
            Debug.Log($"Room created: {roomName} with {maxPlayers} max players - You are the HOST");

            // Room lobby UI'ýný güncelle
            UpdateRoomLobbyUI();

        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to create room: {e.Message}");
            // Hata durumunda ana lobby'ye dön
            ResetToMainLobby();
        }
    }

    public async void JoinRoom(SessionInfo sessionInfo)
    {
        Debug.Log($"Joining room: {sessionInfo.Name}");

        // Lobby runner'ý KAPAT
        if (lobbyRunner != null && lobbyRunner.IsRunning)
        {
            await lobbyRunner.Shutdown();
        }

        // UI'larý güncelle - Room Lobby'ye geç
        if (lobbyUI != null) lobbyUI.SetActive(false);
        if (gameUI != null) gameUI.SetActive(false);
        if (roomLobbyUI != null) roomLobbyUI.SetActive(true);

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

            // Guest olarak ayarla
            isRoomHost = false;
            currentRoomName = sessionInfo.Name;
            Debug.Log($"Joined room: {sessionInfo.Name} - You are a GUEST");

            // Room lobby UI'ýný güncelle
            UpdateRoomLobbyUI();

        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to join room: {e.Message}");
            ResetToMainLobby();
        }
    }
    // bu oyunu baþlatmak için kullanýlan fonksiyon
    public void StartGame()
    {
        if (!isRoomHost)
        {
            Debug.LogWarning("Only the host can start the game!");
            return;
        }

        if (networkRunner == null || !networkRunner.IsRunning)
        {
            Debug.LogError("No active room to start game!");
            return;
        }

        Debug.Log("Host is starting the game...");

        // DOÐRU: Fusion'da RPC'yi böyle çaðýrmalýsýn
        StartGameRPC();
    }


    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void StartGameRPC()
    {
        Debug.Log("Game started by host!");

        // Tüm client'larda UI deðiþimi
        if (roomLobbyUI != null) roomLobbyUI.SetActive(false);
        if (gameUI != null) gameUI.SetActive(true);

        // Oyuncularý spawn et
        Invoke("SpawnAllPlayersFromDictionary", 1f);
    }

    private void SpawnAllPlayersFromDictionary()
    {
        // Sadece host (StateAuthority sahibi) spawn iþlemini yapabilir
        if (!isRoomHost)
        {
            Debug.Log("Only host can spawn players");
            return;
        }

        Debug.Log($"Host spawning players from dictionary. Players in room: {playersInRoom.Count}");

        // Sözlükteki tüm oyuncularý spawn et
        for (int i = 0; i < playersInRoom.Count; i++)
        {
            var player = playersInRoom[i];

            if (!spawnedCharacters.ContainsKey(player))
            {
                Debug.Log($"Spawning player from dictionary: {player}");
                SpawnCharacter(player);
            }
            else
            {
                Debug.Log($"Player {player} already spawned, skipping");
            }
        }

        Debug.Log($"Spawn completed. Total spawned characters: {spawnedCharacters.Count}");
    }

    private void SpawnCharacter(PlayerRef player)
    {
        if (PlayerPrefab == null)
        {
            Debug.LogError("PlayerPrefab is null!");
            return;
        }

        if (spawnedCharacters.ContainsKey(player))
        {
            Debug.LogWarning($"Player {player} already spawned!");
            return;
        }

        // Pozisyonu hesapla
        Vector3 playerPos = new Vector3(
            (player.RawEncoded % 4) * 3, // Max 4 oyuncu için
            1f,
            0f
        );

        Debug.Log($"Spawning player {player} at position: {playerPos}");

        try
        {
            NetworkObject networkObject = networkRunner.Spawn(
                PlayerPrefab,
                playerPos,
                Quaternion.identity,
                player // Bu parametreyi input authority olarak ayarlar
            );

            if (networkObject != null)
            {
                spawnedCharacters.Add(player, networkObject);
                Debug.Log($"? Player {player} spawned successfully! Total spawned: {spawnedCharacters.Count}");
            }
            else
            {
                Debug.LogError($"? Failed to spawn player {player} - NetworkObject is null!");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"? Exception while spawning player {player}: {e.Message}");
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

        // Ana lobby'ye dön
        ResetToMainLobby();
    }

    private async void ResetToMainLobby()
    {
        // State'i sýfýrla
        isRoomHost = false;
        currentRoomName = "";
        playersInRoom.Clear();
        spawnedCharacters.Clear();

        // UI'larý sýfýrla
        if (lobbyUI != null) lobbyUI.SetActive(true);
        if (gameUI != null) gameUI.SetActive(false);
        if (roomLobbyUI != null) roomLobbyUI.SetActive(false);
        if (odaKurUI != null) odaKurUI.SetActive(false);

        // Session discovery'yi yeniden baþlat
        await StartSessionDiscovery();
    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        if (runner != networkRunner) return;

        Debug.Log($"Player joined room: {player} (LocalPlayer: {runner.LocalPlayer == player})");

        // Player'ý listeye ekle (sözlüðe)
        if (!playersInRoom.Contains(player))
        {
            playersInRoom.Add(player);
            Debug.Log($"Added player {player} to room list. Total players: {playersInRoom.Count}");

            // Sözlükteki oyuncu listesini yazdýr
            Debug.Log("Current players in room:");
            for (int i = 0; i < playersInRoom.Count; i++)
            {
                Debug.Log($"  - Player {i}: {playersInRoom[i]}");
            }
        }

        // Room lobby UI'ýný güncelle
        UpdateRoomLobbyUI();

        // Eðer oyun zaten baþlamýþsa ve host ise, yeni player'ý spawn et
        if (gameUI != null && gameUI.activeInHierarchy && isRoomHost)
        {
            if (!spawnedCharacters.ContainsKey(player))
            {
                Debug.Log($"Game already started, spawning new player: {player}");
                SpawnCharacter(player);
            }
        }
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        if (runner != networkRunner) return;

        Debug.Log($"Player left room: {player}");

        // Player'ý listeden çýkar
        playersInRoom.Remove(player);

        // Spawn edilmiþ karakteri temizle
        if (spawnedCharacters.TryGetValue(player, out NetworkObject networkObject))
        {
            if (networkObject != null)
            {
                runner.Despawn(networkObject);
            }
            spawnedCharacters.Remove(player);
        }

        // Room lobby UI'ýný güncelle
        UpdateRoomLobbyUI();
    }

    private void UpdateRoomLobbyUI()
    {
        // Room lobby UI bileþenlerini güncelle
        roomNameText.text = $"{currentRoomName}";
        playerCountText.text = $"{playersInRoom.Count} / {networkRunner.Config.Simulation.PlayerCount}";

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

        NetworkInputData data = new NetworkInputData
        {
            move = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")),
            jump = Input.GetButtonDown("Jump"),
            crouch = Input.GetKey(KeyCode.LeftControl),
            run = Input.GetKey(KeyCode.LeftShift),
        };
        input.Set(data);
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

        /* if (runner == networkRunner)
         {
             // Ana lobby'ye dön
             _ = Task.Run(async () => {
                 await Task.Delay(100); // Kýsa gecikme
                 UnityMainThreadDispatcher.Instance().Enqueue(() => ResetToMainLobby());
             });
         }*/
    }

    public void OdaKur()
    {
        if (lobbyUI != null) lobbyUI.SetActive(false);
        if (gameUI != null) gameUI.SetActive(false);
        if (roomLobbyUI != null) roomLobbyUI.SetActive(false);
        if (odaKurUI != null) odaKurUI.SetActive(true);
    }

    // Diðer callback'ler
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
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