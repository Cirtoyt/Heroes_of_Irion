using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ActionMenu : MonoBehaviour
{
    [SerializeField] private GameObject actionTitle;
    [SerializeField] private List<GameObject> characterImageHolders;
    [SerializeField] private List<Image> characterImages;
    [SerializeField] private GameObject[] menuOptions;

    public List<int> selectedPartyMembers;
    [HideInInspector] public Actions lastSelectedAction;

    private Player player;
    private PartyManager partyMngr;
    private PartyHUD partyHUD;
    private SphereCollider safeHavenBorder;

    private Vector2 normCursorPos;
    private float currentAngle;
    private int selection;
    private int previousSelection;

    void Start()
    {
        selectedPartyMembers = new List<int>();
        player = GameObject.FindGameObjectWithTag("Player").GetComponent<Player>();
        partyMngr = FindObjectOfType<PartyManager>();
        partyHUD = FindObjectOfType<PartyHUD>();
        safeHavenBorder = GameObject.FindGameObjectWithTag("Safe Haven").GetComponent<SphereCollider>();
    }
    
    void Update()
    {
        if (transform.Find("Menu Contents").gameObject.activeInHierarchy)
        {
            // Visualise currently selected party member avatars
            for (int i = 0; i < characterImageHolders.Count; i++)
            {
                if (i < selectedPartyMembers.Count)
                {
                    SquadMember member = partyMngr.GetSquadMemberFromPositionInParty(selectedPartyMembers[i]);

                    characterImageHolders[i].SetActive(true);
                    characterImages[i].sprite = partyHUD.avatars[member.squadMemberUID];
                    characterImages[i].color = Color.white;
                }
                else
                {
                    characterImageHolders[i].SetActive(false);
                    characterImages[i].sprite = null;
                }
            }

            // Get current selection
            normCursorPos = new Vector2(player.UIMousePos.x - Screen.width / 2,
                                       player.UIMousePos.y - Screen.height / 2);
            currentAngle = Mathf.Atan2(normCursorPos.y, normCursorPos.x) * Mathf.Rad2Deg;

            currentAngle = (currentAngle + 450) % 360;

            selection = (int)currentAngle / (360 / 5);

            // Update menu selection visuals
            if (selection != previousSelection)
            {
                // Set title
                actionTitle.GetComponent<Text>().text =
                    menuOptions[selection].GetComponent<ActionMenuOption>().GetTitle();

                // Store selected action
                lastSelectedAction = menuOptions[selection].GetComponent<ActionMenuOption>().GetActionType();

                // Update background visuals
                menuOptions[previousSelection].GetComponent<ActionMenuOption>().Deselect();
                previousSelection = selection;

                menuOptions[selection].GetComponent<ActionMenuOption>().Select();
            }
            
            // Check if any members are chasing or in-combat
            SquadMember[] members = FindObjectsOfType<SquadMember>();
            bool anyMembersInChasingOrCombat = false;
            foreach (SquadMember member in members)
            {
                if (!anyMembersInChasingOrCombat
                    && (member.GetMemberState() == SquadMember.States.INCOMBAT
                        || member.GetMemberState() == SquadMember.States.CHASING))
                {
                    anyMembersInChasingOrCombat = true;
                }
            }

            // Check if all selected members are outside safe haven
            bool allMembersAreOutsideSafeHaven = true;
            foreach (int partyPos in selectedPartyMembers)
            {
                SquadMember member = partyMngr.GetSquadMemberFromPositionInParty(partyPos);
                if (Vector3.Distance(member.transform.position, safeHavenBorder.transform.position) <= safeHavenBorder.radius)
                {
                    allMembersAreOutsideSafeHaven = false;
                }
            }

            // Make certain UI options dim or not
            foreach (GameObject option in menuOptions)
            {
                ActionMenuOption optionScript = option.GetComponent<ActionMenuOption>();

                if (anyMembersInChasingOrCombat && optionScript.GetActionType() == Actions.REMOVEFROMPARTY)
                {
                    optionScript.Dim();
                    if (lastSelectedAction == Actions.REMOVEFROMPARTY)
                    {
                        lastSelectedAction = Actions.NONE;
                    }
                }
                else if (!anyMembersInChasingOrCombat && optionScript.GetActionType() == Actions.REGROUP)
                {
                    optionScript.Dim();
                    if (lastSelectedAction == Actions.REGROUP)
                    {
                        lastSelectedAction = Actions.NONE;
                    }
                }
                else if (!allMembersAreOutsideSafeHaven && optionScript.GetActionType() == Actions.ATTACK)
                {
                    optionScript.Dim();
                }
                else
                {
                    optionScript.UnDim();
                }
            }
        }
    }
}
