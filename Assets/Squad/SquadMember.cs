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
    [SerializeField] private float walkSpeed;
    [SerializeField] private float pathingSearchRate;
    [SerializeField] private float enemyCheckRadius;
    [SerializeField] private float enemyCheckRate;
    [SerializeField] private float enemyEngagePosPadding;
    [SerializeField] private float inCombatRotationSpeed;
    [SerializeField] private float leavePartyStepBackSize;
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private Animator anim;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private SpawnGhost spawnGhostScript;
    [SerializeField] private bool showDubugGizmos;

    private Rigidbody rb;
    private GameObject characterModel;
    private NavMeshAgent agent;
    private NavMeshObstacle obstacle;
    private SquadBelt squadBelt;
    private Transform player;
    private Player playerScript;
    private PartyManager partyMngr;
    private PartyHUD partyHUD;
    private SphereCollider safeHavenBorder;

    private float swordsmanAttackDamage = 7f;
    private float swordsmanSwingTillHitTime = 0.5f;
    private float swordsmanAttackSpeed = 1.3f;
    private float swordsmanAttackDelay = 1.6f;
    private float tankAttackDamage = 7f;
    private float tankSwingTillHitTime = 0.43f;
    private float tankAttackSpeed = 1.2f;
    private float tankAttackDelay = 2.2f;
    private float archerAttackDamage = 5f;
    private float archerFireBowSpeed = 0.5f;
    private float archerAttackDelay = 2.5f;
    private float archerAttackAirTime = 0.8f;
    private float rogueAttackDamage = 4f;
    private float rogueSwingTillHitTime = 0.3f;
    private float rogueAttackSpeed = 1.3f;
    private float rogueAttackDelay = 0.8f;
    private float sorcererAttackDamage = 6f;
    private float sorcererCastSpeed = 0.8f;
    private float sorcererAttackDelay = 3.1f;
    private float sorcererAttackAirTime = 1.3f;
    private float healerHealAmount = 15f;
    private float healerCastSpeed = 0.8f;
    private float healerHealDelay = 3.9f;
    private float healerHealAirTime = 0.5f;

    private Vector3 followPoint;
    private bool isFollowing;
    private int partyPosition;
    [SerializeField] private Transform closestEnemyFound;
    private Vector3 engagePos;
    [SerializeField] private bool canAttack;
    private float health;
    private bool canEngageCombat;
    private States preCombatState;
    private int lastAttack;
    private bool regrouping;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        characterModel = transform.Find("Character Model").gameObject;
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
        ProcessMultipliers();

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
    }

    void ProcessMultipliers()
    {
        agent.speed = walkSpeed * EnemyBaseManager.Instance.GetMovementSpeedMultiplier();
        if (anim.isActiveAndEnabled)
            anim.SetFloat("RunSpeed", EnemyBaseManager.Instance.GetMovementSpeedMultiplier());
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
        if (closestEnemyFound)
        {
            // Face enemy
            Vector3 newRot = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation((closestEnemyFound.position - transform.position).normalized), inCombatRotationSpeed * Time.deltaTime).eulerAngles;
            newRot.x = 0;
            newRot.z = 0;
            transform.rotation = Quaternion.Euler(newRot);
        }

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
            closestEnemyFound = null;
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
            //StopCoroutine(nameof(Attack));
            //canAttack = true;

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
                state = States.CHASING;
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
                {
                    if (Vector3.Distance(transform.position, closestEnemyFound.position) <= enemyEngagePosPadding)
                    {
                        return transform.position;
                    }
                    else
                    {
                        Vector3 inverseDirection = (transform.position - closestEnemyFound.position).normalized;
                        return closestEnemyFound.position + (inverseDirection * enemyEngagePosPadding);
                    }
                }
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
                    anim.SetFloat("AttackSpeed", swordsmanAttackSpeed);
                    anim.SetTrigger("Attack1");
                    yield return new WaitForSeconds(swordsmanSwingTillHitTime);
                    if (closestEnemyFound)
                    {
                        closestEnemyFound.GetComponent<Enemy>().TakeDamage(swordsmanAttackDamage * EnemyBaseManager.Instance.GetBladeDamageMultiplier());
                        Debug.Log(gameObject.name + " attacks " + closestEnemyFound.gameObject.name
                                  + " (-" + swordsmanAttackDamage * EnemyBaseManager.Instance.GetBladeDamageMultiplier() + ")");
                    }

                    yield return new WaitForSeconds(swordsmanAttackDelay - swordsmanSwingTillHitTime);
                    break;
                }
            case Classes.Tank:
                {
                    anim.SetFloat("AttackSpeed", tankAttackSpeed);
                    anim.SetTrigger("Attack2");
                    yield return new WaitForSeconds(tankSwingTillHitTime);
                    if (closestEnemyFound)
                    {
                        closestEnemyFound.GetComponent<Enemy>().TakeDamage(tankAttackDamage * EnemyBaseManager.Instance.GetBladeDamageMultiplier());
                        Debug.Log(gameObject.name + " attacks " + closestEnemyFound.gameObject.name
                                  + " (-" + tankAttackDamage * EnemyBaseManager.Instance.GetBladeDamageMultiplier() + ")");
                    }
                    yield return new WaitForSeconds(tankAttackDelay - tankSwingTillHitTime);
                    break;
                }
            case Classes.Archer:
                {   
                    anim.SetFloat("AttackSpeed", archerFireBowSpeed);
                    //play animation
                    yield return new WaitForSeconds(archerAttackAirTime);
                    if (closestEnemyFound)
                    {
                        closestEnemyFound.GetComponent<Enemy>().TakeDamage(archerAttackDamage * EnemyBaseManager.Instance.GetBladeDamageMultiplier());
                        Debug.Log(gameObject.name + " attacks " + closestEnemyFound.gameObject.name
                                  + " (-" + archerAttackDamage * EnemyBaseManager.Instance.GetBladeDamageMultiplier() + ")");
                    }
                    yield return new WaitForSeconds(archerAttackDelay - archerAttackAirTime);
                    break;
                }
            case Classes.Rogue:
                {
                    anim.SetFloat("AttackSpeed", rogueAttackSpeed);
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
                    yield return new WaitForSeconds(rogueSwingTillHitTime);
                    if (closestEnemyFound)
                    {
                        closestEnemyFound.GetComponent<Enemy>().TakeDamage(rogueAttackDamage * EnemyBaseManager.Instance.GetBladeDamageMultiplier());
                        Debug.Log(gameObject.name + " attacks " + closestEnemyFound.gameObject.name
                                  + " (-" + rogueAttackDamage * EnemyBaseManager.Instance.GetBladeDamageMultiplier() + ")");
                    }
                    yield return new WaitForSeconds(rogueAttackDelay - rogueSwingTillHitTime);
                    break;
                }
            case Classes.Sorcerer:
                {
                    anim.SetFloat("AttackSpeed", sorcererCastSpeed);
                    //play animation
                    yield return new WaitForSeconds(sorcererAttackAirTime);
                    anim.SetTrigger("Attack2");
                    closestEnemyFound.TryGetComponent<Enemy>(out Enemy enemyComponent);
                    if (closestEnemyFound)
                    {
                        closestEnemyFound.GetComponent<Enemy>().TakeDamage(sorcererAttackDamage * EnemyBaseManager.Instance.GetMagicDamageMultiplier());
                        Debug.Log(gameObject.name + " attacks " + closestEnemyFound.gameObject.name
                                  + " (-" + sorcererAttackDamage * EnemyBaseManager.Instance.GetMagicDamageMultiplier() + ")");
                    }
                    yield return new WaitForSeconds(sorcererAttackDelay - sorcererAttackAirTime);
                    break;
                }
            case Classes.Healer:
                {
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

                    bool playerIsLowest = false;
                    if (playerScript.GetHealth() < lowestHealth)
                    {
                        playerIsLowest = true;
                    }

                    if (!playerScript.IsDead() && playerIsLowest && playerScript.GetHealth() < playerScript.GetMaxHealth())
                    {
                        anim.SetFloat("AttackSpeed", healerCastSpeed);
                        anim.SetTrigger("Attack3");

                        yield return new WaitForSeconds(healerHealAirTime);

                        float oldHealth = playerScript.GetHealth();

                        playerScript.HealHealth(healerHealAmount * EnemyBaseManager.Instance.GetMagicDamageMultiplier());

                        float healingDone = weakestSM.GetHealth() - oldHealth;
                        Debug.Log(gameObject.name + " heals " + player.name +
                                  " (+" + healingDone + ")");
                    }
                    else if (weakestSM.GetHealth() < weakestSM.GetMaxHealth())
                    {
                        anim.SetFloat("AttackSpeed", sorcererCastSpeed);
                        anim.SetTrigger("Attack3");

                        yield return new WaitForSeconds(healerHealAirTime);

                        if (partyMngr.partyMembers.Contains(weakestSM.squadMemberUID))
                        {
                            float oldHealth = weakestSM.GetHealth();

                            weakestSM.HealHealth(healerHealAmount * EnemyBaseManager.Instance.GetMagicDamageMultiplier());

                            float healingDone = weakestSM.GetHealth() - oldHealth;
                            Debug.Log(gameObject.name + " heals " + weakestSM.gameObject.name +
                                      " (+" + healingDone + ")");
                        }
                    }
                    yield return new WaitForSeconds(healerHealDelay - healerHealAirTime);
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

    public void TakeDamage(float _damage)
    {
        health -= _damage;
        partyHUD.SetHealthBar(partyPosition, health, maxHealth);

        if (health <= 0)
        {
            KillMember();
        }
    }

    private void KillMember()
    {
        tag = "Untagged";
        gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");
        characterModel.SetActive(false);
        state = States.IDLE;
        closestEnemyFound = null;
        transform.position = spawnPoint.position;
        transform.rotation = spawnPoint.rotation;
        StopFollowing();
        // Play death animation
        // Play death particle effects

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

        spawnGhostScript.BeginRespawnTimer();
    }

    public void RespawnMember()
    {
        health = maxHealth;
        gameObject.layer = LayerMask.NameToLayer("Member");
        BeginSearchingForEnemies();
        characterModel.SetActive(true);
    }

    public void DeathScreenFreezeMember()
    {
        tag = "Untagged";
        state = States.IDLE;
        closestEnemyFound = null;
        StopFollowing();
    }

    public void DeathScreenResetMember()
    {
        transform.position = spawnPoint.position;
        transform.rotation = spawnPoint.rotation;
        partyMngr.RemoveMemberFromParty(squadMemberUID);
        // Remove HUD display on reset
        partyHUD.UpdateHUD(4);
        partyHUD.UpdateHUD(3);
        partyHUD.UpdateHUD(2);
        partyHUD.UpdateHUD(1);
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
            DrawCircle(transform.position, enemyEngagePosPadding, Color.red);
        }
    }
}
