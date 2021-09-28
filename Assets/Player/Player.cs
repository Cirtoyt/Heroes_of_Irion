using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class Player : MonoBehaviour
{
    [Header("Player")]
    [SerializeField] private float walkSpeed;
    [SerializeField] private float maxHealth;
    [SerializeField] private float interactRange;
    [SerializeField] private float interactHalfWidth;
    [SerializeField] private float attackHighlightRange;
    [SerializeField] private LayerMask memberLayer;
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private Animator anim;
    [SerializeField] private Transform meshTrans;
    [SerializeField] private float attackDelay;
    [SerializeField] private float attackSwingTime;
    [SerializeField] private float weaponDamage;
    [SerializeField] private Image healthBar;
    [SerializeField] private GameObject pauseMenu;
    [SerializeField] private Transform spawnPoint;
    [Header("Camera")]
    [SerializeField] private Transform cameraArm;
    [SerializeField] private Transform cameraFocus;
    [SerializeField] private float cameraSensitivity;
    [SerializeField] private float cameraSmoothing;
    [SerializeField] private float topCameraMaxAngle;
    [SerializeField] private float bottomCameraMaxAngle;

    [HideInInspector] public bool isMoving;
    [HideInInspector] public Vector2 UIMousePos;

    private PlayerInput playerInput;
    private Rigidbody rb;
    private Camera mainCamera;
    private SquadBelt squadBelt;
    private PartyManager partyMngr;
    private ActionMenu actionMenu;
    private PartyHUD partyHUD;
    private Weapon weapon;
    private SphereCollider safeHavenBorder;

    private Vector3 inputLookDirection;
    private float xAxisRotation;
    private float yAxisRotation;
    private Vector3 inputMoveDirection;
    private Vector3 movement;
    private float health;
    private bool canAttack = true;
    private bool isAttacking = false;
    private int lastAttack = 0;
    private List<Enemy> hitTargets = new List<Enemy>();
    private bool selectingTarget;
    private bool gameIsPaused = false;
    private CursorLockMode currentCursorLockMode;

    void Start()    
    {
        playerInput = GetComponent<PlayerInput>();
        rb = GetComponent<Rigidbody>();
        mainCamera = playerInput.camera;
        squadBelt = FindObjectOfType<SquadBelt>();
        partyMngr = FindObjectOfType<PartyManager>();
        actionMenu = FindObjectOfType<ActionMenu>();
        partyHUD = FindObjectOfType<PartyHUD>();
        weapon = GetComponentInChildren<Weapon>();
        safeHavenBorder = GameObject.FindGameObjectWithTag("Safe Haven").GetComponent<SphereCollider>();

        Cursor.lockState = CursorLockMode.Locked;
        currentCursorLockMode = CursorLockMode.Locked;
        health = maxHealth;
    }
    
    void Update()
    {
        if (gameIsPaused)
        {
            isMoving = false;
        }
        UpdateAnimator();

        if (selectingTarget)
        {
            bool allMembersAreOutsideSafeHaven = true;
            foreach (int partyPos in actionMenu.selectedPartyMembers)
            {
                SquadMember member = partyMngr.GetSquadMemberFromPositionInParty(partyPos);
                if (Vector3.Distance(member.transform.position, safeHavenBorder.transform.position) <= safeHavenBorder.radius)
                {
                    allMembersAreOutsideSafeHaven = false;
                }
            }

            if (!allMembersAreOutsideSafeHaven)
            {
                selectingTarget = false;
                // Remove selected party positions/members after action is cancelled
                actionMenu.selectedPartyMembers.Clear();
            }
        }
    }

    private void LateUpdate()
    {
        // Triggering enemy outline
        Vector3 playerForward = new Vector3(mainCamera.transform.forward.x, 0, mainCamera.transform.forward.z).normalized;
        if (selectingTarget && Physics.SphereCast(transform.position - (playerForward * interactHalfWidth), interactHalfWidth, playerForward, out RaycastHit enemyHit, attackHighlightRange, enemyLayer))
        {
            enemyHit.transform.GetComponent<Outline>().enabled = true;
            enemyHit.transform.GetComponent<OutlineCanceler>().enabled = true;
        }

        // Triggering squad member not in party outline
        if (Physics.SphereCast(transform.position - (playerForward * interactHalfWidth), interactHalfWidth, playerForward,
                               out RaycastHit memberHit, interactRange, memberLayer))
        {
            if (memberHit.transform.tag == "Untagged")
            {
                memberHit.transform.GetComponent<Outline>().enabled = true;
                memberHit.transform.GetComponent<OutlineCanceler>().enabled = true;
            }
            
        }
    }

    private void FixedUpdate()
    {
        if (!gameIsPaused)
        {
            PivotCamera();
            MovePlayer();
        }
    }

    private void PivotCamera()
    {
        float lookX = inputLookDirection.x * cameraSensitivity * Time.deltaTime;
        float lookY = inputLookDirection.y * cameraSensitivity * Time.deltaTime;

        xAxisRotation -= lookY;
        xAxisRotation = Mathf.Clamp(xAxisRotation, bottomCameraMaxAngle, topCameraMaxAngle);
        yAxisRotation += lookX;

        Vector3 newRot = Quaternion.Slerp(cameraArm.transform.localRotation, Quaternion.Euler((Vector3.right * xAxisRotation) + (Vector3.up * yAxisRotation)), cameraSmoothing * Time.deltaTime).eulerAngles;
        cameraArm.transform.localRotation = Quaternion.Euler(newRot);
        mainCamera.transform.LookAt(cameraFocus);
    }

    private void MovePlayer()
    {
        // Remove Y so that when you look up you don't go up into the sky lol
        Vector3 cameraForward =
            new Vector3(mainCamera.transform.forward.x, 0f, mainCamera.transform.forward.z);
        Vector3 cameraRight = mainCamera.transform.right;

        movement = ((cameraForward * inputMoveDirection.z) + (cameraRight * inputMoveDirection.x)) * walkSpeed
            * EnemyBaseManager.Instance.GetMovementSpeedMultiplier() * Time.fixedDeltaTime;

        rb.MovePosition(transform.position + movement);

        Vector3 groundedPos = new Vector3(transform.position.x,
                                          transform.position.y - 1,
                                          transform.position.z);

        meshTrans.LookAt(groundedPos + movement, Vector3.up);

        isMoving = movement.magnitude > 0f ? true : false;
    }

    private void UpdateAnimator()
    {
        // Walking
        if (isMoving)
        {
            anim.SetBool("isRunning", true);
            anim.SetFloat("RunSpeed", EnemyBaseManager.Instance.GetMovementSpeedMultiplier());
        }
        else
        {
            anim.SetBool("isRunning", false);
        }

        //Attack speed
        anim.SetFloat("AttackSpeed", 1.3f);
    }

    private void OnLook(InputValue value)
    {
        Vector2 inputLook = value.Get<Vector2>();
        inputLookDirection = new Vector3(inputLook.x, inputLook.y, 0);
    }

    private void OnMovement(InputValue value)
    {
        Vector2 inputMovement = value.Get<Vector2>();
        inputMoveDirection = new Vector3(inputMovement.x, 0, inputMovement.y);
    }

    private void OnUICursorPosition(InputValue value)
    {
        UIMousePos = value.Get<Vector2>();
    }

    private void OnMouseLeftButton(InputValue value)
    {
        if (!gameIsPaused)
        {
            // Effect UI wheel if active
            if (actionMenu.transform.Find("Menu Contents").gameObject.activeInHierarchy)
            {
                actionMenu.transform.Find("Menu Contents").gameObject.SetActive(false);
                Cursor.lockState = CursorLockMode.Locked;
                currentCursorLockMode = CursorLockMode.Locked;

                // Detect which action is to be executed and to whome (using party position)
                switch (actionMenu.lastSelectedAction)
                {
                    case Actions.NONE:
                        {
                            // Remove selected party positions/members after action is none
                            actionMenu.selectedPartyMembers.Clear();
                            partyHUD.RemoveAllHUDHighlights();
                        }
                        break;
                    case Actions.REMOVEFROMPARTY:
                        {
                            partyHUD.RemoveAllHUDHighlights();
                            List<int> sortedList = actionMenu.selectedPartyMembers;
                            sortedList.Sort();
                            sortedList.Reverse();
                            foreach (int partyPos in sortedList)
                            {
                                int memberUID = partyMngr.GetUIDFromPositionInParty(partyPos);
                                RemovePartyMember(memberUID);
                            }
                            partyHUD.UpdateHUD(1);
                            partyHUD.UpdateHUD(2);
                            partyHUD.UpdateHUD(3);
                            partyHUD.UpdateHUD(4);

                            // Remove selected party positions/members after action is executed
                            actionMenu.selectedPartyMembers.Clear();
                        }
                        break;
                    case Actions.REGROUP:
                        {
                            foreach (int partyPos in actionMenu.selectedPartyMembers)
                            {
                                int memberUID = partyMngr.GetUIDFromPositionInParty(partyPos);
                                RegroupPartyMember(memberUID);
                            }

                            // Remove selected party positions/members after action is executed
                            actionMenu.selectedPartyMembers.Clear();
                            partyHUD.RemoveAllHUDHighlights();
                        }
                        break;
                    case Actions.ATTACK:
                        {
                            bool allMembersAreOutsideSafeHaven = true;
                            foreach (int partyPos in actionMenu.selectedPartyMembers)
                            {
                                SquadMember member = partyMngr.GetSquadMemberFromPositionInParty(partyPos);
                                if (Vector3.Distance(member.transform.position, safeHavenBorder.transform.position) <= safeHavenBorder.radius)
                                {
                                    allMembersAreOutsideSafeHaven = false;
                                }
                            }

                            if (allMembersAreOutsideSafeHaven)
                            {
                                selectingTarget = true;
                            }
                            else
                            {
                                // Remove selected party positions/members after action is cancelled
                                actionMenu.selectedPartyMembers.Clear();
                                partyHUD.RemoveAllHUDHighlights();
                            }
                        }
                        break;
                    case Actions.TAKECOVER:
                        {
                            Debug.Log("Take cover is currently not implemented");
                            // Remove selected party positions/members after action is none
                            actionMenu.selectedPartyMembers.Clear();
                            partyHUD.RemoveAllHUDHighlights();
                        }
                        break;
                }
            }
            // else if any other menu is open
            // Perform regular in-world attacks
            else
            {
                if (canAttack)
                {
                    StartCoroutine(Attack());
                }
            }
        }
    }

    private void OnMouseRightButton(InputValue value)
    {
        if (!gameIsPaused)
        {
            Vector3 playerForward = new Vector3(mainCamera.transform.forward.x, 0, mainCamera.transform.forward.z).normalized;
            if (selectingTarget)
            {
                // Look for enemy
                if (Physics.SphereCast(transform.position - (playerForward * interactHalfWidth), interactHalfWidth, playerForward,
                                   out RaycastHit hit, attackHighlightRange, enemyLayer))
                {
                    // Command party members to attack target
                    foreach (int partyPos in actionMenu.selectedPartyMembers)
                    {
                        int memberUID = partyMngr.GetUIDFromPositionInParty(partyPos);
                        AttackTarget(memberUID, hit.transform);
                    }
                }
                else
                {
                    Debug.Log("No enemy found to attack");
                }

                selectingTarget = false;

                // Remove selected party positions/members after action is executed
                actionMenu.selectedPartyMembers.Clear();
                partyHUD.RemoveAllHUDHighlights();
            }
            else
            {
                // Check for member in-front of player
                if (Physics.SphereCast(transform.position - (playerForward * interactHalfWidth), interactHalfWidth, playerForward,
                                   out RaycastHit hit, interactRange, memberLayer))
                {
                    if (hit.transform.TryGetComponent(out SquadMember hitMember))
                    {
                        // If not in party, add to party
                        if (partyMngr.GetPositionInParty(hitMember.squadMemberUID) == -1)
                        {
                            AddPartyMember(hitMember);
                        }
                    }
                }
            }
        }
    }

    private void OnOpenPartyMember1ActionMenu(InputValue value)
    {
        if (!gameIsPaused && !selectingTarget && partyMngr.partyMembers.Count > 1)
            OpenActionMenu(1);
    }

    private void OnOpenPartyMember2ActionMenu(InputValue value)
    {
        if (!gameIsPaused && !selectingTarget && partyMngr.partyMembers.Count > 2)
            OpenActionMenu(2);
    }

    private void OnOpenPartyMember3ActionMenu(InputValue value)
    {
        if (!gameIsPaused && !selectingTarget && partyMngr.partyMembers.Count > 3)
            OpenActionMenu(3);
    }

    private void OnOpenPartyMember4ActionMenu(InputValue value)
    {
        if (!gameIsPaused && !selectingTarget && partyMngr.partyMembers.Count > 4)
            OpenActionMenu(4);
    }

    private void OnOpenClosePauseMenu(InputValue value)
    {
        if (!gameIsPaused)
        {
            gameIsPaused = true;
            Cursor.lockState = CursorLockMode.Confined;
            pauseMenu.SetActive(true);
        }
        else
        {
            ClosePauseMenu();
        }
    }

    public void ClosePauseMenu()
    {
        gameIsPaused = false;
        if (currentCursorLockMode == CursorLockMode.Locked)
            Cursor.lockState = CursorLockMode.Locked;
        else if (currentCursorLockMode == CursorLockMode.Confined)
            Cursor.lockState = CursorLockMode.Confined;
        pauseMenu.SetActive(false);
    }

    private void OpenActionMenu(int partyPosition)
    {
        if (!actionMenu.transform.Find("Menu Contents").gameObject.activeInHierarchy)
        {
            Cursor.lockState = CursorLockMode.Confined;
            currentCursorLockMode = CursorLockMode.Confined;
            actionMenu.transform.Find("Menu Contents").gameObject.SetActive(true);
        }
        if (partyMngr.partyMembers.Contains(partyMngr.GetUIDFromPositionInParty(partyPosition))
            && !actionMenu.selectedPartyMembers.Contains(partyPosition))
        {
            actionMenu.selectedPartyMembers.Add(partyPosition);
            partyHUD.AddHUDHighlight(partyPosition);
        }
        else if (actionMenu.selectedPartyMembers.Contains(partyPosition))
        {
            actionMenu.selectedPartyMembers.Remove(partyPosition);
            partyHUD.RemoveHUDHighlight(partyPosition);
            if (actionMenu.selectedPartyMembers.Count <= 0)
            {
                actionMenu.transform.Find("Menu Contents").gameObject.SetActive(false);
                Cursor.lockState = CursorLockMode.Locked;
                currentCursorLockMode = CursorLockMode.Locked;
            }
        }
    }

    private IEnumerator Attack()
    {
        canAttack = false;
        isAttacking = true;

        // Affect animator
        switch (lastAttack)
        {
            case 0:
            case 2:
                anim.SetTrigger("Attack1");
                lastAttack = 1;
                break;
            case 1:
                anim.SetTrigger("Attack2");
                lastAttack = 2;
                break;
        }

        yield return new WaitForSeconds(attackSwingTime);

        foreach(var target in hitTargets)
        {
            DealDamage(hitTargets[0]);
        }
        if (hitTargets.Count > 0)
        {
            hitTargets.RemoveAt(0);
        }

        // After attack, allow attacking again after delay (as to not spam the button)
        yield return new WaitForSeconds(attackDelay - attackSwingTime);

        canAttack = true;
        isAttacking = false;
        hitTargets.Clear();

    }

    private void DealDamage(Enemy enemyTarget)
    {
        enemyTarget.TakeDamage(weaponDamage * EnemyBaseManager.Instance.GetBladeDamageMultiplier());
        Debug.Log("Player attacks " + enemyTarget.gameObject.name + " (-" + weaponDamage * EnemyBaseManager.Instance.GetBladeDamageMultiplier() + ")");
    }

    private void RemovePartyMember(int _memberUID)
    {
        partyMngr.RemoveMemberFromParty(_memberUID);
        squadBelt.UpdateFormationPositions();

        SquadMember[] members = FindObjectsOfType<SquadMember>();
        foreach (SquadMember member in members)
        {
            if (member.squadMemberUID == _memberUID)
            {
                member.SwitchState(SquadMember.States.IDLE);
                member.StepBack();
                member.gameObject.tag = "Untagged";
                Debug.Log(member.name + " has been removed from the party");
            }
        }
    }

    private void AddPartyMember(SquadMember _member)
    {
        partyMngr.AddMemberToParty(_member.squadMemberUID);
        squadBelt.UpdateFormationPositions();
        _member.SwitchState(SquadMember.States.CASUALFOLLOWING);
        partyHUD.UpdateHUD(partyMngr.GetPositionInParty(_member.squadMemberUID));
        _member.gameObject.tag = "In Party";

        SquadMember[] members = FindObjectsOfType<SquadMember>();
        foreach (SquadMember member in members)
        {
            member.BeginFollowing();
        }

        Debug.Log(_member.name + " has been added to the party");
    }

    private void RegroupPartyMember(int _memberUID)
    {
        SquadMember[] members = FindObjectsOfType<SquadMember>();
        foreach (SquadMember member in members)
        {
            if (member.squadMemberUID == _memberUID)
            {
                if (member.squadMemberUID == _memberUID && partyMngr.partyMembers.Contains(_memberUID))
                {
                    member.AttemptBeginRegrouping();
                }
            }
        }
    }

    private void AttackTarget(int _memberUID, Transform newEnemy)
    {
        SquadMember[] members = FindObjectsOfType<SquadMember>();
        foreach (SquadMember member in members)
        {
            if (member.squadMemberUID == _memberUID)
            {
                Debug.Log(member.name + " will now attack " + newEnemy.name);
                member.ChangeAttackTarget(newEnemy);
            }
        }
    }

    private void KillPlayer()
    {
        // Player anim plays death animation/particle effect
        // Non-UI input is frozen
        // Display dead menu
        //Time.timeScale = 0;

        Debug.Log("Player has been defeated");

        RespawnPlayer();
    }

    public void RespawnPlayer()
    {
        transform.position = spawnPoint.position;
        transform.rotation = spawnPoint.rotation;
        health = maxHealth;
        StartCoroutine(partyHUD.SmoothBarUI(healthBar, health, maxHealth));
    }

    public void TakeDamage(float _Damage)
    {
        health -= _Damage;
        StartCoroutine(partyHUD.SmoothBarUI(healthBar, health, maxHealth));

        if (health <= 0)
        {
            KillPlayer();
        }
    }

    public void HealHealth(float _amount)
    {
        health += _amount;
        StartCoroutine(partyHUD.SmoothBarUI(healthBar, health, maxHealth));

        // Play healing particle effect

        if (health > maxHealth)
        {
            health = maxHealth;
        }
    }

    public float GetHealth()
    {
        return health;
    }

    public float GetMaxHealth()
    {
        return maxHealth;
    }

    public void AddHitTarget(Enemy enemy)
    {
        if (!hitTargets.Contains(enemy))
        {
            hitTargets.Add(enemy);
        }
    }

    public bool IsAttacking()
    {
        return isAttacking;
    }
}
