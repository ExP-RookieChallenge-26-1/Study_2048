using System.Collections;
using System;
using UnityEngine;
using UnityEngine.UI;


public class Simple2048TileView : MonoBehaviour
{
    public RectTransform RectTransform;
    public Image Image;
    public Text Text;

    public int GridX;
    public int GridY;

    public void SetValue(int value, Color color)
    {
        if (Image != null)
            Image.color = color;

        if (Text != null)
        {
            Text.text = value == 0 ? "" : value.ToString();
            Text.fontSize = value >= 1024 ? 20 : 28;
            Text.color = value <= 4
                ? new Color(0.35f, 0.32f, 0.30f)
                : Color.white;
        }
    }
}