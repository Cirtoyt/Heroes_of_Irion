using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TailManager : MonoBehaviour
{
    public Transform link1;
    public Transform link2;
    public Transform link3;
    public Transform linkEnd;

    [HideInInspector] public float tailLength;

    private SquadBelt sbc;

    private void Awake()
    {
        sbc = FindObjectOfType<SquadBelt>();
    }

    private void Update()
    {
        tailLength = sbc.formationSpacing;
    }
}
