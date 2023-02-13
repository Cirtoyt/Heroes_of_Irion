using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum Actions
{
    ATTACK,
    REMOVEFROMPARTY,
    PRIORITISELARGEENEMIES,
    REGROUP,
    SWITCHCLASS,
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
    [SerializeField] private Image icon2;
    [SerializeField] private Image linkImage;

    private Color darkBaseColour;
    private Color darkHoverColour;
    private bool isOption1Selected = true;

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

    public void ToggleIcon(bool isOption1)
    {
        isOption1Selected = isOption1;
        UpdateToggleIconDims();
    }

    private void UpdateToggleIconDims()
    {
        if (isOption1Selected)
        {
            icon.color = new Color(1, 1, 1, 1f);
            if (icon2) icon2.color = new Color(1, 1, 1, 0.3f);
            if (linkImage) linkImage.color = new Color(1, 1, 1, 1f);
        }
        else
        {
            icon.color = new Color(1, 1, 1, 0.3f);
            if (icon2) icon2.color = new Color(1, 1, 1, 1f);
            if (linkImage) linkImage.color = new Color(1, 1, 1, 1f);
        }
    }

    public void ForceToggleIconsUndim()
    {
        icon.color = new Color(1, 1, 1, 1f);
        if (icon2) icon2.color = new Color(1, 1, 1, 1f);
        if (linkImage) linkImage.color = new Color(1, 1, 1, 1f);
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
        if (icon2) icon2.color = new Color(1, 1, 1, 0.3f);
        if (linkImage) linkImage.color = new Color(1, 1, 1, 0.3f);
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

        if (icon && icon2)
        {
            UpdateToggleIconDims();
        }
        else
        {
            icon.color = new Color(1, 1, 1, 1);
        }
    }
}
