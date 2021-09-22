using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ActionMenu : MonoBehaviour
{
    [SerializeField] private GameObject actionTitle;
    [SerializeField] private GameObject characterImage;
    [SerializeField] private GameObject characterTitle;
    [SerializeField] private GameObject[] menuOptions;

    public List<int> selectedPartyMembers;
    [HideInInspector] public Actions lastSelectedAction;

    private Player player;
    private PartyManager partyMngr;

    private Vector2 normCursorPos;
    private float currentAngle;
    private int selection;
    private int previousSelection;

    void Start()
    {
        selectedPartyMembers = new List<int>();
        player = GameObject.FindGameObjectWithTag("Player").GetComponent<Player>();
        partyMngr = FindObjectOfType<PartyManager>();
    }
    
    void Update()
    {
        if (transform.Find("Menu Contents").gameObject.activeInHierarchy)
        {
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
            bool anyMembersInCombat = false;
            foreach (SquadMember member in members)
            {
                if (!anyMembersInCombat
                    && (member.GetMemberState() == SquadMember.States.INCOMBAT
                        || member.GetMemberState() == SquadMember.States.CHASING))
                {
                    anyMembersInCombat = true;
                }
            }

            // Make certain UI options dim or not
            foreach (GameObject option in menuOptions)
            {
                ActionMenuOption optionScript = option.GetComponent<ActionMenuOption>();

                if (anyMembersInCombat && optionScript.GetActionType() == Actions.REMOVEFROMPARTY)
                {
                    optionScript.Dim();
                    if (lastSelectedAction == Actions.REMOVEFROMPARTY)
                    {
                        lastSelectedAction = Actions.NONE;
                    }
                }
                else if (!anyMembersInCombat && optionScript.GetActionType() == Actions.REGROUP)
                {
                    optionScript.Dim();
                    if (lastSelectedAction == Actions.REGROUP)
                    {
                        lastSelectedAction = Actions.NONE;
                    }
                }
                else
                {
                    optionScript.UnDim();
                }
            }
        }
    }
}
