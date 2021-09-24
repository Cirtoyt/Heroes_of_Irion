using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    [SerializeField] private Slider soundVolumeSlider;

    void Start()
    {
        soundVolumeSlider.onValueChanged.AddListener(delegate { SoundVolumeValueChanged(); });
        gameObject.SetActive(false);
    }

    private void SoundVolumeValueChanged()
    {
        AudioManager.Instance.AdjustVolume(soundVolumeSlider.value);
    }

    public void ExitToMainMenu()
    {
        SceneManager.LoadScene("MainMenuScene");
        AudioManager.Instance.SwitchMusicSource("MainMenuScene");
    }
}
