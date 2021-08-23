using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PartyManager : MonoBehaviour
{
    public List<int> partyMembers;

    public void RemoveMemberFromParty(int _memberUID)
    {
        int extractPos = 0;

        foreach (int memberUID in partyMembers)
        {
            if (memberUID != 0 && memberUID == _memberUID)
            {
                partyMembers.RemoveAt(extractPos);
                break;
            }

            extractPos++;
        }

        SquadMember[] members = FindObjectsOfType<SquadMember>();
        foreach (SquadMember member in members)
        {
            member.UpdatePartyPositionVar();
        }
    }

    public void AddMemberToParty(int _memberUID)
    {
        bool inPartyAlready = false;

        foreach (int memberUID in partyMembers)
        {
            if (memberUID == _memberUID)
            {
                Debug.LogWarning("Member is already in party!");
                inPartyAlready = true;
                break;
            }
        }

        if (partyMembers.Count < 5 && inPartyAlready == false)
        {
            partyMembers.Add(_memberUID);

            SquadMember[] members = FindObjectsOfType<SquadMember>();
            foreach (SquadMember member in members)
            {
                if (member.squadMemberUID == _memberUID)
                    member.UpdatePartyPositionVar();
            }
        }
        else
        {
            if (partyMembers.Count >= 5)
                Debug.LogWarning("Cannot add more than 5 members to party");
        }
    }

    public int GetPositionInParty(int _memberUID)
    {
        int inPartyPos = 0;

        foreach (int memberUID in partyMembers)
        {
            if (memberUID != 0 && memberUID == _memberUID)
            {
                return inPartyPos;
            }

            inPartyPos++;
        }

        // Else, could not find member in party
        return -1;
    }

    public int GetUIDFromPositionInParty(int _partyPosition)
    {
        if (_partyPosition < partyMembers.Count)
        {
            return partyMembers[_partyPosition];
        }
        else
        {
            Debug.LogWarning("Requested party position is outside scope");
            return -1;
        }
    }

    public SquadMember GetSquadMemberFromPositionInParty(int _partyPosition)
    {
        if (_partyPosition < partyMembers.Count)
        {
            int UID = partyMembers[_partyPosition];

            SquadMember[] members = FindObjectsOfType<SquadMember>();
            foreach (SquadMember member in members)
            {
                if (member.squadMemberUID == UID)
                {
                    return member;
                }
            }
        }
        return null;
    }
}
