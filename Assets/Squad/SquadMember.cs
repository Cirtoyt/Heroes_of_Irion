using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class SquadMember : MonoBehaviour
{
    public enum Classes
    {
        Swordsman,
        Tank,
        Rogue,
        Archer,
        Healer,
        Sorcerer,
    }

    public enum States
    {
        IDLE,
        CASUALFOLLOWING,
        CHASING,
        INCOMBAT,
        TAKINGCOVER,
    }

    public int squadMemberUID;
    public Classes squadMemberClass;

    [SerializeField] private States state;
    [SerializeField] private float maxHealth;
    [SerializeField] private float pathingSearchRate;
    [SerializeField] private float enemyCheckRadius;
    [SerializeField] private float enemyCheckRate;
    [SerializeField] private float enemyEngagePosPadding;
    [SerializeField] private float leavePartyStepBackSize;
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private Animator anim;
    [SerializeField] private bool showDubugGizmos;
    [SerializeField] private GameObject healthBar;

    private Rigidbody rb;
    private NavMeshAgent agent;
    private NavMeshObstacle obstacle;
    private SquadBelt squadBelt;
    private Transform player;
    private Player playerScript;
    private PartyManager partyMngr;
    private PartyHUD partyHUD;
    private SphereCollider safeHavenBorder;

    private Vector3 followPoint;
    private bool isFollowing;
    [SerializeField] private int partyPosition;
    private Transform closestEnemyFound;
    private Vector3 engagePos;
    private bool canAttack;
    private float health;
    private bool canEngageCombat;
    private States preCombatState;
    private int lastAttack;
    private bool regrouping;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        agent = GetComponent<NavMeshAgent>();
        obstacle = GetComponent<NavMeshObstacle>();
        squadBelt = FindObjectOfType<SquadBelt>();
        player = GameObject.FindGameObjectWithTag("Player").transform;
        playerScript = player.GetComponent<Player>();
        partyMngr = FindObjectOfType<PartyManager>();
        partyHUD = FindObjectOfType<PartyHUD>();
        safeHavenBorder = GameObject.FindGameObjectWithTag("Safe Haven").GetComponent<SphereCollider>();

        followPoint = Vector3.zero;
        isFollowing = false;
        engagePos = Vector3.zero;
        canAttack = true;
        canEngageCombat = true;
        preCombatState = state;
        lastAttack = 0;
        regrouping = false;


        UpdatePartyPositionVar();
        agent.enabled = false;
        obstacle.enabled = true;
        health = maxHealth;
        followPoint = GenerateGroundedPosition(transform.position);
        BeginSearchingForEnemies();
    }
    
    void Update()
    {
        switch (state)
        {
            case States.IDLE:
                // Do nothing, just stand still & chill
                break;
            case States.CASUALFOLLOWING:
                {
                    UpdateFollowPointWithBeltPos();

                    if (squadBelt.GetFormation() == SquadBelt.Formations.STANDARD)
                    {
                        StandardFormation();
                    }
                    else if (squadBelt.GetFormation() == SquadBelt.Formations.COLUMN)
                    {
                        ColumnFormation();
                    }
                }
                break;
            case States.INCOMBAT:
                {
                    InCombat();
                }
                break;
            case States.CHASING:
                {
                    if (closestEnemyFound)
                    {
                        if (Vector3.Distance(transform.position, closestEnemyFound.position) <= enemyEngagePosPadding)
                        {
                            // Enemy reached, begin combat
                            agent.SetDestination(transform.position);
                            anim.SetBool("isRunning", false);
                            state = States.INCOMBAT;
                        }
                        else
                        {
                            // Keep chasing
                            engagePos = GetCombatPosition();
                            agent.SetDestination(engagePos);
                        }
                    }
                    else
                    {
                        if (preCombatState == States.CASUALFOLLOWING)
                        {
                            state = preCombatState;
                            BeginFollowing();
                            BeginSearchingForEnemies();
                        }
                        else if (preCombatState == States.IDLE)
                        {
                            state = preCombatState;
                        }
                    }
                }
                break;
        }

        // Test to begin chasing enemies
        if (canEngageCombat && closestEnemyFound && state != States.INCOMBAT && state != States.CHASING)
        {
            // Begin chasing
            StopFollowingButKeepAgentOn();
            CancelInvoke(nameof(CheckForEnemyPresence));

            engagePos = GetCombatPosition();

            if (engagePos != transform.position)
            {
                agent.SetDestination(engagePos);
                anim.SetBool("isRunning", true);
            }

            preCombatState = state;
            state = States.CHASING;
        }

        //rotation stuff
        //agent.updateRotation = false;
        //transform.LookAt(agent.desiredVelocity);
    }

    private void UpdateFollowPointWithBeltPos()
    {
        switch (partyPosition)
        {
            case 1:
                followPoint = squadBelt.followPos1;
                break;
            case 2:
                followPoint = squadBelt.followPos2;
                break;
            case 3:
                followPoint = squadBelt.followPos3;
                break;
            case 4:
                followPoint = squadBelt.followPos4;
                break;
        }
    }

    private void StandardFormation()
    {
        Vector3 groundedPos = GenerateGroundedPosition(transform.position);
        Vector3 groundedPlayerPos = GenerateGroundedPosition(player.position);


        if (!isFollowing && Vector3.Distance(groundedPos, groundedPlayerPos) > squadBelt.playerDeadzoneRadius)
        {
            BeginFollowing();
        }

        else if (isFollowing && !playerScript.isMoving &&
                 Vector3.Distance(groundedPos, followPoint) <= squadBelt.memberDeadzoneRadius)
        {
            StopFollowing();
        }
    }

    private void ColumnFormation()
    {
        Vector3 groundedPos = GenerateGroundedPosition(transform.position);
        

        if (!isFollowing && Vector3.Distance(groundedPos, followPoint) > squadBelt.memberDeadzoneRadius)
        {
            BeginFollowing();
        }
        
        else if (isFollowing && !playerScript.isMoving &&
                 Vector3.Distance(groundedPos, followPoint) <= squadBelt.memberDeadzoneRadius)
        {
            StopFollowing();
        }
    }

    private void InCombat()
    {
        // If can attack
        if (closestEnemyFound && canAttack)
        {
            // Attack
            canAttack = false;
            StartCoroutine(Attack());
        }

        // If enemy walks away
        if (closestEnemyFound
            && Vector3.Distance(transform.position, closestEnemyFound.position) > enemyEngagePosPadding)
        {
            if (preCombatState == States.CASUALFOLLOWING)
            {
                state = preCombatState;
                BeginFollowing();
                BeginSearchingForEnemies();
            }
            else if (preCombatState == States.IDLE)
            {
                state = preCombatState;
            }
        }

        // When enemy has been defeated, reset & check for new target
        if (!closestEnemyFound)
        {
            canAttack = true;

            CheckForEnemyPresence();

            // No target found
            if (!closestEnemyFound)
            {
                if (preCombatState == States.CASUALFOLLOWING)
                {
                    state = preCombatState;
                    BeginFollowing();
                    BeginSearchingForEnemies();
                }
                else if (preCombatState == States.IDLE)
                {
                    state = preCombatState;
                }
            }
            // Target found, goto new target
            else
            {
                engagePos = GetCombatPosition();
                agent.SetDestination(engagePos);
                anim.SetBool("isRunning", true);
            }
        }
    }

    private Vector3 GetCombatPosition()
    {
        switch (squadMemberClass)
        {
            case Classes.Tank:
            case Classes.Swordsman:
            case Classes.Rogue:
                //Vector3 lookDir = (closestEnemyFound.position - transform.position).normalized;
                Vector3 meleePos = closestEnemyFound.position; // + (lookDir * -enemyEngagePosPadding);
                return meleePos;
            default:
                return transform.position;
        }
    }

    private void CheckForEnemyPresence()
    {
        if (Vector3.Distance(transform.position, safeHavenBorder.transform.position) > safeHavenBorder.radius
            && tag == "In Party")
        {
            Collider[] enemies = Physics.OverlapSphere(transform.position, enemyCheckRadius, enemyLayer);

            if (enemies.Length > 0)
            {
                float minDist = Mathf.Infinity;
                foreach (Collider enemy in enemies)
                {
                    float dist = Vector3.Distance(enemy.transform.position, transform.position);
                    if (dist < minDist)
                    {
                        minDist = dist;
                        closestEnemyFound = enemy.transform;
                    }
                }

                // Go through list of enemies again, then prioritise the closest large enemy instead, ignorning regular if large is present
                /*float minLargeDist = Mathf.Infinity;
                foreach (Collider enemy in enemies)
                {
                    if (enemy.GetComponent<Enemy>().GetEnemyType() == Enemy.EnemyType.LARGE)
                    {
                        float dist = Vector3.Distance(enemy.transform.position, transform.position);
                        if (dist < minLargeDist)
                        {
                            minLargeDist = dist;
                            closestEnemyFound = enemy.transform;
                        }
                    }
                }*/
            }
            else
            {
                closestEnemyFound = null;
            }
        }
        else
        {
            closestEnemyFound = null;
        }
    }

    private Vector3 GenerateGroundedPosition(Vector3 position)
    {
        return new Vector3(position.x, position.y - 1, position.z);
    }

    public void BeginFollowing()
    {
        if (state == States.CASUALFOLLOWING)
        {
            obstacle.enabled = false;
            agent.enabled = true;
            isFollowing = true;
            anim.SetBool("isRunning", true);
            agent.SetDestination(followPoint);
            
            // Set repeated pathfinding until cancelled later
            InvokeRepeating(nameof(SetDestinationToFollowPoint), pathingSearchRate, pathingSearchRate);
        }
    }

    public void StepBack()
    {
        CancelInvoke(nameof(SetDestinationToFollowPoint));

        anim.SetBool("isRunning", true);
        obstacle.enabled = false;
        agent.enabled = true;
        agent.SetDestination(transform.position + transform.forward * -leavePartyStepBackSize);
        isFollowing = false;

        StartCoroutine(EndStepBack());
    }

    private IEnumerator EndStepBack()
    {
        yield return new WaitForSeconds(leavePartyStepBackSize / 4);
        agent.enabled = false;
        obstacle.enabled = true;
        anim.SetBool("isRunning", false);
    }

    private void StopFollowing()
    {
        CancelInvoke(nameof(SetDestinationToFollowPoint));

        agent.enabled = false;
        obstacle.enabled = true;
        isFollowing = false;
        canEngageCombat = true;
        anim.SetBool("isRunning", false);

        if (regrouping)
        {
            regrouping = false;
            BeginSearchingForEnemies();
        }
    }

    private void StopFollowingButKeepAgentOn()
    {
        CancelInvoke(nameof(SetDestinationToFollowPoint));

        obstacle.enabled = false;
        agent.enabled = true;
        isFollowing = false;
    }

    private void SetDestinationToFollowPoint()
    {
        agent.SetDestination(followPoint);
    }

    private void BeginSearchingForEnemies()
    {
        InvokeRepeating(nameof(CheckForEnemyPresence), 0, enemyCheckRate);
    }

    private IEnumerator Attack()
    {
        switch (squadMemberClass)
        {
            case Classes.Swordsman:
                {
                    float attackDelay = 1.6f;
                    float attackDamage = 7;

                    float attackSpeed = (attackDelay / 1.2f);
                    anim.SetFloat("AttackSpeed", attackSpeed);
                    anim.SetTrigger("Attack1");
                    closestEnemyFound.GetComponent<Enemy>().TakeDamage(attackDamage);
                    Debug.Log(gameObject.name + " attacks " + closestEnemyFound.gameObject.name
                              + " (-" + attackDamage + ")");

                    yield return new WaitForSeconds(attackDelay);
                    break;
                }
            case Classes.Tank:
                {
                    float attackDelay = 2.2f;
                    float attackDamage = 7;

                    float attackSpeed = (attackDelay / 1.2f);
                    anim.SetFloat("AttackSpeed", attackSpeed);
                    anim.SetTrigger("Attack2");
                    closestEnemyFound.GetComponent<Enemy>().TakeDamage(attackDamage);
                    Debug.Log(gameObject.name + " attacks " + closestEnemyFound.gameObject.name
                              + " (-" + attackDamage + ")");

                    yield return new WaitForSeconds(attackDelay);
                    break;
                }
            case Classes.Archer:
                {
                    float attackTime = 0.3f;
                    float attackDelay = 2.5f;
                    float attackDamage = 5;
                    
                    float attackSpeed = (attackTime / 1.2f) * 2;
                    anim.SetFloat("AttackSpeed", attackSpeed);
                    //play animation

                    yield return new WaitForSeconds(attackTime);

                    closestEnemyFound.GetComponent<Enemy>().TakeDamage(attackDamage);
                    Debug.Log(gameObject.name + " attacks " + closestEnemyFound.gameObject.name
                              + " (-" + attackDamage + ")");

                    yield return new WaitForSeconds(attackDelay);
                    break;
                }
            case Classes.Rogue:
                {
                    float attackDelay = 0.8f;
                    float attackDamage = 4;

                    float attackSpeed = (attackDelay / 1.2f) * 2;
                    anim.SetFloat("AttackSpeed", attackSpeed);
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

                    closestEnemyFound.GetComponent<Enemy>().TakeDamage(attackDamage);
                    Debug.Log(gameObject.name + " attacks " + closestEnemyFound.gameObject.name
                              + " (-" + attackDamage + ")");

                    yield return new WaitForSeconds(attackDelay);
                    break;
                }
            case Classes.Sorcerer:
                {
                    float attackTime = 0.5f;
                    float attackDelay = 3.1f;
                    float attackDamage = 6;

                    float attackSpeed = (attackTime / 1.2f) * 2;
                    anim.SetFloat("AttackSpeed", attackSpeed);
                    //play animation

                    yield return new WaitForSeconds(attackTime);

                    anim.SetTrigger("Attack2");
                    closestEnemyFound.GetComponent<Enemy>().TakeDamage(attackDamage);
                    Debug.Log(gameObject.name + " attacks " + closestEnemyFound.gameObject.name
                              + " (-" + attackDamage + ")");

                    yield return new WaitForSeconds(attackDelay);
                    break;
                }
            case Classes.Healer:
                {
                    float healTime = 0.5f;
                    float healDelay = 3.9f;
                    float healAmount = 15;
                    
                    float attackSpeed = (healTime / 1.2f) * 2;
                    anim.SetFloat("AttackSpeed", attackSpeed);
                    anim.SetTrigger("Attack3");

                    yield return new WaitForSeconds(healTime);

                    SquadMember weakestSM = this;
                    float lowestHealth = Mathf.Infinity;
                    SquadMember[] members = FindObjectsOfType<SquadMember>();
                    foreach (SquadMember member in members)
                    {
                        if (partyMngr.GetPositionInParty(member.squadMemberUID) != -1 &&
                            member.GetHealth() < lowestHealth)
                        {
                            weakestSM = member;
                            lowestHealth = member.GetHealth();
                        }
                    }

                    if (weakestSM.GetHealth() < maxHealth)
                    {
                        float oldHealth = weakestSM.GetHealth();

                        weakestSM.HealHealth(healAmount);

                        float healingDone = weakestSM.GetHealth() - oldHealth;
                        Debug.Log(gameObject.name + " heals " + weakestSM.gameObject.name +
                                  " (+" + healingDone + ")");
                    }
                    yield return new WaitForSeconds(healDelay);
                    break;
                }
        }

        canAttack = true;
    }

    public void SwitchState(States _state)
    {
        state = _state;
    }

    public States GetMemberState()
    {
        return state;
    }

    public void UpdatePartyPositionVar()
    {
        partyPosition = partyMngr.GetPositionInParty(squadMemberUID);
    }

    public float GetHealth()
    {
        return health;
    }

    public float GetMaxHealth()
    {
        return maxHealth;
    }

    public void AttemptBeginRegrouping()
    {
        if (state != States.CASUALFOLLOWING && state != States.IDLE)
        {
            SwitchState(States.CASUALFOLLOWING);
            canEngageCombat = false;
            regrouping = true;
            closestEnemyFound = null;

            Debug.Log(name + " is now regrouping");
        }
    }
    public void ChangeAttackTarget(Transform newTarget)
    {
        closestEnemyFound = newTarget;
    }

    public void TakeDamage(float _damage)
    {
        health -= _damage;
        partyHUD.SetHealthBar(partyPosition, health, maxHealth);

        if (health <= 0)
        {
            partyMngr.RemoveMemberFromParty(squadMemberUID);
            squadBelt.UpdateFormationPositions();

            SquadMember[] members = FindObjectsOfType<SquadMember>();
            foreach (SquadMember member in members)
            {
                if (member.state == States.CASUALFOLLOWING)
                {
                    member.BeginFollowing();
                }
            }

            // Remove HUD display on death
            partyHUD.UpdateHUD(4);
            partyHUD.UpdateHUD(3);
            partyHUD.UpdateHUD(2);
            partyHUD.UpdateHUD(1);

            // Play death animation
            // Play death particle effects

            Destroy(gameObject);
        }
    }

    public void HealHealth(float _amount)
    {
        health += _amount;
        partyHUD.SetHealthBar(partyPosition, health, maxHealth);

        // Play healing particle effect

        if (health > maxHealth)
        {
            health = maxHealth;
        }
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
        if (showDubugGizmos)
        {
            DrawCircle(transform.position, enemyCheckRadius, Color.magenta);
            DrawCircle(transform.position + transform.forward * enemyEngagePosPadding, 0.1f, Color.red);
        }
    }
}
