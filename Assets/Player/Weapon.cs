using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Weapon : MonoBehaviour
{
    private new Renderer renderer;
    private Player player;

    private void Start()
    {
        renderer = GetComponent<Renderer>();

        if (GetComponentInParent<Player>())
        {
            player = GetComponentInParent<Player>();
        }
    }

    public void RevealWeapon()
    {
        // May need to get multiple rendered for weapons with multiple parts (staff)
        renderer.enabled = true;
    }

    public void HideWeapon()
    {
        renderer.enabled = false;
    }

    private void OnTriggerStay(Collider other)
    {
        if (player && player.IsAttacking() && other.TryGetComponent(out Enemy enemy))
        {
            player.AddHitTarget(enemy);
        }
    }
}
