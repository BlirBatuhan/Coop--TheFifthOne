using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.AI;

public class PatrolAgent : MonoBehaviour
{
    NavMeshAgent Agent;
    public Transform[] PatrolPoints;  // 2 veya daha fazla nokta
    Animator Anim;

    private int currentPointIndex = 0;

    void Start()
    {
        Anim = GetComponent<Animator>();
        Agent = GetComponent<NavMeshAgent>();
        Agent.SetDestination(PatrolPoints[currentPointIndex].position);
        Anim.SetFloat("speed", 0.2f); // Yürüyüþ animasyonunu baþlat
    }

    void Update()
    {
        if (!Agent.pathPending && Agent.remainingDistance < 0.5f)
        {
            // Bir sonraki noktaya geç
            currentPointIndex = (currentPointIndex + 1) % PatrolPoints.Length;
            Agent.SetDestination(PatrolPoints[currentPointIndex].position);
        }
    }
}
