using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

// Helper methods used to set drawing settings
public class DrawingSettings : MonoBehaviour
{
    [SerializeField] Drawable _drawableCS;


    public static bool isCursorOverUI = false;
    public float Transparency = 1f;

    // Changing pen settings is easy as changing the static properties Drawable.Pen_Colour and Drawable.Pen_Width
    public void SetMarkerColour(Color new_color)
    {
        _drawableCS.Pen_Colour = new_color;
    }
    // new_width is radius in pixels
    public void SetMarkerWidth(int new_width)
    {
        _drawableCS.Pen_Width = new_width;
    }
    public void SetMarkerWidth(float new_width)
    {
        SetMarkerWidth((int)new_width);
    }

    public void SetTransparency(float amount)
    {
        Transparency = amount;
        Color c = _drawableCS.Pen_Colour;
        c.a = amount;
        _drawableCS.Pen_Colour = c;
    }


    // Call these these to change the pen settings
    public void SetPenRed()
    {
        Color c = Color.red;
        c.a = Transparency;
        SetMarkerColour(c);
        Drawable.drawable.SetPenBrush();
    }
    public void SetPenGreen()
    {
        Color c = Color.green;
        c.a = Transparency;
        SetMarkerColour(c);
        Drawable.drawable.SetPenBrush();
    }
    public void SetPenBlue()
    {
        Color c = Color.blue;
        c.a = Transparency;
        SetMarkerColour(c);
        Drawable.drawable.SetPenBrush();
    }

    public void SetPenYellow()
    {
        Color c = Color.yellow;
        c.a = Transparency;
        SetMarkerColour(c);
        Drawable.drawable.SetPenBrush();
    }

    public void SetEraser()
    {
        //SetMarkerColour(new Color(255f, 255f, 255f, 0f));

        SetMarkerColour(Color.white); // Equivalent to new Color(1f, 1f, 1f, 1f)
    }

    public void PartialSetEraser()
    {
        SetMarkerColour(new Color(255f, 255f, 255f, 0.5f));
    }

    public void SetFillBrush()
    {
        Drawable.drawable.SetFillBrush();
    }
}
