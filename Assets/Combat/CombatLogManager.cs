using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CombatLogManager : MonoBehaviour
{
    [SerializeField] private GameObject singleLineLogPrefab;
    [SerializeField] private Transform container;
    [SerializeField] private int maxLogsToDisplay = 7;
    [SerializeField] private bool printDebugLogs = true;

    private static CombatLogManager instance;

    public static CombatLogManager Instance { get => instance; }

    private List<GameObject> singleLineLogs = new List<GameObject>();

    private void Awake()
    {
        instance = this;
    }

    private void AddLogToList(GameObject log)
    {
        singleLineLogs.Add(log);
        if (singleLineLogs.Count > maxLogsToDisplay)
        {
            GameObject logToDelete = singleLineLogs[0];
            singleLineLogs.RemoveAt(0);
            Destroy(logToDelete);
        }
    }

    public void PrintAttackLog(string attacker, bool attackerIsGood, string damageTaker, bool damageTakerIsGood, float damage)
    {
        GameObject singleLineLog = Instantiate(singleLineLogPrefab, container);
        singleLineLog.name = "Single Line Log";

        string attackerIsGoodColorFormatOpener = attackerIsGood ? "<color=cyan>" : "<color=orange>";
        string damagerTakerIsGoodColorFormatOpener = damageTakerIsGood ? "<color=cyan>" : "<color=orange>";

        if (attacker == "Alex") attackerIsGoodColorFormatOpener = "<color=magenta>";
        if (damageTaker == "Alex") damagerTakerIsGoodColorFormatOpener = "<color=magenta>";

        string formattedText = $"{attackerIsGoodColorFormatOpener}{attacker}</color> attacks {damagerTakerIsGoodColorFormatOpener}{damageTaker}</color> <color=red>(-{damage})</color>";

        singleLineLog.transform.Find("Text").GetComponent<Text>().text = formattedText;

        AddLogToList(singleLineLog);

        if (printDebugLogs) Debug.Log(formattedText);
    }

    public void PrintHealLog(string healer, bool healerIsGood, string healingTaker, bool healingTakerIsGood, float healAmount)
    {
        GameObject singleLineLog = Instantiate(singleLineLogPrefab, container);
        singleLineLog.name = "Single Line Log";

        string healerIsGoodColorFormatOpener = healerIsGood ? "<color=cyan>" : "<color=orange>";
        string healingTakerIsGoodColorFormatOpener = healingTakerIsGood ? "<color=cyan>" : "<color=orange>";

        if (healer == "Alex") healerIsGoodColorFormatOpener = "<color=magenta>";
        if (healingTaker == "Alex") healingTakerIsGoodColorFormatOpener = "<color=magenta>";

        string formattedText = $"{healerIsGoodColorFormatOpener}{healer}</color> heals {healingTakerIsGoodColorFormatOpener}{healingTaker}</color> <color=green>(+{healAmount})</color>";

        singleLineLog.transform.Find("Text").GetComponent<Text>().text = formattedText;

        AddLogToList(singleLineLog);

        if (printDebugLogs) Debug.Log(formattedText);
    }

    public void PrintAddedToPartyLog(string partyMember)
    {
        GameObject singleLineLog = Instantiate(singleLineLogPrefab, container);
        singleLineLog.name = "Single Line Log";

        string formattedText = $"<color=cyan>{partyMember}</color> has been <color=green>added</color> to the <color=magenta>party</color>";

        singleLineLog.transform.Find("Text").GetComponent<Text>().text = formattedText;

        AddLogToList(singleLineLog);
        
        if (printDebugLogs) Debug.Log(formattedText);
    }

    public void PrintRemovedToPartyLog(string partyMember)
    {
        GameObject singleLineLog = Instantiate(singleLineLogPrefab, container);
        singleLineLog.name = "Single Line Log";

        string formattedText = $"<color=cyan>{partyMember}</color> has been <color=red>removed</color> from the <color=magenta>party</color>";

        singleLineLog.transform.Find("Text").GetComponent<Text>().text = formattedText;

        AddLogToList(singleLineLog);
        
        if (printDebugLogs) Debug.Log(formattedText);
    }

    public void PrintTargetNewEnemyLog(string partyMember, string enemy)
    {
        GameObject singleLineLog = Instantiate(singleLineLogPrefab, container);
        singleLineLog.name = "Single Line Log";

        string formattedText = $"<color=cyan>{partyMember}</color> will <color=purple>now attack</color> <color=orange>{enemy}</color>";

        singleLineLog.transform.Find("Text").GetComponent<Text>().text = formattedText;

        AddLogToList(singleLineLog);

        if (printDebugLogs) Debug.Log(formattedText);
    }

    public void PrintRegroupLog(string partyMember)
    {
        GameObject singleLineLog = Instantiate(singleLineLogPrefab, container);
        singleLineLog.name = "Single Line Log";

        string formattedText = $"<color=cyan>{partyMember}</color> is now <color=purple>regrouping</color>";

        singleLineLog.transform.Find("Text").GetComponent<Text>().text = formattedText;

        AddLogToList(singleLineLog);

        if (printDebugLogs) Debug.Log(formattedText);
    }
}
