using UnityEngine;
using Fusion;

public class GameManager : NetworkBehaviour
{
    [Networked] public bool gameStarted { get; set; } = false;

    public void StartGame() {
        if (Object.HasStateAuthority) // Sadece host deðiþtirebilir
        {
            gameStarted = true;
            Debug.Log("Game started by host!");
        }
    }

}
