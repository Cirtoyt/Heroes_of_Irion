using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CombatParticleEffect : MonoBehaviour
{
    [Header("Statics")]
    [SerializeField] private TextMeshProUGUI text;
    [Header("Variables")]
    [SerializeField] private float destroyDelay = 0.467f;
    public float impulseForceStrength = 0.5f;
    public float impulseAngleRange = 0.25f;
    [SerializeField] private float fadeToTransparentSpeed = 0.1f;
    [SerializeField] private float initialAlphaValue = 1.5f;

    private float alphaValue;

    private void Start()
    {
        TryGetComponent(out Canvas canvas);
        Camera[] cameras = FindObjectsOfType<Camera>();
        Camera uiCamera = null;
        foreach (Camera camera in cameras)
        {
            if (camera.name == "UI Camera")
            {
                uiCamera = camera;
                break;
            }
        }

        if (canvas && uiCamera) canvas.worldCamera = uiCamera;

        alphaValue = initialAlphaValue;

        Destroy(gameObject, destroyDelay);
    }

    private void Update()
    {
        transform.LookAt(transform.position + Camera.main.transform.forward);

        if (text)
        {
            alphaValue -= fadeToTransparentSpeed * Time.deltaTime;
            text.color = new Color(text.color.r, text.color.g, text.color.b, Mathf.Clamp01(alphaValue));
        }
    }

    /// <param name="colourValue">the string that will fit within the color styling</param>
    public void EditText(string newText, string colourValue)
    {
        if (text) text.text = $"<color={colourValue}>{newText}</color>";
    }
}
