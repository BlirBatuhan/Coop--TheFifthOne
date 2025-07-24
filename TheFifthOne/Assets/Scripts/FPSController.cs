using UnityEngine;

public class MyCam : MonoBehaviour
{
    float MouseX;
    float MouseY;
    public Transform Body;  // Vücut objesi
    public Transform Head;  // Kamera objesi veya ba? objesi

    public float Angle;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
    }

    void LateUpdate()
    {


        MouseX = Input.GetAxis("Mouse X") * 100 * Time.deltaTime;
        Body.Rotate(Vector3.up, MouseX);

        MouseY = Input.GetAxis("Mouse Y") * 100 * Time.deltaTime;
        Angle -= MouseY;
        Angle = Mathf.Clamp(Angle, -30, 45);
        Head.localRotation = Quaternion.Euler(Angle, 0, 0);


    }


}