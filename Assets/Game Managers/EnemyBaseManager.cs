using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyBaseManager : MonoBehaviour
{
    private static EnemyBaseManager _instance;

    public static EnemyBaseManager Instance { get { return _instance; } }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            _instance = this;
        }
    }

    [Header("Debug")]
    [SerializeField] private List<uint> baseEnemyTotals;
    [SerializeField] private List<bool> defeatedBases;

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
}
