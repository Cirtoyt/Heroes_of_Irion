using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EnemyBaseManager : MonoBehaviour
{
    private static EnemyBaseManager _instance;

    public static EnemyBaseManager Instance { get { return _instance; } }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            _instance = this;
        }
    }

    [Header("Variables")]
    [SerializeField] private Text totalDefeatedBasesText;
    [SerializeField] private List<Text> upgradeSlots;
    [SerializeField] private List<Reward> baseRewards;
    [SerializeField] private float bladeDamageMultiplierBonus;
    [SerializeField] private float magicDamageMultiplierBonus;
    [SerializeField] private float incomingDamageMultiplierBonus;
    [SerializeField] private float movementSpeedMultiplierBonus;
    [SerializeField] private GameObject kate;
    [SerializeField] private GameObject kateGhost;
    [SerializeField] private Transform kateCage;
    [SerializeField] private GameObject gabriel;
    [SerializeField] private GameObject gabrielGhost;
    [SerializeField] private Transform gabrielCage;
    [Header("Debug")]
    [SerializeField] private List<uint> baseEnemyTotals;
    [SerializeField] private List<bool> defeatedBases;
    [SerializeField] private List<Reward> currentUpgrades;
    [SerializeField] private float bladeDamageMultiplier;
    [SerializeField] private float magicDamageMultiplier;
    [SerializeField] private float incomingDamageMultiplier;
    [SerializeField] private float movementSpeedMultiplier;

    private enum Reward
    {
        Kate,
        Gabriel,
        SharpenedBlades,
        MagicalEye,
        OrbOfProtection,
        BootsOfSwiftness,
    }

    void Start()
    {
        // loop through all enemies and check their baseID to set total requirements
        object[] enemies = FindObjectsOfType(typeof(Enemy));
        foreach (object obj in enemies)
        {
            Enemy enemy = (Enemy)obj;
            if (enemy.GetBaseID() > 0 && enemy.GetBaseID() <= baseEnemyTotals.Count)
            {
                baseEnemyTotals[enemy.GetBaseID() - 1]++;
            }
        }

        // Reset upgrade text slots to empty
        totalDefeatedBasesText.text = "0 / " + baseEnemyTotals.Count;
        foreach (var slot in upgradeSlots)
        {
            slot.text = "";
        }
        upgradeSlots[0].text = "None";

        bladeDamageMultiplier = 1;
        magicDamageMultiplier = 1;
        incomingDamageMultiplier = 1;
        movementSpeedMultiplier = 1;
    }

    // Called everytime an enemy is killed
    public void UpdateRecords(int baseID)
    {
        // handling nemeies that don't belong to an enemy base
        if (baseID <= 0)
        {
            return;
        }

        // updating totals
        baseEnemyTotals[baseID - 1]--;

        // checking if an enemy base is now defeated
        if (baseEnemyTotals[baseID - 1] <= 0)
        {
            defeatedBases[baseID - 1] = true;
            GiveOutReward(baseRewards[baseID - 1]);
            currentUpgrades.Add(baseRewards[baseID - 1]);
            
            // Update UI
            int totalBasesDefeated = 0;
            foreach (bool state in defeatedBases)
            {
                if (state == true)
                {
                    totalBasesDefeated++;
                }
            }
            totalDefeatedBasesText.text = totalBasesDefeated.ToString() + " / " + baseEnemyTotals.Count;

            if (currentUpgrades.Count < baseEnemyTotals.Count)
            {
                for (int slot = 0; slot < currentUpgrades.Count; slot++)
                {
                    upgradeSlots[slot].text = currentUpgrades[slot].ToString();
                }
            }
        }

        // check for end-game state where all bases are defeated
        bool allBasesDefeated = true;
        foreach (bool state in defeatedBases)
        {
            if (state == false)
            {
                allBasesDefeated = false;
            }
        }

        if (allBasesDefeated)
        {
            // End game
            Debug.Log("Game has been completed!");
        }
    }

    private void GiveOutReward(Reward reward)
    {
        switch (reward)
        {
            case Reward.Kate:
                // enable character at reward point
                GameObject kateDoorClosed = kateCage.Find("Door Closed").gameObject;
                kateDoorClosed.SetActive(false);
                GameObject kateDoorOpened = kateCage.Find("Door Opened").gameObject;
                kateDoorOpened.SetActive(true);
                kateGhost.SetActive(false);
                kate.SetActive(true);
                break;
            case Reward.Gabriel:
                // enable character at reward point
                GameObject gabrielDoorClosed = gabrielCage.Find("Door Closed").gameObject;
                gabrielDoorClosed.SetActive(false);
                GameObject gabrielDoorOpened = gabrielCage.Find("Door Opened").gameObject;
                gabrielDoorOpened.SetActive(true);
                gabrielGhost.SetActive(false);
                gabriel.SetActive(true);
                break;
            case Reward.SharpenedBlades:
                // increase all blade damage output from player and party members by %
                bladeDamageMultiplier += bladeDamageMultiplierBonus;
                break;
            case Reward.MagicalEye:
                // increase all magic damage output from player and party members by %
                magicDamageMultiplier += magicDamageMultiplierBonus;
                break;
            case Reward.OrbOfProtection:
                // decrease all damage taken in from enemies by %
                incomingDamageMultiplier += incomingDamageMultiplierBonus;
                break;
            case Reward.BootsOfSwiftness:
                // increase movement speed by %
                movementSpeedMultiplier += movementSpeedMultiplierBonus;
                break;
        }
    }

    public float GetBladeDamageMultiplier()
    {
        return bladeDamageMultiplier;
    }

    public float GetMagicDamageMultiplier()
    {
        return magicDamageMultiplier;
    }

    public float GetIncomingDamageMultiplier()
    {
        return incomingDamageMultiplier;
    }

    public float GetMovementSpeedMultiplier()
    {
        return movementSpeedMultiplier;
    }
}
