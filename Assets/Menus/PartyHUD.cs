using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PartyHUD : MonoBehaviour
{
    [SerializeField] private float smoothSpeed = 0.2f;
    [SerializeField] List<GameObject> partyMemberHUDS;
    [SerializeField] public List<Sprite> avatars;

    private PartyManager partyManager;

    void Start()
    {
        partyManager = FindObjectOfType<PartyManager>();

        // Set all party member HUDs to hidden
        foreach (var HUD in partyMemberHUDS)
        {
            HUD.SetActive(false);
        }

        // begin game:
        partyMemberHUDS[0].SetActive(true);
    }
    
    void Update()
    {
        
    }

    // Adding party members to party & setting their avatars, names, and health bars on
    public void UpdateHUD(int partyPosition)
    {
        if (partyManager.partyMembers.Count > partyPosition)
        {
            // Enable HUD element on-screen
            partyMemberHUDS[partyPosition].SetActive(true);

            // Get squad member at party position
            SquadMember member = partyManager.GetSquadMemberFromPositionInParty(partyPosition);

            // Set avatar
            Image avatar = partyMemberHUDS[partyPosition].transform.Find("Avatar Image Mask").Find("Avatar Image").GetComponent<Image>();
            avatar.sprite = avatars[member.squadMemberUID];
            avatar.color = new Color(1, 1, 1, 1);

            // Set text
            Text nameplate = partyMemberHUDS[partyPosition].transform.Find("Nameplate Text").GetComponent<Text>();
            nameplate.text = member.gameObject.name + " The " + member.currentSquadMemberClass.ToString();

            // Set health bar
            SetHealthBar(partyPosition, member.GetHealth(), member.GetMaxHealth());
        }
        else
        {
            partyMemberHUDS[partyPosition].SetActive(false);
        }
    }

    public void AddHUDHighlight(int partyPosition)
    {
        partyMemberHUDS[partyPosition].transform.Find("Highlight Image").gameObject.SetActive(true);
    }

    public void RemoveHUDHighlight(int partyPosition)
    {
        partyMemberHUDS[partyPosition].transform.Find("Highlight Image").gameObject.SetActive(false);
    }

    public void RemoveAllHUDHighlights()
    {
        for (int i = 1; i < partyManager.partyMembers.Count; i++)
        {
            partyMemberHUDS[i].transform.Find("Highlight Image").gameObject.SetActive(false);
        }
    }

    public void SetHealthBar(int partyPosition, float newHealth, float maxHealth)
    {
        if (partyManager.partyMembers.Contains(partyManager.GetUIDFromPositionInParty(partyPosition)))
        {
            // Updates health bar slider for correct party members
            Image healthBar = partyMemberHUDS[partyPosition].transform.Find("Health Fill").GetComponent<Image>();

            StartCoroutine(SmoothBarUI(healthBar, newHealth, maxHealth));
        }
    }

    public IEnumerator SmoothBarUI(Image healthBar, float _newHealth, float _maxHealth)
    {
        float newPercentage = _newHealth / _maxHealth;
        float preChangePercent = healthBar.fillAmount;
        float elapsed = 0f;

        while (elapsed < smoothSpeed)
        {
            elapsed += Time.deltaTime;
            healthBar.fillAmount = Mathf.Lerp(preChangePercent, newPercentage, elapsed / smoothSpeed);
            yield return null;
        }

        healthBar.fillAmount = newPercentage;
    }
}
