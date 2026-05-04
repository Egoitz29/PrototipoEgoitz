using UnityEngine;
using UnityEngine.AI;

public class SimpleChase : MonoBehaviour
{
    public Transform target;

    NavMeshAgent agent;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    void Update()
    {
        if (target != null)
            agent.SetDestination(target.position);
    }
}