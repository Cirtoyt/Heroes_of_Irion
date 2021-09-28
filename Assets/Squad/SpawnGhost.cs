using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SpawnGhost : MonoBehaviour
{
    [SerializeField] private SquadMember memberScript;
    [SerializeField] private Text timerUIText;
    public float respawnDelay;

    private GameObject characterModel;
    private GameObject timerUI;
    private float timer;
    private bool runTimer;

    private void Start()
    {
        characterModel = transform.Find("Character Model").gameObject;
        timerUI = transform.Find("Canvas").gameObject;
        characterModel.SetActive(false);
        timer = 0;
        runTimer = false;
    }

    public void BeginRespawnTimer()
    {
        runTimer = true;
        characterModel.SetActive(true);
        timerUI.SetActive(true);
    }

    private void Update()
    {
        if (runTimer)
        {
            timer += Time.deltaTime;

            if (timer >= respawnDelay)
            {
                timer = 0;
                runTimer = false;
                characterModel.SetActive(false);
                timerUI.SetActive(false);
                memberScript.RespawnMember();
            }

            // Update visual respawn timer number display
            timerUI.transform.LookAt(transform.position + Camera.main.transform.forward);
            int roundDownTimer = Mathf.CeilToInt(respawnDelay - timer);
            timerUIText.text = roundDownTimer.ToString();
        }
    }

    public void ForceRespawn()
    {
        if (runTimer)
        {
            timer = 0;
            runTimer = false;
            characterModel.SetActive(false);
            timerUI.SetActive(false);
            memberScript.RespawnMember();
        }
    }
}
