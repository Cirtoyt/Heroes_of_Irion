using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HealingSpellEffect : MonoBehaviour
{
    [SerializeField] private float destroyDelay = 0.5f;
    
    public Transform target;
    public float flySpeed = 0;

    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        Destroy(gameObject, destroyDelay);
    }

    void FixedUpdate()
    {
        if (target)
        {
            Vector3 playerPos = target.position + (Vector3.up * 0.5f);
            Vector3 travelDirection = (playerPos - transform.position).normalized;
            rb.MovePosition(transform.position + travelDirection * Time.deltaTime * flySpeed);
        }
    }
}
