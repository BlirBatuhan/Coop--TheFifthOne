using Fusion;
using UnityEngine;

public class CharacterHealth : MonoBehaviour
{
     public float Health = 100f;
    

    public void TakeDamage(float damage)
    {
        if (Health <= 0) return;

        Health -= damage;
        Debug.Log("Damage taken: " + damage + ", Remaining Health: " + Health);
        if (Health <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        Destroy(gameObject);
        // NPC ise Destroy(gameObject);
        // Player ise respawn / disable controls
    }
}
