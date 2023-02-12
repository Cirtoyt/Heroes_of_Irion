using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CombatParticleVisualiser : MonoBehaviour
{
    [SerializeField] private CombatParticleEffect numberParticleEffectPrefab;
    [SerializeField] private CombatParticleEffect damageParticleEffectPrefab;
    [SerializeField] private CombatParticleEffect healParticleEffectPrefab;

    private static CombatParticleVisualiser instance;
    public static CombatParticleVisualiser Instance { get => instance; }

    private void Awake()
    {
        instance = this;
    }

    public void SpawnDamageParticleEffects(Vector3 position, float damage, float scale = 0.5f)
    {
        // Damage particle effect
        CombatParticleEffect damageParticleEffect = Instantiate(damageParticleEffectPrefab, position, Quaternion.LookRotation(Camera.main.transform.forward, Camera.main.transform.up));
        damageParticleEffect.transform.localScale = Vector3.one * scale;

        // Number particle effect
        CombatParticleEffect numberParticleEffect = Instantiate(numberParticleEffectPrefab, position, Quaternion.LookRotation(Camera.main.transform.forward, Camera.main.transform.up));
        numberParticleEffect.EditText($"-{Mathf.RoundToInt(damage)}", "red");
        numberParticleEffect.TryGetComponent(out Rigidbody rb);
        if (rb)
        {
            rb.useGravity = true;

            float impulseRotationOffsetAngle = Random.Range(-numberParticleEffect.impulseAngleRange, numberParticleEffect.impulseAngleRange);
            Vector3 impulseVector = Quaternion.AngleAxis(impulseRotationOffsetAngle, Camera.main.transform.forward) * Vector3.up;
            rb.AddForce(impulseVector * numberParticleEffect.impulseForceStrength, ForceMode.VelocityChange);
        }
    }

    public void SpawnHealingParticleEffects(Vector3 position, float healAmount)
    {
        // Damage particle effect
        Instantiate(healParticleEffectPrefab, position, Quaternion.LookRotation(Camera.main.transform.forward, Camera.main.transform.up));

        // Number particle effect
        CombatParticleEffect numberParticleEffect = Instantiate(numberParticleEffectPrefab, position, Quaternion.LookRotation(Camera.main.transform.forward, Camera.main.transform.up));
        numberParticleEffect.EditText($"+{Mathf.RoundToInt(healAmount)}", "green");
        numberParticleEffect.TryGetComponent(out Rigidbody rb);
        if (rb)
        {
            rb.useGravity = false;
            
            rb.AddForce(Vector3.up * numberParticleEffect.impulseForceStrength, ForceMode.VelocityChange);
        }
    }
}
