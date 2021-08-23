using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class SquadBelt : MonoBehaviour
{
    public enum Formations
    {
        STANDARD, // Casual following formation
        COLUMN, // Casual following formation for when not enough space within the room for standard
        STAGGEREDCOLUMNLEFT, // On-guard following formation, aiming left
        STAGGEREDCOLUMNRIGHT, // On-guard following formation, aiming right
    }

    [Header("Setup Variables")]
    [SerializeField] private SquadBeltPoint beltPoint1;
    [SerializeField] private SquadBeltPoint beltPoint2;
    [SerializeField] private SquadBeltPoint beltPoint3;
    [SerializeField] private SquadBeltPoint beltPoint4;
    public TailManager tailManager;
    [Header("Options")]
    public float playerDeadzoneRadius;
    public float memberDeadzoneRadius;
    [SerializeField] private Formations formation;
    [SerializeField] private float formationMovementSmoothing;
    public float formationSpacing;
    [SerializeField] private float enoughSpaceCheckRadius;
    private bool[] spaceCheckArray = new bool[4];
    [SerializeField] private bool showDebugGizmos;

    [HideInInspector] public Vector3 followPos1;
    [HideInInspector] public Vector3 followPos2;
    [HideInInspector] public Vector3 followPos3;
    [HideInInspector] public Vector3 followPos4;

    private Transform player;
    private PartyManager partyMngr;

    private void Awake()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
        partyMngr = FindObjectOfType<PartyManager>();

        UpdateFormationPositions();
    }

    void Update()
    {
        // Follow player
        Vector3 followPos = new Vector3(player.position.x, player.position.y - 1.0f, player.position.z);
        transform.position = followPos;

        // Smooth rotate formation with tail to player facing direction
        Quaternion followDir =
            Quaternion.LookRotation((followPos - tailManager.link1.position).normalized, Vector3.up);
        transform.rotation =
            Quaternion.Lerp(transform.rotation, followDir, Time.deltaTime * formationMovementSmoothing);

        UpdateFormationPositions();

        IsThereEnoughSpace();
    }

    public void UpdateFormationPositions()
    {
        float fullSpace = formationSpacing;
        float halfSpace = formationSpacing / 2;
        float thirdSpace = formationSpacing / 3;
        float twoThirdSpace = (formationSpacing / 3) * 2;

        // Always move belt points based on diamond formation
        switch (partyMngr.partyMembers.Count)
        {
            case 2:
                beltPoint1.transform.localPosition = new Vector3(0, 0, -fullSpace);
                break;
            case 3:
                beltPoint1.transform.localPosition = new Vector3(fullSpace, 0, -fullSpace);
                beltPoint2.transform.localPosition = new Vector3(-fullSpace, 0, -fullSpace);
                break;
            case 4:
                beltPoint1.transform.localPosition = new Vector3(fullSpace, 0, -fullSpace);
                beltPoint2.transform.localPosition = new Vector3(-fullSpace, 0, -fullSpace);
                beltPoint3.transform.localPosition = new Vector3(0, 0, -fullSpace * 2);
                break;
            case 5:
                beltPoint1.transform.localPosition = new Vector3(fullSpace, 0, -fullSpace);
                beltPoint2.transform.localPosition = new Vector3(-fullSpace, 0, -fullSpace);
                beltPoint3.transform.localPosition = new Vector3(halfSpace, 0, -fullSpace * 2);
                beltPoint4.transform.localPosition = new Vector3(-halfSpace, 0, -fullSpace * 2);
                break;
        }

        // Set follow point depending on current formation
        switch (formation)
        {
            case Formations.STANDARD:
                followPos1 = beltPoint1.transform.position;
                followPos2 = beltPoint2.transform.position;
                followPos3 = beltPoint3.transform.position;
                followPos4 = beltPoint4.transform.position;
                break;
            case Formations.COLUMN:
                followPos1 = tailManager.link1.position;
                followPos2 = tailManager.link2.position;
                followPos3 = tailManager.link3.position;
                followPos4 = tailManager.linkEnd.position;
                break;
            case Formations.STAGGEREDCOLUMNLEFT:
                // TODO
                beltPoint1.transform.localPosition = new Vector3(-fullSpace, 0, -halfSpace);
                beltPoint2.transform.localPosition = new Vector3(0, 0, -fullSpace + -thirdSpace);
                beltPoint3.transform.localPosition = new Vector3(-fullSpace, 0, -fullSpace + -thirdSpace + -halfSpace);
                beltPoint4.transform.localPosition = new Vector3(0, 0, (-fullSpace + -thirdSpace) * 2);
                Debug.Log("Entered Staggered Column Left Formation");
                break;
            case Formations.STAGGEREDCOLUMNRIGHT:
                // TODO
                beltPoint1.transform.localPosition = new Vector3(fullSpace, 0, -halfSpace);
                beltPoint2.transform.localPosition = new Vector3(0, 0, -fullSpace + -thirdSpace);
                beltPoint3.transform.localPosition = new Vector3(fullSpace, 0, -fullSpace + -thirdSpace + -halfSpace);
                beltPoint4.transform.localPosition = new Vector3(0, 0, (-fullSpace + -thirdSpace) * 2);
                Debug.Log("Entered Staggered Column Right Formation");
                break;
        }
    }

    private void IsThereEnoughSpace()
    {
        CheckUnitForEnoughSpace(beltPoint1.transform.position, 1);
        CheckUnitForEnoughSpace(beltPoint2.transform.position, 2);
        CheckUnitForEnoughSpace(beltPoint3.transform.position, 3);
        CheckUnitForEnoughSpace(beltPoint4.transform.position, 4);

        bool result = true;

        foreach (bool unitResult in spaceCheckArray)
        {
            if (unitResult == false)
            {
                result = false;
            }
        }

        Formations newFormation = result == true ? Formations.STANDARD : Formations.COLUMN;

        if (formation != newFormation)
        {
            formation = newFormation;
        }
    }

    private void CheckUnitForEnoughSpace(Vector3 originPos, int beltPos)
    {
        if (NavMesh.FindClosestEdge(originPos, out NavMeshHit hit, NavMesh.AllAreas))
        {
            spaceCheckArray[beltPos - 1] =
                !(Vector3.Distance(originPos, hit.position) <= enoughSpaceCheckRadius);
        }
    }

    public Formations GetFormation()
    {
        return formation;
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
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(GameObject.FindGameObjectWithTag("Player").transform.position, playerDeadzoneRadius);
            Gizmos.color = Color.green;
            if (FindObjectOfType<PartyManager>().partyMembers.Count >= 2)
                Gizmos.DrawWireSphere(beltPoint1.transform.position, memberDeadzoneRadius);
                DrawCircle(beltPoint1.transform.position, enoughSpaceCheckRadius, Color.red);
            if (FindObjectOfType<PartyManager>().partyMembers.Count >= 3)
                Gizmos.DrawWireSphere(beltPoint2.transform.position, memberDeadzoneRadius);
                DrawCircle(beltPoint2.transform.position, enoughSpaceCheckRadius, Color.red);
            if (FindObjectOfType<PartyManager>().partyMembers.Count >= 4)
                Gizmos.DrawWireSphere(beltPoint3.transform.position, memberDeadzoneRadius);
                DrawCircle(beltPoint3.transform.position, enoughSpaceCheckRadius, Color.red);
            if (FindObjectOfType<PartyManager>().partyMembers.Count >= 5)
                Gizmos.DrawWireSphere(beltPoint4.transform.position, memberDeadzoneRadius);
                DrawCircle(beltPoint4.transform.position, enoughSpaceCheckRadius, Color.red);
        }
    }
}
