using UnityEngine;
using Fusion;
using TMPro;
using UnityEngine.UI;

public class SessionListUIhandler : MonoBehaviour
{
    public TextMeshProUGUI statuText;
    public GameObject sessionInfoUIPrefab;
    public VerticalLayoutGroup VerticalLayoutGroup;

    
    [SerializeField] private SpawnPlayer spawnPlayer;

    private void Start()
    {
        
        if (spawnPlayer == null)
        {
            spawnPlayer = FindObjectOfType<SpawnPlayer>();
        }
    }

    public void ClearList()
    {
        foreach (Transform child in VerticalLayoutGroup.transform)
        {
            Destroy(child.gameObject);
        }
        statuText.gameObject.SetActive(false);
    }

    public void AddToList(SessionInfo sessionInfo)
    {
        SessionInfoUI addedSessionInfo = Instantiate(sessionInfoUIPrefab, VerticalLayoutGroup.transform).GetComponent<SessionInfoUI>();
        addedSessionInfo.SetInformation(sessionInfo);
        addedSessionInfo.OnJoinSession += AddedSeassionInfoListUIItem_OnJoinSession;
    }

    private void AddedSeassionInfoListUIItem_OnJoinSession(SessionInfo sessionInfo)
    {
        
        if (spawnPlayer != null)
        {
            spawnPlayer.OnJoinSessionRequested(sessionInfo);
        }
        else
        {
            Debug.LogError("SpawnPlayer reference is missing!");
        }
    }

    public void OnNoSessionsFound()
    {
        statuText.text = "Oda Bulunamadý";
        statuText.gameObject.SetActive(true);
    }

    public void OnLookingForGameSession()
    {
        statuText.text = "Oda Aranýyor";
        statuText.gameObject.SetActive(true);
    }

    
    public void OnCreateRoomButtonClicked()
    {
        
        string roomName = "Room_" + Random.Range(1000, 9999);
        if (spawnPlayer != null)
        {
            spawnPlayer.CreateRoom(roomName, 4);
        }
    }

    
}