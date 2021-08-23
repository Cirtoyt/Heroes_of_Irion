using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TailLink : MonoBehaviour
{
    public Transform chainPoint;

    private TailManager manager;

    private void Awake()
    {
        manager = GetComponentInParent<TailManager>();
    }

    private void Update()
    {
        Vector3 chainPointPos = chainPoint.position;

        // Handle height correction if chain point is the player
        if (chainPoint == GameObject.FindGameObjectWithTag("Player").transform)
        {
            chainPointPos = new Vector3(chainPoint.position.x,
                                        chainPoint.position.y - 1,
                                        chainPoint.position.z);
        }

        if (Vector3.Distance(transform.position, chainPointPos) > manager.tailLength)
        {
            RestrictPosition(chainPointPos);
        }
    }

    private void RestrictPosition(Vector3 chainPointPos)
    {
        Vector3 chainPointToCurrentPointVecNorm = (transform.position - chainPointPos).normalized;
        Vector3 centreToInnerEdge = chainPointToCurrentPointVecNorm * manager.tailLength;
        Vector3 edgePos = chainPointPos + centreToInnerEdge;

        transform.position = edgePos;
    }

    private void OnDrawGizmos()
    {
        Vector3 chainPointPos = chainPoint.position;

        // Handle height correction if chain point is the player
        if (chainPoint == GameObject.FindGameObjectWithTag("Player").transform)
        {
            chainPointPos = new Vector3(chainPoint.position.x,
                                        chainPoint.position.y - 1,
                                        chainPoint.position.z);
        }

        Gizmos.color = Color.green;
        Gizmos.DrawRay(transform.position, chainPointPos - transform.position);
    }
}
