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
    [SerializeField] private float weaponDamage;
    [SerializeField] private Image healthBar;
    [Header("Camera")]
    [SerializeField] private Transform cameraArm;
    [SerializeField] private Transform cameraFocus;
    [SerializeField] private float cameraSensitivity;
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

    private Vector3 inputLookDirection;
    private float xAxisRotation;
    private Vector3 inputMoveDirection;
    private Vector3 movement;
    private float health;
    private bool canAttack = true;
    private int lastAttack = 0;
    private bool selectingTarget;
    private Transform highlightedEnemy;

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

        Cursor.lockState = CursorLockMode.Locked;
        health = maxHealth;
    }
    
    void Update()
    {
        PivotCamera();
        UpdateAnimator();
    }

    private void LateUpdate()
    {
        // Triggering enemy outline
        if (selectingTarget && Physics.SphereCast(transform.position, interactHalfWidth, transform.forward, out RaycastHit hit, attackHighlightRange, enemyLayer))
        {
            hit.transform.GetComponent<Outline>().enabled = true;
            hit.transform.GetComponent<OutlineCanceler>().enabled = true;
        }
    }

    private void FixedUpdate()
    {
        MovePlayer();
    }

    private void PivotCamera()
    {
        float lookX = inputLookDirection.x * cameraSensitivity * Time.deltaTime;
        float lookY = inputLookDirection.y * cameraSensitivity * Time.deltaTime;

        xAxisRotation -= lookY;
        xAxisRotation = Mathf.Clamp(xAxisRotation, bottomCameraMaxAngle, topCameraMaxAngle);

        transform.Rotate(Vector3.up, lookX);
        cameraArm.transform.localRotation = Quaternion.Euler(Vector3.right * xAxisRotation);
        mainCamera.transform.LookAt(cameraFocus);
    }

    private void MovePlayer()
    {
        // Remove Y so that when you look up you don't go up into the sky lol
        Vector3 cameraForward =
            new Vector3(mainCamera.transform.forward.x, 0f, mainCamera.transform.forward.z).normalized;
        Vector3 cameraRight = mainCamera.transform.right.normalized;

        movement = cameraForward * inputMoveDirection.z * walkSpeed * Time.fixedDeltaTime +
                   cameraRight * inputMoveDirection.x * walkSpeed * Time.fixedDeltaTime;

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
        }
        else
        {
            anim.SetBool("isRunning", false);
        }

        //Attack speed
        float attackSpeed = (attackDelay / 1.2f) * 2;
        anim.SetFloat("AttackSpeed", attackSpeed);
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
        // Effect UI wheel if active
        if (actionMenu.transform.Find("Menu Contents").gameObject.activeInHierarchy)
        {
            actionMenu.transform.Find("Menu Contents").gameObject.SetActive(false);
            Cursor.lockState = CursorLockMode.Locked;

            // Detect which action is to be executed and to whome (using party position)
            switch (actionMenu.lastSelectedAction)
            {
                case Actions.REMOVEFROMPARTY:
                    {
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
                    }
                    break;
                case Actions.REGROUP:
                    {
                        foreach (int partyPos in actionMenu.selectedPartyMembers)
                        {
                            int memberUID = partyMngr.GetUIDFromPositionInParty(partyPos);
                            RegroupPartyMember(memberUID);
                        }
                    }
                    break;
                case Actions.ATTACK:
                    {
                        selectingTarget = true;
                    }
                    break;
            }

            // Remove selected party positions/members after action is executed
            actionMenu.selectedPartyMembers.Clear();
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

    private void OnMouseRightButton(InputValue value)
    {
        if (selectingTarget)
        {
            // Look for enemy
            if (Physics.SphereCast(transform.position, interactHalfWidth, transform.forward,
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
        }
        else
        {
            // Check for member in-front of player
            if (Physics.SphereCast(transform.position, interactHalfWidth, transform.forward,
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

    private void OnOpenPartyMember1ActionMenu(InputValue value)
    {
        if (partyMngr.partyMembers.Count > 1)
            OpenActionMenu(1);
    }

    private void OnOpenPartyMember2ActionMenu(InputValue value)
    {
        if (partyMngr.partyMembers.Count > 2)
            OpenActionMenu(2);
    }

    private void OnOpenPartyMember3ActionMenu(InputValue value)
    {
        if (partyMngr.partyMembers.Count > 3)
            OpenActionMenu(3);
    }

    private void OnOpenPartyMember4ActionMenu(InputValue value)
    {
        if (partyMngr.partyMembers.Count > 4)
            OpenActionMenu(4);
    }

    private void OpenActionMenu(int partyPosition)
    {
        if (!actionMenu.transform.Find("Menu Contents").gameObject.activeInHierarchy)
        {
            Cursor.lockState = CursorLockMode.Confined;
            actionMenu.transform.Find("Menu Contents").gameObject.SetActive(true);
        }
        if (partyMngr.partyMembers.Contains(partyMngr.GetUIDFromPositionInParty(partyPosition))
            && !actionMenu.selectedPartyMembers.Contains(partyPosition))
        {
            actionMenu.selectedPartyMembers.Add(partyPosition);
        }
        else if (actionMenu.selectedPartyMembers.Contains(partyPosition))
        {
            actionMenu.selectedPartyMembers.Remove(partyPosition);
        }
    }

    private IEnumerator Attack()
    {
        canAttack = false;
        
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
        
        // After attack, allow attacking again after delay (as to not spam the button)
        yield return new WaitForSeconds(attackDelay);

        weapon.canHit = true;
        canAttack = true;

    }

    public void DealDamage(Enemy enemyTarget)
    {
        enemyTarget.TakeDamage(weaponDamage);
        Debug.Log("Player attacks " + enemyTarget.gameObject.name + " (-" + weaponDamage + ")");
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
                member.ChangeAttackTarget(newEnemy);
            }
        }
    }

    private void KillPlayer()
    {
        // Player anim plays death animation
        // Non-UI input is frozen
        // Display dead menu appears
        //Time.timeScale = 0;

        Debug.Log("Player has been defeated");
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
}
