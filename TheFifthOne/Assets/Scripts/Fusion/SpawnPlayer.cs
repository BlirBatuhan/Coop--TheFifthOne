using UnityEngine;
using Fusion;
using UnityEngine.SceneManagement;
using Fusion.Sockets;
using System.Collections.Generic;
using System;

public class SpawnPlayer : MonoBehaviour, INetworkRunnerCallbacks
{
    private NetworkRunner networkRunner;
    [SerializeField] public GameObject PlayerPrefab;

    
    [SerializeField] private SessionListUIhandler sessionListUI;
    [SerializeField] private GameObject lobbyUI;
    [SerializeField] private GameObject gameUI;

    // Oyuncularýn karakterlerini tutmak için bir sözlük
    private Dictionary<PlayerRef, NetworkObject> spawnedCharacters = new Dictionary<PlayerRef, NetworkObject>();

    private void Start()
    {
        
        if (lobbyUI != null) lobbyUI.SetActive(true);
        if (gameUI != null) gameUI.SetActive(false);
    }

   
    public async void CreateRoom(string roomName, int maxPlayers = 4)
    {
        networkRunner = gameObject.GetComponent<NetworkRunner>();
        if (networkRunner == null)
        {
            networkRunner = gameObject.AddComponent<NetworkRunner>();
        }

        networkRunner.ProvideInput = true;

        var scene = SceneRef.FromIndex(SceneManager.GetActiveScene().buildIndex);
        var sceneInfo = new NetworkSceneInfo();
        if (scene.IsValid)
        {
            sceneInfo.AddSceneRef(scene, LoadSceneMode.Additive);
        }

        

        await networkRunner.StartGame(new StartGameArgs()
        {
            GameMode = GameMode.Shared, // Shared mode kullan
            SessionName = roomName,
            PlayerCount = maxPlayers,
            Scene = scene,
            SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>()
           
        });

        Debug.Log($"Room created: {roomName} with {maxPlayers} max players");
    }

    
    public async void JoinRoom(SessionInfo sessionInfo)
    {
        networkRunner = gameObject.GetComponent<NetworkRunner>();
        if (networkRunner == null)
        {
            networkRunner = gameObject.AddComponent<NetworkRunner>();
        }

        networkRunner.ProvideInput = true;

        await networkRunner.StartGame(new StartGameArgs()
        {
            GameMode = GameMode.Shared,
            SessionName = sessionInfo.Name,
            Scene = SceneRef.FromIndex(SceneManager.GetActiveScene().buildIndex),
            SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>()
        });

        Debug.Log($"Joining room: {sessionInfo.Name}");
    }

    
    // SessionListUIhandler'dan çaðrýlacak
    public void OnJoinSessionRequested(SessionInfo sessionInfo)
    {
        JoinRoom(sessionInfo);
    }

    // Test için - Editor'da kullan
    private void OnGUI()
    {
        if (networkRunner == null || !networkRunner.IsRunning)
        {
            if (GUI.Button(new Rect(0, 0, 200, 40), "Create Test Room"))
            {
                CreateRoom("TestRoom", 4);
            }

            if (GUI.Button(new Rect(0, 50, 200, 40), "Manual Test"))
            {
                // Test için manuel session list çaðrýsý
                OnSessionListUpdated(null, new List<SessionInfo>());
            }
        }
        else
        {
            if (GUI.Button(new Rect(0, 0, 200, 40), "Leave Room"))
            {
                LeaveRoom();
            }
        }
    }

    public async void LeaveRoom()
    {
        if (networkRunner != null && networkRunner.IsRunning)
        {
            await networkRunner.Shutdown();

            // UI'larý eski haline getir
            if (lobbyUI != null) lobbyUI.SetActive(true);
            if (gameUI != null) gameUI.SetActive(false);
        }
    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
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
                    
                    bool canJoin = session.IsOpen && session.PlayerCount < session.MaxPlayers;

                    if (canJoin)
                    {
                        sessionListUI.AddToList(session);
                    }
                }
            }
        }
    }

    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        NetworkInputData data = new NetworkInputData
        {
            move = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")),
            jump = Input.GetButtonDown("Jump"),
            crouch = Input.GetKey(KeyCode.LeftControl),
            run = Input.GetKey(KeyCode.LeftShift),
        };
        input.Set(data);
    }

    
    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
    {
        Debug.LogError($"Connection failed: {reason}");
    }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
    {
        Debug.Log($"Disconnected: {reason}");
        // Lobby'ye geri dön
        if (lobbyUI != null) lobbyUI.SetActive(true);
        if (gameUI != null) gameUI.SetActive(false);
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
        Debug.Log($"Runner shutdown: {shutdownReason}");
    }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
}