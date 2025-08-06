using Fusion;
using UnityEngine;

public class MyCam : NetworkBehaviour
{
    float MouseX;
    float MouseY;
    public Transform Body;  // Vücut objesi
    public Transform Head;  // Kamera objesi veya ba? objesi
    public Camera camera;
    
    private bool isLocalPlayer;
    public static MyCam instance;

    [Networked] public bool IsWaiting { get; set; } = true;

    public float Angle;

    public void Awake()
    {


        if (camera != null)
        {
            camera.enabled = false; 
        }
    }
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_DisableLobbyCamera()
    {
        GameObject cam = GameObject.Find("LobbyCamera");
        if (cam != null)
        {
            cam.SetActive(false);
            Debug.Log("LobbyCamera deactivated on client: " + Object.InputAuthority);
        }
    }
    public void SetWaitingMode(bool waiting)
    {
        IsWaiting = waiting;

        isLocalPlayer = Object.HasInputAuthority;

        if (isLocalPlayer)
        {
            // ODA BEKLEME ALANI  DÜZENLENECEK
            /* Sadece yerel oyuncu için mouse'u kilitle
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;*/

            // Kamerayý aktif et
            if (camera != null && !IsWaiting)
            {
                camera.enabled = true;
                
                // AudioListener kontrolü
                AudioListener audioListener = camera.GetComponent<AudioListener>();
                if (audioListener != null)
                {
                    audioListener.enabled = true;
                }
            }
        }
        else
        {
            // Diðer oyuncularýn kameralarýný kapat
            if (camera != null)
            {
                camera.enabled = false;

                // AudioListener'ý da kapat
                AudioListener audioListener = camera.GetComponent<AudioListener>();
                if (audioListener != null)
                {
                    audioListener.enabled = false;
                }
            }
        }

        Debug.Log($"Player {Object.InputAuthority} waiting mode: {waiting}");
    }
    public override void Spawned()
    {

        SetWaitingMode(true);

    }

    void LateUpdate()
    {
        if (!isLocalPlayer) return;

        MouseX = Input.GetAxis("Mouse X") * 100 * Time.deltaTime;
        Body.Rotate(Vector3.up, MouseX);

        MouseY = Input.GetAxis("Mouse Y") * 100 * Time.deltaTime;
        Angle -= MouseY;
        Angle = Mathf.Clamp(Angle, -30, 45);
        Head.localRotation = Quaternion.Euler(Angle, 0, 0);


    }


}