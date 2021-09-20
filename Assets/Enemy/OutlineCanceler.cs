using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OutlineCanceler : MonoBehaviour
{
    private Outline outline;

    void Start()
    {
        outline = GetComponent<Outline>();
    }

    void Update()
    {
        if (outline.enabled)
        {
            outline.enabled = false;
        }
    }
}
