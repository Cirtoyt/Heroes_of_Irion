using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PartyHUD : MonoBehaviour
{
    [SerializeField] private float smoothSpeed = 0.2f;
    [SerializeField] List<GameObject> partyMemberHUDS;
    [SerializeField] List<Sprite> avatars;

    private PartyManager partyManager;

    void Start()
    {
        partyManager = FindObjectOfType<PartyManager>();

        // Set all party member HUDs to hidden
        foreach (var HUD in partyMemberHUDS)
        {
            HUD.SetActive(false);
        }
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
            partyMemberHUDS[partyPosition - 1].SetActive(true);

            // Get squad member at party position
            SquadMember member = partyManager.GetSquadMemberFromPositionInParty(partyPosition);

            // Set avatar
            Image avatar = partyMemberHUDS[partyPosition - 1].transform.Find("Avatar Image").GetComponent<Image>();
            avatar.sprite = avatars[member.squadMemberUID - 1];
            avatar.color = new Color(1, 1, 1, 1);

            // Set text
            Text nameplate = partyMemberHUDS[partyPosition - 1].transform.Find("Nameplate Text").GetComponent<Text>();
            nameplate.text = member.gameObject.name + " The " + member.squadMemberClass.ToString();

            // Set health bar
            SetHealthBar(partyPosition, member.GetHealth(), member.GetMaxHealth());
        }
        else
        {
            partyMemberHUDS[partyPosition - 1].SetActive(false);
        }
    }

    public void SetHealthBar(int partyPosition, float newHealth, float maxHealth)
    {
        if (partyManager.partyMembers.Contains(partyManager.GetUIDFromPositionInParty(partyPosition)))
        {
            // Updates health bar slider for correct party members
            Image healthBar = partyMemberHUDS[partyPosition - 1].transform.Find("Health Fill").GetComponent<Image>();

            StartCoroutine(SmoothBarUI(healthBar, newHealth, maxHealth));
        }
    }

    private IEnumerator SmoothBarUI(Image healthBar, float _newHealth, float _maxHealth)
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
