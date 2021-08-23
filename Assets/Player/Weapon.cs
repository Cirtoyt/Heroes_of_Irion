using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Weapon : MonoBehaviour
{
    [HideInInspector] public bool canHit;

    private new Renderer renderer;
    private Player player;

    private void Start()
    {
        renderer = GetComponent<Renderer>();

        if (GetComponentInParent<Player>())
        {
            player = GetComponentInParent<Player>();
        }

        canHit = false;
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
        if (player && canHit
            && other.transform.TryGetComponent(out Enemy enemy))
        {
            canHit = false;
            player.DealDamage(enemy);
        }
    }
}
