using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    private static AudioManager _instance;

    public static AudioManager Instance { get { return _instance; } }

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

        DontDestroyOnLoad(this);

        soundVolume = 0.15f;
    }

    [SerializeField] private AudioSource mainMenuMusic;
    [SerializeField] private AudioSource inGameMusic;
    [Header("Debug")]
    [SerializeField] private float soundVolume;

    private AudioSource currentMusic;

    private void Start()
    {
        currentMusic = mainMenuMusic;
        AdjustVolume(soundVolume);
    }

    public void AdjustVolume(float value)
    {
        soundVolume = value;
        currentMusic.volume = value;
    }

    public void SwitchMusicSource(string eventName)
    {
        if (eventName == "GameScene")
        {
            mainMenuMusic.enabled = false;
            inGameMusic.enabled = true;
            currentMusic = inGameMusic;
        }
        else if (eventName == "MainMenuScene")
        {
            inGameMusic.enabled = false;
            mainMenuMusic.enabled = true;
            currentMusic = mainMenuMusic;
        }

        AdjustVolume(soundVolume);
    }

    public float GetSoundVolume()
    {
        return soundVolume;
    }
}
