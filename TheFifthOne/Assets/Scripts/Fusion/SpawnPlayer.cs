using UnityEngine;
using Fusion;
using UnityEngine.SceneManagement;
using Fusion.Sockets;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;

public class SpawnPlayer : MonoBehaviour, INetworkRunnerCallbacks
{
    private NetworkRunner networkRunner;
    private NetworkRunner lobbyRunner; 

    [SerializeField] public GameObject PlayerPrefab;
    [SerializeField] private SessionListUIhandler sessionListUI;
    [SerializeField] private GameObject lobbyUI;
    [SerializeField] private GameObject gameUI;

    // Oyuncularýn karakterlerini tutmak için bir sözlük
    private Dictionary<PlayerRef, NetworkObject> spawnedCharacters = new Dictionary<PlayerRef, NetworkObject>();

    private async void Start()
    {
        if (lobbyUI != null) lobbyUI.SetActive(true);
        if (gameUI != null) gameUI.SetActive(false);

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

        
        networkRunner = gameObject.GetComponent<NetworkRunner>();
        if (networkRunner == null)
        {
            networkRunner = gameObject.AddComponent<NetworkRunner>();
        }

        networkRunner.ProvideInput = true;
        networkRunner.AddCallbacks(this); // Callback'leri ekle

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

            Debug.Log($"Room created: {roomName} with {maxPlayers} max players");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to create room: {e.Message}");
           
            lobbyUI.SetActive(true);
            gameUI.SetActive(false);
            
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
            
            await StartSessionDiscovery();
        }
    }

    // SessionListUIhandler'dan çaðrýlacak
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

        
        await StartSessionDiscovery();
    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        
        if (runner != networkRunner) return;

        Debug.Log($"Player joined: {player}");

        if (lobbyUI != null) lobbyUI.SetActive(false);
        if (gameUI != null) gameUI.SetActive(true);

        // Shared modda her client kendi karakterini spawn eder
        if (player == runner.LocalPlayer)
        {
            if (PlayerPrefab == null)
            {
                Debug.LogError("PlayerPrefab is null!");
                return;
            }

            Vector3 playerPos = new Vector3(
                (player.RawEncoded % runner.Config.Simulation.PlayerCount) * 3,
                1f,
                0f
            );

            Debug.Log($"Spawning local player at position: {playerPos}");

            NetworkObject networkObject = runner.Spawn(
                PlayerPrefab,
                playerPos,
                Quaternion.identity,
                player
            );

            if (networkObject != null)
            {
                spawnedCharacters.Add(player, networkObject);
                Debug.Log("Player spawned successfully!");
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

    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
    {
        
        if (runner != lobbyRunner) return;

        Debug.Log($"Session list updated: {sessionList.Count} sessions found");

        // Oda listesi güncellendiðinde UI'ý güncelle
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

    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }

    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
    {
        Debug.Log($"Disconnected: {reason}");

        if (runner == networkRunner)
        {
            // Game session'dan çýkýldý, lobby'ye geri dön
            if (lobbyUI != null) lobbyUI.SetActive(true);
            if (gameUI != null) gameUI.SetActive(false);

            // Session discovery'yi yeniden baþlat
            _ = StartSessionDiscovery(); // discard pattern 
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