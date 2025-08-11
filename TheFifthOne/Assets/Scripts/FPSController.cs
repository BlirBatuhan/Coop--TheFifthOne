using Fusion;
using System.Runtime.CompilerServices;
using UnityEngine;

public class MyCam : NetworkBehaviour
{
    [Header("Camera Settings")]
    public Transform Body;
    public Transform Head;
    public Camera camera;
    public float mouseSensitivity = 100f;

    [Header("Movement Restrictions")]
    public bool restrictMovementInWaiting = true;

    // Input variables
    private float MouseX;
    private float MouseY;
    private float Angle;

    // State management
    private bool isLocalPlayer;
    private GameState currentCameraState = GameState.Lobby;
    private GameState previousCameraState = GameState.Lobby;

    // References
    private SpawnPlayer spawnPlayerManager;
    private GameManager gameManager;

    // Camera objects
    private Camera lobbyCamera;
    private AudioListener lobbyAudioListener;
    private AudioListener playerAudioListener;

    public void Awake()
    {
        // Player kameralarý baþlangýçta kapalý
        if (camera != null)
        {
            camera.enabled = false;
            playerAudioListener = camera.GetComponent<AudioListener>();
            if (playerAudioListener != null)
            {
                playerAudioListener.enabled = false;
            }
        }

        // Lobby kamerasý referansýný bul
        FindLobbyCamera();
    }

    private void FindLobbyCamera()
    {
        GameObject lobbyCamObject = GameObject.Find("LobbyCamera");
        if (lobbyCamObject != null)
        {
            lobbyCamera = lobbyCamObject.GetComponent<Camera>();
            lobbyAudioListener = lobbyCamObject.GetComponent<AudioListener>();
        }
    }

    public override void Spawned()
    {
        isLocalPlayer = Object.HasInputAuthority;

        // Manager referanslarýný al
        spawnPlayerManager = FindObjectOfType<SpawnPlayer>();
        gameManager = FindObjectOfType<GameManager>();

        Debug.Log($"[MyCam SPAWN] Player: {Object.InputAuthority}, isLocalPlayer: {isLocalPlayer}");

        // Baþlangýç kamera state'ini belirle
        DetermineInitialCameraState();

        // Local player için kamera kurulumu
        if (isLocalPlayer)
        {
            SetupCameraForCurrentState();
        }
        else
        {
            // Remote player kamerasý her zaman kapalý
            DisablePlayerCamera();
        }
    }

    private void DetermineInitialCameraState()
    {
        // SpawnPlayer'dan current game state'i al
        if (spawnPlayerManager != null)
        {
            // SpawnPlayer'daki currentGameState'e eriþim için public property eklemen gerekebilir
            // Þimdilik GameManager'dan kontrol edelim
            if (gameManager != null && gameManager.gameStarted)
            {
                currentCameraState = GameState.InGame;
            }
            else
            {
                currentCameraState = GameState.WaitingRoom;
            }
        }
        else
        {
            currentCameraState = GameState.WaitingRoom;
        }

        previousCameraState = currentCameraState;
        Debug.Log($"[MyCam] Initial camera state: {currentCameraState}");
    }

    void Update()
    {
        if (!isLocalPlayer) return;

        // Game state deðiþikliklerini kontrol et
        CheckForStateChanges();

        // Debug controls
        HandleDebugInput();
    }

    private void CheckForStateChanges()
    {
        GameState newState = GetCurrentGameState();

        if (newState != currentCameraState)
        {
            Debug.Log($"[MyCam STATE CHANGE] {currentCameraState} ? {newState}");

            previousCameraState = currentCameraState;
            currentCameraState = newState;

            SetupCameraForCurrentState();
        }
    }

    private GameState GetCurrentGameState()
    {
        // GameManager'dan oyun durumunu kontrol et
        if (gameManager != null && gameManager.gameStarted)
        {
            return GameState.InGame;
        }

        // SpawnPlayer'dan durumu kontrol et (eðer public property varsa)
        // Þimdilik GameManager'a göre karar verelim
        return GameState.WaitingRoom;
    }

    private void SetupCameraForCurrentState()
    {
        switch (currentCameraState)
        {
            case GameState.Lobby:
                SetupLobbyMode();
                break;
            case GameState.WaitingRoom:
                SetupWaitingRoomMode();
                break;
            case GameState.InGame:
                SetupGameMode();
                break;
        }
    }

    private void SetupLobbyMode()
    {
        Debug.Log("[MyCam] Setting up Lobby Mode");

        DisablePlayerCamera();
        EnableLobbyCamera();

        // Mouse serbest
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void SetupWaitingRoomMode()
    {
        Debug.Log("[MyCam] Setting up Waiting Room Mode");

        DisablePlayerCamera();
        EnableLobbyCamera();

        // Mouse serbest (UI için)
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void SetupGameMode()
    {
        Debug.Log("[MyCam] Setting up Game Mode");

        DisableLobbyCamera();
        EnablePlayerCamera();

        // Mouse kilitli
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void EnableLobbyCamera()
    {
        if (lobbyCamera != null)
        {
            lobbyCamera.gameObject.SetActive(true);
            lobbyCamera.enabled = true;

            if (lobbyAudioListener != null)
            {
                lobbyAudioListener.enabled = true;
            }

            Debug.Log("[MyCam] Lobby camera enabled");
        }
        else
        {
            Debug.LogWarning("[MyCam] Lobby camera not found!");
            FindLobbyCamera(); // Tekrar dene
        }
    }

    private void DisableLobbyCamera()
    {
        if (lobbyCamera != null)
        {
            lobbyCamera.gameObject.SetActive(false);
            lobbyCamera.enabled = false;

            if (lobbyAudioListener != null)
            {
                lobbyAudioListener.enabled = false;
            }

            Debug.Log("[MyCam] Lobby camera disabled");
        }
    }

    private void EnablePlayerCamera()
    {
        if (camera != null)
        {
            camera.enabled = true;

            if (playerAudioListener != null)
            {
                playerAudioListener.enabled = true;
            }

            Debug.Log("[MyCam] Player camera enabled");
        }
        else
        {
            Debug.LogError("[MyCam] Player camera is null!");
        }
    }

    private void DisablePlayerCamera()
    {
        if (camera != null)
        {
            camera.enabled = false;

            if (playerAudioListener != null)
            {
                playerAudioListener.enabled = false;
            }
        }
    }

    private void HandleDebugInput()
    {
        // ESC tuþu ile mouse lock toggle
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ToggleMouseLock();
        }

        // Debug: F1 ile manuel kamera geçiþi
        if (Input.GetKeyDown(KeyCode.F1))
        {
            Debug.Log($"[DEBUG] Current camera state: {currentCameraState}");
        }
    }

    private void ToggleMouseLock()
    {
        if (currentCameraState == GameState.InGame)
        {
            if (Cursor.lockState == CursorLockMode.Locked)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                Debug.Log("[MyCam] Mouse unlocked");
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                Debug.Log("[MyCam] Mouse locked");
            }
        }
    }

    void LateUpdate()
    {
        // Sadece local player ve oyun modunda mouse look
        if (!isLocalPlayer || currentCameraState != GameState.InGame)
            return;

        // Mouse locked deðilse mouse look yapma
        if (Cursor.lockState != CursorLockMode.Locked)
            return;

        HandleMouseLook();
    }

    private void HandleMouseLook()
    {
        MouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        Body.Rotate(Vector3.up, MouseX);

        MouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;
        Angle -= MouseY;
        Angle = Mathf.Clamp(Angle, -90f, 90f); // Daha geniþ açý aralýðý
        Head.localRotation = Quaternion.Euler(Angle, 0, 0);
    }

    // Public methods - SpawnPlayer tarafýndan çaðrýlabilir
    public void SetWaitingMode(bool isWaiting)
    {
        if (!isLocalPlayer) return;

        GameState targetState = isWaiting ? GameState.WaitingRoom : GameState.InGame;

        if (targetState != currentCameraState)
        {
            Debug.Log($"[MyCam] SetWaitingMode: {isWaiting} ? State: {targetState}");
            currentCameraState = targetState;
            SetupCameraForCurrentState();
        }
    }

    public GameState GetCurrentCameraState()
    {
        return currentCameraState;
    }

    public bool IsInGameMode()
    {
        return currentCameraState == GameState.InGame;
    }

    public bool IsLocalPlayerCamera()
    {
        return isLocalPlayer;
    }

    // Cleanup
    private void OnDisable()
    {
        if (isLocalPlayer)
        {
            DisablePlayerCamera();
        }
    }
}