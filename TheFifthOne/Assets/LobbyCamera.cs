using Unity.Cinemachine;
using UnityEngine;

public class LobbyCamera : MonoBehaviour
{
    private CinemachineOrbitalFollow freeLookCamera;
    float xDegeri;
    float yDegeri;
    void Start()
    {
       freeLookCamera = GetComponent<CinemachineOrbitalFollow>();
    }

    // Update is called once per frame
    void Update()
    {
        freeLookCamera.HorizontalAxis.Value = Mathf.MoveTowards(freeLookCamera.HorizontalAxis.Value, 160, Time.deltaTime * 5f);
        freeLookCamera.VerticalAxis.Value = Mathf.MoveTowards(freeLookCamera.VerticalAxis.Value, 40, Time.deltaTime * 0.5f);
    }
}
