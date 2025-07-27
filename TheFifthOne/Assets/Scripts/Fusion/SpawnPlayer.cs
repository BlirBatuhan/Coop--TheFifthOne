using UnityEngine;
using Fusion;
using UnityEngine.SceneManagement;
using Fusion.Sockets;
using System.Collections.Generic;
using System;

public class SpawnPlayer : MonoBehaviour, INetworkRunnerCallbacks
{
    private NetworkRunner networkRunner;
    [SerializeField] private NetworkPrefabRef networkPrefabPlayer;

    // Oyuncularýn karakterlerini tutmak için bir sözlük
    private Dictionary<PlayerRef,NetworkObject> spawanCharacter = new Dictionary<PlayerRef, NetworkObject>();

    async void GameStart(GameMode mode)
    {
        networkRunner = gameObject.AddComponent<NetworkRunner>();
        networkRunner.ProvideInput = true;

        var scene = SceneRef.FromIndex(SceneManager.GetActiveScene().buildIndex);
        var sceneInfo = new NetworkSceneInfo();
        if(scene.IsValid)
        {
            sceneInfo.AddSceneRef(scene,LoadSceneMode.Additive);
        }

        await networkRunner.StartGame(new StartGameArgs()
        {
            GameMode = mode,
            SessionName = "TestRoom",
            Scene = scene,
            SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>(),
            
        });

    }


    private void OnGUI()
    {
        if (networkRunner == null)
        {
            if (GUI.Button(new Rect(0, 0, 200, 40), "Host"))
            {
                GameStart(GameMode.Host);
            }
            if (GUI.Button(new Rect(0, 40, 200, 40), "Join"))
            {
                GameStart(GameMode.Client);
            }
        }
    }


    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        if(runner.IsServer)
        {
            Vector3 playerPos = new Vector3((player.RawEncoded % runner.Config.Simulation.PlayerCount) * 3,1f,0f);

            NetworkObject networkObject = runner.Spawn(networkPrefabPlayer, playerPos, Quaternion.identity, player);

            spawanCharacter.Add(player, networkObject);
        }
    }


    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {

        if(spawanCharacter.TryGetValue(player,out NetworkObject networkObject))
        {
            runner.Despawn(networkObject);
            spawanCharacter.Remove(player);
        }

    }




    public void OnConnectedToServer(NetworkRunner runner)
    {
        
    }

    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
    {
        
    }

    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token)
    {
      
    }

    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data)
    {
        
    }

    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
    {
        
    }

    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken)
    {
        
    }

    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        
    }

    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input)
    {
        
    }

    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
    {
                        
    }

    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
    {
        
    }



    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress)
    {
        
    }

    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data)
    {
        
    }

    public void OnSceneLoadDone(NetworkRunner runner)
    {
        
    }

    public void OnSceneLoadStart(NetworkRunner runner)
    {
       
    }

    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
    {
        
    }

    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        
    }

    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message)
    {
        
    }
}
