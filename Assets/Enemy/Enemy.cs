﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Enemy : MonoBehaviour
{
    public enum EnemyType
    {
        REGULAR,
        LARGE,
    }

    [SerializeField] private int baseID;
    [SerializeField] private float maxHealth;
    [SerializeField] private EnemyType type;
    [SerializeField] private float sightRange;
    [SerializeField] private float attackRange;
    [SerializeField] private float attackSwingTime;
    [SerializeField] private float attackDelay;
    [SerializeField] private float attackDamage;
    [SerializeField] private float inCombatRotationSpeed;
    [SerializeField] private List<Transform> patrolPoints;
    [SerializeField] private float patrolPointPadding;
    [SerializeField] private float minPatrolPauseTime;
    [SerializeField] private float maxPatrolPauseTime;
    [SerializeField] private LayerMask targetLayers;
    [SerializeField] private Animator anim;
    [SerializeField] private bool showDebugGizmos;
    [SerializeField] private GameObject healthBar;
    [SerializeField] private MeshRenderer baseIDShaderEffect;
    [SerializeField] private List<Material> baseIDShaderMats;

    private NavMeshAgent agent;
    private NavMeshObstacle obstacle;
    private SphereCollider safeHavenBorder;

    private bool targetInSightRange;
    private Transform nextPatrolPoint;
    private Vector3 nextPatrolPointPos;
    private bool isWaiting;

    private bool targetInAttackRange;
    private Transform target;
    private bool alreadyAttacked;
    private bool noPatrolPointWarning = false;
    private int lastAttack = 0;
    private float health;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        obstacle = GetComponent<NavMeshObstacle>();
        safeHavenBorder = GameObject.FindGameObjectWithTag("Safe Haven").GetComponent<SphereCollider>();

        agent.enabled = false;
        obstacle.enabled = true;

        health = maxHealth;

        if (baseID > 0)
        {
            baseIDShaderEffect.material = baseIDShaderMats[baseID - 1];
        }
    }
    
    void Update()
    {
        if (type == EnemyType.LARGE)
        {
            anim.SetFloat("RunSpeed", 0.7f);
        }

        targetInSightRange = Physics.CheckSphere(transform.position, sightRange, targetLayers);
        targetInAttackRange = Physics.CheckSphere(transform.position, attackRange, targetLayers);

        // Chase & Attack closest enemy
        GetClosestEnemy();

        if (targetInSightRange && !targetInAttackRange && TargetIsOutsideSafeHavenAndInParty(target)) ChaseEnemy();
        else if (targetInSightRange && targetInAttackRange && TargetIsOutsideSafeHavenAndInParty(target)) AttackEnemy();
        else Patrol();
    }

    private void GetClosestEnemy()
    {
        Collider[] results = Physics.OverlapSphere(transform.position, sightRange, targetLayers);

        if (results.Length > 0)
        {
            float shortest = Mathf.Infinity;
            foreach (var result in results)
            {
                if ((result.transform.position - transform.position).sqrMagnitude < shortest
                    && TargetIsOutsideSafeHavenAndInParty(result.transform))
                {
                    if (result.TryGetComponent(out Player player))
                    {
                        if (!player.IsDead())
                        {
                            target = result.transform;
                            shortest = (result.transform.position - transform.position).sqrMagnitude;
                        }
                        else
                        {
                            target = null;
                        }
                    }
                    else // for squad members
                    {
                        target = result.transform;
                        shortest = (result.transform.position - transform.position).sqrMagnitude;
                    }
                }
            }
        }
        else
        {
            target = null;
        }
    }

    private bool TargetIsOutsideSafeHavenAndInParty(Transform _target)
    {
        if (_target && Vector3.Distance(_target.position, safeHavenBorder.transform.position) > safeHavenBorder.radius
            && (_target.tag == "In Party" || _target.tag == "Player"))
        {
            return true;
        }
        return false;
    }

    private void Patrol()
    {
        if (patrolPoints.Count == 0 && !noPatrolPointWarning)
        {
            Debug.LogWarning("No enemy patrol points set for " + name);
            noPatrolPointWarning = true;
        }
        else if (noPatrolPointWarning)
        {
            // Stop doing anything, stand still

            anim.SetBool("isRunning", false);

            if (agent.enabled)
            {
                agent.enabled = false;
                obstacle.enabled = true;
            }
        }
        else if (patrolPoints.Count == 1)
        {
            nextPatrolPoint = patrolPoints[0];
            nextPatrolPointPos = patrolPoints[0].position;

            obstacle.enabled = false;
            agent.enabled = true;

            agent.SetDestination(nextPatrolPointPos);
            anim.SetBool("isRunning", true);

            if (Vector3.Distance(nextPatrolPointPos, transform.position) < patrolPointPadding)
            {
                anim.SetBool("isRunning", false);
            }
        }
        else if (patrolPoints.Count > 1)
        {
            if (!nextPatrolPoint)
            {
                List<Transform> otherPatrolPoints = new List<Transform>(patrolPoints);
                otherPatrolPoints.Remove(nextPatrolPoint);

                int i = Random.Range(0, otherPatrolPoints.Count);
                nextPatrolPoint = otherPatrolPoints[i];
                nextPatrolPointPos = otherPatrolPoints[i].position;

                obstacle.enabled = false;
                agent.enabled = true;

                agent.SetDestination(nextPatrolPointPos);
                anim.SetBool("isRunning", true);
            }

            if (!isWaiting && Vector3.Distance(nextPatrolPointPos, transform.position) < patrolPointPadding)
            {
                isWaiting = true;
                anim.SetBool("isRunning", false);

                StartCoroutine(nameof(WaitForSomeTime));
            }
        }
    }

    private IEnumerator WaitForSomeTime()
    {
        float WaitForSomeTime = Random.Range(minPatrolPauseTime, maxPatrolPauseTime);
        yield return new WaitForSeconds(WaitForSomeTime);

        nextPatrolPoint = null;
        isWaiting = false;
    }

    private void ChaseEnemy()
    {
        obstacle.enabled = false;
        agent.enabled = true;

        anim.SetBool("isRunning", true);

        nextPatrolPoint = null;
        agent.SetDestination(target.position);
    }

    private void AttackEnemy()
    {
        agent.enabled = false;
        obstacle.enabled = true;

        anim.SetBool("isRunning", false);

        nextPatrolPoint = null;
        // Face enemy
        Vector3 newRot = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation((target.position - transform.position).normalized), inCombatRotationSpeed * Time.deltaTime).eulerAngles;
        newRot.x = 0;
        newRot.z = 0;
        transform.rotation = Quaternion.Euler(newRot);

        if (!alreadyAttacked)
        {
            alreadyAttacked = true;
            StartCoroutine(nameof(Attack));
        }
    }

    private IEnumerator Attack()
    {
        if (type == EnemyType.REGULAR)
        {
            if (lastAttack == 0 || lastAttack == 2)
            {
                anim.SetTrigger("Attack6");
                lastAttack = 1;
            }
            else
            {
                anim.SetTrigger("Attack7");
                lastAttack = 2;
            }
        }
        else
        {
            if (lastAttack == 0 || lastAttack == 2)
            {
                anim.SetTrigger("Attack4");
                lastAttack = 1;
            }
            else
            {
                anim.SetTrigger("Attack5");
                lastAttack = 2;
            }
        }

        yield return new WaitForSeconds(attackSwingTime);

        if (target)
        {
            float damage = attackDamage * EnemyBaseManager.Instance.GetIncomingDamageMultiplier();

            if (target.TryGetComponent(out Player playerScript))
            {
                playerScript.TakeDamage(damage);
                CombatLogManager.Instance.PrintAttackLog(gameObject.name, false, "Alex", true, damage);
            }
            else if (target.TryGetComponent(out SquadMember squadMemberScript))
            {
                squadMemberScript.TakeDamage(damage);
                CombatLogManager.Instance.PrintAttackLog(gameObject.name, false, target.name, true, damage);
            }
            CombatParticleVisualiser.Instance.SpawnDamageParticleEffects(target.position + (Vector3.up * 0.5f), damage);
        }

        yield return new WaitForSeconds(attackDelay - attackSwingTime);

        alreadyAttacked = false;
    }

    public void TakeDamage(float _damage)
    {
        health -= _damage;
        healthBar.GetComponent<HealthBar>().UpdateBarUI(health, maxHealth);

        if (health <= 0) Invoke(nameof(DestroyEnemy), 0.2f);
    }

    private void DestroyEnemy()
    {
        EnemyBaseManager.Instance.UpdateRecords(baseID);
        Destroy(gameObject);
    }

    public EnemyType GetEnemyType()
    {
        return type;
    }

    public int GetBaseID()
    {
        return baseID;
    }

    private void DrawCircle(Vector3 center, float radius, Color color)
    {
        Vector3 prevPos = center + new Vector3(radius, 0, 0);
        for (int i = 0; i < 30; i++)
        {
            float angle = (float)(i + 1) / 30.0f * Mathf.PI * 2.0f;
            Vector3 newPos = center + new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius);
            Debug.DrawLine(prevPos, newPos, color);
            prevPos = newPos;
        }
    }

    private void OnDrawGizmos()
    {
        if (showDebugGizmos)
        {
            DrawCircle(transform.position, sightRange, Color.blue);
            DrawCircle(transform.position, attackRange, Color.red);
        }
    }
}
