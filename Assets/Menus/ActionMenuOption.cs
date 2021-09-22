using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum Actions
{
    ATTACK,
    REMOVEFROMPARTY,
    TAKECOVER,
    REGROUP,
    STATS,
    NONE,
}

public class ActionMenuOption : MonoBehaviour
{
    [SerializeField] private string actionTitle;
    [SerializeField] private Actions actionType;

    [SerializeField] private Color baseColour;
    [SerializeField] private Color hoverColour;
    [SerializeField] private float darkenAmount;
    [SerializeField] private Image background;
    [SerializeField] private Image icon;

    private Color darkBaseColour;
    private Color darkHoverColour;

    private void Awake()
    {
        background.color = baseColour;
    }

    public string GetTitle()
    {
        return actionTitle;
    }

    public Actions GetActionType()
    {
        return actionType;
    }

    public void Select()
    {
        background.color = hoverColour;
    }

    public void Deselect()
    {
        background.color = baseColour;
    }

    private void UpdateDarkColours()
    {
        darkBaseColour = new Color(baseColour.r - darkenAmount,
                                         baseColour.g - darkenAmount,
                                         baseColour.b - darkenAmount,
                                         baseColour.a);
        darkHoverColour = new Color(hoverColour.r - darkenAmount,
                                          hoverColour.g - darkenAmount,
                                          hoverColour.b - darkenAmount,
                                          hoverColour.a);
    }

    public void Dim()
    {
        UpdateDarkColours();

        if (background.color == baseColour)
        {
            background.color = darkBaseColour;
        }
        else if (background.color == hoverColour)
        {
            background.color = darkHoverColour;
        }

        icon.color = new Color(1, 1, 1, 0.3f);
    }

    public void UnDim()
    {
        if (background.color == darkBaseColour)
        {
            background.color = baseColour;
        }
        else if (background.color == darkHoverColour)
        {
            background.color = hoverColour;
        }

        icon.color = new Color(1, 1, 1, 1);
    }
}
