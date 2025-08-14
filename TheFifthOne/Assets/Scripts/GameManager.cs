using UnityEngine;
using Fusion;
using System.Collections;
using System.Linq;
using TMPro;

public class GameManager : NetworkBehaviour
{
       
    public GameObject kup;
    private GameObject Canvas;



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
        Debug.Log("Kamera Kapatýldý");

       foreach(var player in SpawnPlayer.Instance.spawnedCharacters)
        {
          Camera playerCamera = player.Value.GetComponentInChildren<Camera>();
            playerCamera.GetComponent<MyCam>().oyundaMý = false;
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
            if(Input.GetKey(KeyCode.L))
            {
                KameraKapatRpc();
            }
        }
    }
}