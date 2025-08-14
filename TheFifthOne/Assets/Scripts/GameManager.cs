using UnityEngine;
using Fusion;
using System.Collections;
using System.Linq;
using TMPro;
using System;

public class GameManager : NetworkBehaviour
{
       
    public GameObject kup;
    private GameObject Canvas;
    [SerializeField] private Camera lobbyCamera;



    [Rpc(RpcSources.StateAuthority,RpcTargets.All)]
   public void KupCikarRpc()
    {
        Instantiate(kup,new Vector3(0,0,0), Quaternion.identity);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void CanvasCýkarRpc()
    {
        Canvas = GameObject.FindWithTag("Canvas");
        Canvas.SetActive(false);

    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void KameraKapatRpc()
    {

        foreach (var player in SpawnPlayer.Instance.spawnedCharacters)
        {
            if (player.Value == Object.HasStateAuthority)
            {
                lobbyCamera.enabled = false;
                lobbyCamera.GetComponent<AudioListener>().enabled = false;

                Camera playerCamera = player.Value.GetComponentInChildren<Camera>();
                playerCamera.GetComponent<MyCam>().oyundaMý = false;
                Debug.Log($"Kamera kapatýldý: {player.Value}");
            }
        }
    }


    public override void FixedUpdateNetwork()
    {
        if(Object.HasStateAuthority)
        {
            if(Input.GetKey(KeyCode.K))
            {
                KupCikarRpc();
            }
            if(Input.GetKey(KeyCode.C))
            {
                CanvasCýkarRpc();
            }
            if(Input.GetKeyDown(KeyCode.L))
            {
                KameraKapatRpc();
            }
        }
    }
}