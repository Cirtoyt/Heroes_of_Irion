using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    [SerializeField] private Image bar;
    [SerializeField] private float smoothSpeed = 0.2f;

    public void UpdateBarUI(float _newHealth, float _maxHealth)
    {
        StartCoroutine(SmoothBarUI(_newHealth, _maxHealth));
    }

    private IEnumerator SmoothBarUI(float _newHealth, float _maxHealth)
    {
        float newPercentage = _newHealth / _maxHealth;
        float preChangePercent = bar.fillAmount;
        float elapsed = 0f;

        while (elapsed < smoothSpeed)
        {
            elapsed += Time.deltaTime;
            bar.fillAmount = Mathf.Lerp(preChangePercent, newPercentage, elapsed / smoothSpeed);
            yield return null;
        }

        bar.fillAmount = newPercentage;
    }

    private void LateUpdate()
    {
        transform.parent.LookAt(Camera.main.transform);
    }
}
