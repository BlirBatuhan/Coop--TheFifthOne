using UnityEngine;

public class deneme : MonoBehaviour
{
    public float Health = 100f;
    public void TakeDamage(float damage)
    {
        Health -= damage;
        Debug.Log("Health: " + Health);
        if (Health <= 0f)
        {
            Destroy(gameObject);
        }
    }
}
