using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectivesMenuHandler : MonoBehaviour
{
    [SerializeField] private GameObject FoldOutMenu;
    [SerializeField] private GameObject ObjectivesMarker;

    private bool openState;

    private void Start()
    {
        HideObjectives();
    }

    public void ToggleOpenCloseMenu()
    {
        if (openState)
        {
            HideObjectives();
        }
        else
        {
            OpenObjectives();
        }
    }

    public void OpenObjectives()
    {
        ObjectivesMarker.SetActive(false);
        FoldOutMenu.SetActive(true);
        openState = true;
    }

    public void HideObjectives()
    {
        ObjectivesMarker.SetActive(true);
        FoldOutMenu.SetActive(false);
        openState = false;
    }
}
