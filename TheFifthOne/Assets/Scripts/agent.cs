using UnityEngine;
using UnityEngine.AI;

public class MonsterAI : MonoBehaviour
{
    public enum AIState { Idle, Chase, Attack }
    public AIState currentState = AIState.Idle;

    [Header("Görüþ Ayarlarý")]
    public float detectionRadius = 10f;
    public float viewAngle = 90f;
    public Transform eyePoint;
    public LayerMask playerMask;
    public LayerMask obstacleMask;

    [Header("Takip ve Saldýrý")]
    public float attackDistance = 2f;
    

    private NavMeshAgent agent;
    private Transform targetPlayer;
    private Animator anim;

    void Start()
    {
        anim = GetComponent<Animator>();
        agent = GetComponent<NavMeshAgent>();
       
    }

    void Update()
    {
        switch (currentState)
        {
            case AIState.Idle:
                SearchForPlayer();
                break;
            case AIState.Chase:
                ChasePlayer();
                break;
            case AIState.Attack:
                AttackPlayer();
                break;
        }
    }

    void SearchForPlayer()
    {
        agent.isStopped = false; 
        Collider[] targets = Physics.OverlapSphere(transform.position, detectionRadius, playerMask);

        foreach (Collider target in targets)
        {
            //göz pozisyonuyla hedef arasýndaki yönü hesapla
            Vector3 dirToTarget = (target.transform.position - eyePoint.position).normalized;
            // Hedefe olan açýyý kontrol et
            float angle = Vector3.Angle(eyePoint.forward, dirToTarget);

            Debug.DrawRay(eyePoint.position, dirToTarget * detectionRadius, Color.red);

            if (angle < viewAngle / 1.5f)
            {
                // Engel kontrolü - eðer ray bir engele çarparsa oyuncuyu göremeyiz
                if (Physics.Raycast(eyePoint.position, dirToTarget, out RaycastHit hit, detectionRadius, obstacleMask))
                {
                    
                    continue; 
                }

                Debug.Log("Oyuncu bulundu! Takip baþlatýlýyor.");
                targetPlayer = target.transform;
                currentState = AIState.Chase;
                return;
            }
        }
    }

    void ChasePlayer()
    {
        if (targetPlayer == null)
        {
            currentState = AIState.Idle;
            return;
        }

        Debug.Log("Takip ediliyor.");
        anim.SetFloat("speed", 0.2f);
        agent.SetDestination(targetPlayer.position);

        float distance = Vector3.Distance(transform.position, targetPlayer.position);

        if(distance <= detectionRadius/2)
        {
            anim.SetFloat("speed", 0.5f);
        }

        if (distance <= attackDistance)
        {
            currentState = AIState.Attack;
        }

        // Oyuncu çok uzaklaþtýysa takibi býrak
        if (distance > detectionRadius * 1.5f)
        {
            agent.isStopped = true;
            agent.ResetPath();
            Debug.Log("Oyuncu çok uzaklaþtý, takip durduruluyor.");
            targetPlayer = null;
            currentState = AIState.Idle;
            anim.SetFloat("speed", 0);
        }
    }

    void AttackPlayer()
    {
        if (targetPlayer == null)
        {
            currentState = AIState.Idle;
            return;
        }

        anim.SetFloat("speed", 0);
        
        Vector3 lookDirection = targetPlayer.position - transform.position;
        lookDirection.y = 0; // canavarin geriye yatmasi engellendi

        if (lookDirection != Vector3.zero) 
        {
            transform.rotation = Quaternion.LookRotation(lookDirection.normalized);
        }

        Debug.Log("Saldýrýyor!");

        float distance = Vector3.Distance(transform.position, targetPlayer.position);
        if (distance > attackDistance)
        {
            currentState = AIState.Chase;
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackDistance);

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        // Görüþ açýsýný göster
        if (eyePoint != null)
        {
            Gizmos.color = Color.yellow;
            Vector3 leftBoundary = Quaternion.AngleAxis(-viewAngle / 2, Vector3.up) * eyePoint.forward * detectionRadius;
            Vector3 rightBoundary = Quaternion.AngleAxis(viewAngle / 2, Vector3.up) * eyePoint.forward * detectionRadius;

            Gizmos.DrawLine(eyePoint.position, eyePoint.position + leftBoundary);
            Gizmos.DrawLine(eyePoint.position, eyePoint.position + rightBoundary);
        }
    }
}