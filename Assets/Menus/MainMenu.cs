using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    [SerializeField] private GameObject mainMenu;
    [SerializeField] private GameObject optionsMenu;
    [SerializeField] private Slider soundVolumeSlider;

    private void Start()
    {
        optionsMenu.SetActive(false);
        soundVolumeSlider.maxValue = AudioManager.Instance.GetSoundVolume();
    }

    public void PlayGame()
    {
        // Switch scene to GameScene
        SceneManager.LoadScene("GameScene");
        AudioManager.Instance.SwitchMusicSource("GameScene");
    }

    public void OpenOptions()
    {
        // Show options menu
        mainMenu.SetActive(false);
        optionsMenu.SetActive(true);
    }

    public void CloseOptions()
    {
        // Hide options menu and return to main menu
        optionsMenu.SetActive(false);
        mainMenu.SetActive(true);
    }

    public void QuitApplication()
    {
        Debug.Log("Application has been shutdown");
        Application.Quit();
    }
}
