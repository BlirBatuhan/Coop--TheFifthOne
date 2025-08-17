using UnityEngine;
using Fusion;

public class WeaponHitbox : NetworkBehaviour
{
    public float damage = 10f;
    private bool active = false;

    public void ActivateHitbox() => active = true;
    public void DeactivateHitbox() => active = false;

    private void OnTriggerEnter(Collider other)
    {
        if (!active) return;

        var target = other.GetComponent<CharacterHealth>();
        if (target != null && target.gameObject.tag != "NPC") // kendine vurmasýn
        {
            if (Object.HasStateAuthority) // sadece authority damage uygular
            {
                target.TakeDamage(damage);
            }
        }
    }
}
