using Fusion;
using System.Runtime.CompilerServices;
using UnityEngine;

public class MyCam : NetworkBehaviour
{
    float MouseX;
    float MouseY;
    public Transform Body;
    public Transform Head;
    public Camera camera;
    private bool isLocalPlayer;

    private bool previousGameState = false;
    private GameManager gameManager;

    public float Angle;

    public void Awake()
    {
        if (camera != null)
        {
            camera.enabled = false;
        }
    }

    public override void Spawned()
    {
        isLocalPlayer = Object.HasInputAuthority;
        gameManager = FindObjectOfType<GameManager>();

        Debug.Log($"[SPAWN] Player: {Object.InputAuthority}, isLocalPlayer: {isLocalPlayer}");

        if (isLocalPlayer)
        {
            SetupLobbyCamera();
        }

        DisablePlayerCamera();

        // Baþlangýç state'ini kaydet
        if (gameManager != null)
        {
            previousGameState = gameManager.gameStarted;
        }
    }

    void Update()
    {
        // Global game state deðiþikliðini kontrol et
        if (gameManager != null && previousGameState != gameManager.gameStarted)
        {
            Debug.Log($"[GLOBAL STATE CHANGE] GameStarted: {previousGameState} ? {gameManager.gameStarted} for player {Object.InputAuthority}");

            // Sadece yerel oyuncu kamera deðiþikliði yapsýn
            if (isLocalPlayer)
            {
                HandleCameraSwitch(!gameManager.gameStarted); // IsWaiting = !GameStarted
            }

            previousGameState = gameManager.gameStarted;
        }

        // Debug tuþlarý
        if (isLocalPlayer)
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                ToggleMouseLock();
            }
        }
    }

    void SetupLobbyCamera()
    {
        GameObject lobbyCam = GameObject.Find("LobbyCamera");
        if (lobbyCam != null)
        {
            lobbyCam.SetActive(true);

            Camera lobbyCameraComp = lobbyCam.GetComponent<Camera>();
            if (lobbyCameraComp != null)
            {
                lobbyCameraComp.enabled = true;

                AudioListener lobbyAudio = lobbyCameraComp.GetComponent<AudioListener>();
                if (lobbyAudio != null)
                {
                    lobbyAudio.enabled = true;
                }
            }
            Debug.Log("[LOBBY] LobbyCamera setup completed");
        }
    }

    void DisablePlayerCamera()
    {
        if (camera != null)
        {
            camera.enabled = false;

            AudioListener audioListener = camera.GetComponent<AudioListener>();
            if (audioListener != null)
            {
                audioListener.enabled = false;
            }
        }
    }

    void HandleCameraSwitch(bool isWaiting)
    {
        if (!isWaiting)
        {
            // OYUN BAÞLADI - Lobby'den player kamerasýna geç
            Debug.Log("[CAMERA SWITCH] Lobby ? Player camera");

            GameObject lobbyCam = GameObject.Find("LobbyCamera");
            if (lobbyCam != null)
            {
                lobbyCam.SetActive(false);
                Debug.Log("[LOBBY] LobbyCamera deactivated");
            }

            if (camera != null)
            {
                camera.enabled = true;

                AudioListener audioListener = camera.GetComponent<AudioListener>();
                if (audioListener != null)
                {
                    audioListener.enabled = true;
                }

                Debug.Log("[PLAYER CAM] Player camera activated");
            }

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            // BEKLEME MODUNA DÖN
            Debug.Log("[CAMERA SWITCH] Player ? Lobby camera");

            DisablePlayerCamera();
            if (isLocalPlayer)
            {
                SetupLobbyCamera();
            }

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    void ToggleMouseLock()
    {
        if (Cursor.lockState == CursorLockMode.Locked)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    void LateUpdate()
    {
        if (!isLocalPlayer || (gameManager != null && !gameManager.gameStarted)) return;

        MouseX = Input.GetAxis("Mouse X") * 100 * Time.deltaTime;
        Body.Rotate(Vector3.up, MouseX);

        MouseY = Input.GetAxis("Mouse Y") * 100 * Time.deltaTime;
        Angle -= MouseY;
        Angle = Mathf.Clamp(Angle, -30, 45);
        Head.localRotation = Quaternion.Euler(Angle, 0, 0);
    }
}


