using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Collider2D))]  

public class Drawable : MonoBehaviour
{
    [Header("Reference from itself")]
    public InteractableWhiteboard interactableWhiteboardCS;

    // PEN COLOUR
    public Color Pen_Colour = Color.red;     // Change these to change the default drawing settings
                                                    // PEN WIDTH (actually, it's a radius, in pixels)
    public int Pen_Width = 3;


    public delegate void Brush_Function(Vector2 world_position);
    // This is the function called when a left click happens
    // Pass in your own custom one to change the brush type
    // Set the default function in the Awake method
    public Brush_Function current_brush;

    public LayerMask Drawing_Layers;

    public bool Reset_Canvas_On_Play = true;
    // The colour the canvas is reset to each time
    public Color Reset_Colour = new Color(0, 0, 0, 0);  // By default, reset the canvas to be transparent

    public bool Reset_To_This_Texture_On_Play = false;  // If true, will reset the image back to whatever reset texture is
    public Texture2D reset_texture;

    // Used to reference THIS specific file without making all methods static
    public static Drawable drawable;
    // MUST HAVE READ/WRITE enabled set in the file editor of Unity
    Sprite drawable_sprite;
    Texture2D drawable_texture;

    Vector2 previous_drag_position;
    Color[] clean_colours_array;
    Color transparent;
    Color32[] cur_colors;
    bool mouse_was_previously_held_down = false;
    bool no_drawing_on_current_drag = false;

    [Header("Read-Only")]
    public bool canWeDraw; //modified by InteractableWhiteboard.cs


    #region Unity Functions

    void Awake()
    {
        drawable = this;
        // DEFAULT BRUSH SET HERE
        current_brush = PenBrush;

        drawable_sprite = this.GetComponent<SpriteRenderer>().sprite;
        drawable_texture = drawable_sprite.texture;

        // Initialize clean pixels to use
        clean_colours_array = new Color[(int)drawable_sprite.rect.width * (int)drawable_sprite.rect.height];
        for (int x = 0; x < clean_colours_array.Length; x++)
            clean_colours_array[x] = Reset_Colour;

        // Should we reset our canvas image when we hit play in the editor?
        if (Reset_Canvas_On_Play)
        {
            ResetCanvas();
        }
        else if (Reset_To_This_Texture_On_Play)
        {
            Graphics.CopyTexture(reset_texture, drawable_texture);
            //drawable_texture = reset_texture;
            Debug.Log("Reset texture");
        }
    }

    void Update()
    {
        //Only write if we are the localplayer with access
        if (!canWeDraw)
        {
            return;
        }


        // 1. Check if the mouse (or primary touch) is held down
        bool mouse_held_down = Pointer.current != null && Pointer.current.press.isPressed;

        if (mouse_held_down && !no_drawing_on_current_drag)
        {
            // 2. Read the screen position from the active pointer device
            Vector2 screen_position = Pointer.current.position.ReadValue();

            // NEW: Create a 3D ray from the Perspective Camera
            Ray ray = Camera.main.ScreenPointToRay(screen_position);

            // NEW: Use GetRayIntersection to shoot a 3D ray against 2D Colliders
            RaycastHit2D hit = Physics2D.GetRayIntersection(ray, Mathf.Infinity, Drawing_Layers.value);

            if (hit.collider != null && hit.transform != null)
            {
                // We're over the texture! Pass the exact world hit point.
                current_brush(hit.point);
            }
            else
            {
                // We're not over our destination texture
                previous_drag_position = Vector2.zero;
                if (!mouse_was_previously_held_down)
                {
                    // This is a new drag where the user is left clicking off the canvas
                    no_drawing_on_current_drag = true;
                }
            }
        }
        // Mouse is released
        else if (!mouse_held_down)
        {
            previous_drag_position = Vector2.zero;
            no_drawing_on_current_drag = false;
        }

        mouse_was_previously_held_down = mouse_held_down;
    }
    
    #endregion

    //////////////////////////////////////////////////////////////////////////////
    // BRUSH TYPES. Implement your own here
    // How to write your own brush method:
    // 1. Copy and rename the BrushTemplate() method below with your own brush
    // 2. Write your own code inside of this method
    // 3. Assign this method to the current_brush variable (see how PenBrush does this)


    // When you want to make your own type of brush effects,
    // Copy, paste and rename this function.
    // Go through each step
    public void BrushTemplate(Vector2 world_position)
    {
        // 1. Change world position to pixel coordinates
        Vector2 pixel_pos = WorldToPixelCoordinates(world_position);

        // 2. Make sure our variable for pixel array is updated in this frame
        cur_colors = drawable_texture.GetPixels32();

        ////////////////////////////////////////////////////////////////
        // FILL IN CODE BELOW HERE

        // Do we care about the user left clicking and dragging?
        // If you don't, simply set the below if statement to be:
        //if (true)

        // If you do care about dragging, use the below if/else structure
        if (previous_drag_position == Vector2.zero)
        {
            // THIS IS THE FIRST CLICK
            // FILL IN WHATEVER YOU WANT TO DO HERE
            // Maybe mark multiple pixels to colour?
            MarkPixelsToColour(pixel_pos, Pen_Width, Pen_Colour);
        }
        else
        {
            // THE USER IS DRAGGING
            // Should we do stuff between the previous mouse position and the current one?
            ColourBetween(previous_drag_position, pixel_pos, Pen_Width, Pen_Colour);
        }
        ////////////////////////////////////////////////////////////////

        // 3. Actually apply the changes we marked earlier
        // Done here to be more efficient
        ApplyMarkedPixelChanges();

        // 4. If dragging, update where we were previously
        previous_drag_position = pixel_pos;
    }

    // Default brush type. Has width and colour.
    // Pass in a point in WORLD coordinates
    // Changes the surrounding pixels of the world_point to the static pen_colour
    public void PenBrush(Vector2 world_point)
    {
        Vector2 pixel_pos = WorldToPixelCoordinates(world_point);

        cur_colors = drawable_texture.GetPixels32();

        if (previous_drag_position == Vector2.zero)
        {
            // If this is the first time we've ever dragged on this image, simply colour the pixels at our mouse position
            MarkPixelsToColour(pixel_pos, Pen_Width, Pen_Colour);
        }
        else
        {
            // Send stroke to Fusion instead of drawing directly locally
            if (interactableWhiteboardCS != null)
            {
                interactableWhiteboardCS.BroadcastStroke(previous_drag_position, pixel_pos, Pen_Colour, Pen_Width);
            }

            // Colour in a line from where we were on the last update call
            ColourBetween(previous_drag_position, pixel_pos, Pen_Width, Pen_Colour);
        }
        ApplyMarkedPixelChanges();

        //Debug.Log("Dimensions: " + pixelWidth + "," + pixelHeight + ". Units to pixels: " + unitsToPixels + ". Pixel pos: " + pixel_pos);
        previous_drag_position = pixel_pos;
    }

    public void ResetCanvas()
    {
        drawable_texture.SetPixels(clean_colours_array);
        drawable_texture.Apply();

        // Reset cur_colors buffer so stale pixels aren't re-applied on the next stroke
        cur_colors = drawable_texture.GetPixels32();
    }
    // Helper method used by UI to set what brush the user wants
    // Create a new one for any new brushes you implement
    public void SetPenBrush()
    {
        // PenBrush is the NAME of the method we want to set as our current brush
        current_brush = PenBrush;
    }

    // FILL BRUSH, from Francesco Filipini
    public void FillBrush(Vector2 world_position)
    {
        Vector2 pixel_pos = WorldToPixelCoordinates(world_position);
        int x = (int)pixel_pos.x;
        int y = (int)pixel_pos.y;

        Color target_color = drawable_texture.GetPixel(x, y);
        if (target_color == Pen_Colour) return;

        Queue<Vector2> pixels = new Queue<Vector2>();
        pixels.Enqueue(new Vector2(x, y));

        while (pixels.Count > 0)
        {
            Vector2 current_pixel = pixels.Dequeue();
            int cx = (int)current_pixel.x;
            int cy = (int)current_pixel.y;

            if (cx < 0 || cx >= drawable_texture.width || cy < 0 || cy >= drawable_texture.height) continue;
            if (drawable_texture.GetPixel(cx, cy) != target_color) continue;

            drawable_texture.SetPixel(cx, cy, Pen_Colour);

            pixels.Enqueue(new Vector2(cx + 1, cy));
            pixels.Enqueue(new Vector2(cx - 1, cy));
            pixels.Enqueue(new Vector2(cx, cy + 1));
            pixels.Enqueue(new Vector2(cx, cy - 1));
        }

        drawable_texture.Apply();
    }

    public void SetFillBrush()
    {
        current_brush = FillBrush;
    }

    // Set the colour of pixels in a straight line from start_point all the way to end_point, to ensure everything inbetween is coloured
    public void ColourBetween(Vector2 start_point, Vector2 end_point, int width, Color color)
    {
        // Get the distance from start to finish
        float distance = Vector2.Distance(start_point, end_point);
        Vector2 direction = (start_point - end_point).normalized;

        Vector2 cur_position = start_point;

        // Calculate how many times we should interpolate between start_point and end_point based on the amount of time that has passed since the last update
        float lerp_steps = 1 / distance;

        for (float lerp = 0; lerp <= 1; lerp += lerp_steps)
        {
            cur_position = Vector2.Lerp(start_point, end_point, lerp);
            MarkPixelsToColour(cur_position, width, color);
        }
    }
    public void MarkPixelsToColour(Vector2 center_pixel, int pen_thickness, Color color_of_pen)
    {
        // FIX 1: Ensure cur_colors is populated on all clients before writing
        if (cur_colors == null || cur_colors.Length == 0)
        {
            cur_colors = drawable_texture.GetPixels32();
        }

        int center_x = (int)center_pixel.x;
        int center_y = (int)center_pixel.y;

        for (int x = center_x - pen_thickness; x <= center_x + pen_thickness; x++)
        {
            if (x >= (int)drawable_sprite.rect.width || x < 0)
                continue;

            for (int y = center_y - pen_thickness; y <= center_y + pen_thickness; y++)
            {
                MarkPixelToChange(x, y, color_of_pen);
            }
        }
    }
    public void MarkPixelToChange(int x, int y, Color color)
    {
        int array_pos = y * (int)drawable_sprite.rect.width + x;

        // FIX 2: Guard against null array and fix array_pos out of bounds check (>= instead of >)
        if (cur_colors == null || array_pos >= cur_colors.Length || array_pos < 0)
            return;

        cur_colors[array_pos] = color;
    }
    public void ApplyMarkedPixelChanges()
    {
        drawable_texture.SetPixels32(cur_colors);
        drawable_texture.Apply();
    }


    // Directly colours pixels. This method is slower than using MarkPixelsToColour then using ApplyMarkedPixelChanges
    // SetPixels32 is far faster than SetPixel
    // Colours both the center pixel, and a number of pixels around the center pixel based on pen_thickness (pen radius)
    public void ColourPixels(Vector2 center_pixel, int pen_thickness, Color color_of_pen)
    {
        // Figure out how many pixels we need to colour in each direction (x and y)
        int center_x = (int)center_pixel.x;
        int center_y = (int)center_pixel.y;
        //int extra_radius = Mathf.Min(0, pen_thickness - 2);

        for (int x = center_x - pen_thickness; x <= center_x + pen_thickness; x++)
        {
            for (int y = center_y - pen_thickness; y <= center_y + pen_thickness; y++)
            {
                drawable_texture.SetPixel(x, y, color_of_pen);
            }
        }

        drawable_texture.Apply();
    }
    public Vector2 WorldToPixelCoordinates(Vector2 world_position)
    {
        // Change coordinates to local coordinates of this image
        // InverseTransformPoint automatically factors out your exact Transform scale
        Vector3 local_pos = transform.InverseTransformPoint(world_position);

        float pixelWidth = drawable_sprite.rect.width;
        float pixelHeight = drawable_sprite.rect.height;

        // Get the Sprite's Pixels Per Unit to calculate exact offsets
        float ppu = drawable_sprite.pixelsPerUnit;

        // Calculate pixel coordinates using purely the sprite's local space
        float centered_x = (local_pos.x * ppu) + (pixelWidth / 2f);
        float centered_y = (local_pos.y * ppu) + (pixelHeight / 2f);

        return new Vector2(Mathf.RoundToInt(centered_x), Mathf.RoundToInt(centered_y));
    }

    // Some guy requested this - it might be wrong
    public Vector3 PixelToWorldCoordinates(Vector2 pixel_pos)
    {
        float pixelWidth = drawable_sprite.rect.width;
        float pixelHeight = drawable_sprite.rect.height;
        float unitsToPixels = pixelWidth / drawable_sprite.bounds.size.x * transform.localScale.x;

        // Need to uncenter our coordinates
        float uncentered_x = pixel_pos.x / unitsToPixels - pixelWidth / 2;
        float uncentered_y = pixel_pos.y / unitsToPixels - pixelHeight / 2;

        // Convert point to world space
        Vector3 world_pos = transform.TransformPoint(new Vector3(uncentered_x, uncentered_y, 0f));
        return world_pos;
    }

}
