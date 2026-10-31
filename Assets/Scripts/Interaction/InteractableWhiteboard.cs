using Fusion;
using System.Collections;
using UnityEngine;

//Lightweight 32-byte struct to represent stroke data
public struct DrawStroke : INetworkStruct
{
    public Vector2 StartPixel;
    public Vector2 EndPixel;
    public Color32 Color;
    public int Width;
}

public class InteractableWhiteboard : BaseInteractable
{
    [Header("Necessary SO")]
    [SerializeField] InteractableWhiteboardRefSO _interactableWhiteboardSO;
    [SerializeField] InteractableChairRefSO _interactableChairSO;

    [Header("Reference from itself")]
    [SerializeField] Drawable _drawableCS;

    [Header("Reference from Its Child")]
    [SerializeField] GameObject _whiteboardCanvas;
    [SerializeField] GameObject _viewerCanvas;
    [SerializeField] Transform _standPosition;

    // 2. Networked stroke history for late-joiners (holds up to 1000 strokes)
    [Networked, Capacity(1000)]
    private NetworkLinkedList<DrawStroke> StrokeHistory => default;


    #region Unity Functions

    private void Awake()
    {
        _whiteboardCanvas.SetActive(false);
        _viewerCanvas.SetActive(false);
    }

    private void OnEnable()
    {
        _interactableWhiteboardSO.Instance = this;
    }

    private void OnDisable()
    {
        _interactableWhiteboardSO.Instance = null;

        if (currentlyInUsed)
        {
            if (Object.HasStateAuthority)
            {
                ClickExit();
            }
            else
            {
                ClickViewerExit();
            }
        }
    }

    private void OnDestroy()
    {
        _interactableWhiteboardSO.Instance = null;
    }
    #endregion

    #region GameEvent Overrides

    // Called when Interactable is collided with player
    public override void CallWhenInteracted()
    {
        base.CallWhenInteracted();
    }

    //Calls in ReactAction2 when a player want to view the pdf

    public override void ViewerInteraction()
    {
        base.ViewerInteraction();
        _viewerCanvas.SetActive(true);
    }

    protected override void OnLevelRequirementPassed()
    {
        base.OnLevelRequirementPassed();

        //Focus the Interactable Camera on this one, we cannot add this one Abstract Class because not all interactable need focus immediately like chairs
        FocusInteractableCamera(10);
        _gameManagerSO.Instance.TogglePlayerCanvas(false);
        _whiteboardCanvas.SetActive(true);
        _drawableCS.canWeDraw = true;

        StartCoroutine(DelayLevelPassed());
    }

    IEnumerator DelayLevelPassed()
    {
        yield return new WaitForSeconds(1);
        _playerManagerSO.Instance.RepositionPlayer(_standPosition); //Reposition Player it includes the rotation already                                                                  
        //Camera.main.cullingMask = ~LayerMask.GetMask("Player");// Render EVERYTHING EXCEPT the "Player" layer
    }

    protected override void OnLevelRequirementFailed()
    {
        base.OnLevelRequirementFailed();

        Debug.Log("Displaying 'Level Too Low' notification UI to player.");
        _drawableCS.canWeDraw = false;
    }

    public override void ExitInteractable()
    {
        base.ExitInteractable();

        _gameManagerSO.Instance.TogglePlayerCanvas(true);
        _whiteboardCanvas.SetActive(false);
        _drawableCS.canWeDraw = false;
        //Camera.main.cullingMask = -1;
    }
    #endregion

    #region Button Click

    public void ClickExit()
    {
        ExitInteractable();
    }

    public void ClickViewerExit()
    {
        if(_interactableChairSO != null) //We Mark the Chair Interactable 2nd interaction disable, cause even tho this is Whiteboard the Viewer can only access this using in InteractableChair.cs
        {
            _interactableChairSO.Instance.ExitInteractable2();
        }
        else
        {
            Debug.LogError("There is no Chair Reference");
        }

        _viewerCanvas.SetActive(false);
        FocusInteractableCamera(0);
    }


    public void ClickClearBoard() //Calls by ClearBoard_Btn
    {
        _drawableCS.ResetCanvas();
        RPC_ClearCanvas();
    }

    #endregion

    #region Stroke Functions
    public void BroadcastStroke(Vector2 startPixel, Vector2 endPixel, Color color, int width)
    {
        // Warn and block new strokes if history is almost full
        if (StrokeHistory.Count >= 950)
        {
            _rememberMeSO.Instance.EnableWarning("Whiteboard memory limit reached (900+ strokes)! Please clear the board first before writing again.");
            return;
        }

        // Only allow local input authority to trigger RPCs
        if (Object.HasInputAuthority || Object.HasStateAuthority)
        {
            DrawStroke stroke = new DrawStroke
            {
                StartPixel = startPixel,
                EndPixel = endPixel,
                Color = color,
                Width = width
            };

            RPC_SendStroke(startPixel, endPixel, color, width);
        }
    }
    // Helper method to draw the stroke on the local Drawable component
    private void ApplyStrokeToCanvas(DrawStroke stroke)
    {
        _drawableCS.Pen_Colour = stroke.Color;
        _drawableCS.Pen_Width = stroke.Width;
        _drawableCS.ColourBetween(stroke.StartPixel, stroke.EndPixel, stroke.Width, stroke.Color);
        _drawableCS.ApplyMarkedPixelChanges();
    }
    #endregion

    #region Fusion Callbacks
    public override void Spawned()
    {
        // When a new player joins mid-game, Fusion syncs StrokeHistory automatically.
        // Replay all strokes locally onto their canvas.
        foreach (var stroke in StrokeHistory)
        {
            ApplyStrokeToCanvas(stroke);
        }

        // If a non-host player joins late, request stroke history from State Authority
        if (!Object.HasStateAuthority)
        {
            RPC_RequestStrokeHistory(Runner.LocalPlayer);
        }
    }
    #endregion

    #region RPCS

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_ClearCanvas()
    {
        // 1. Clear saved history on Host so late-joiners don't download old drawings
        StrokeHistory.Clear();

        // 2. Broadcast to all connected clients to clear their visual canvas
        RPC_ClearCanvasOnClients();
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ClearCanvasOnClients()
    {
        // 3. Executes local texture reset on every client machine
        _drawableCS.ResetCanvas();
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    private void RPC_SendStroke(Vector2 startPixel, Vector2 endPixel, Color color, int width)
    {
        // Save stroke to history list
        DrawStroke stroke = new DrawStroke
        {
            StartPixel = startPixel,
            EndPixel = endPixel,
            Color = color,
            Width = width
        };
        StrokeHistory.Add(stroke);

        // Render locally
        _drawableCS.Pen_Colour = color;
        _drawableCS.Pen_Width = width;
        _drawableCS.ColourBetween(startPixel, endPixel, width, color);
        _drawableCS.ApplyMarkedPixelChanges();
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_RequestStrokeHistory(PlayerRef requestingPlayer)
    {
        DrawStroke[] historyArray = StrokeHistory.ToArray();
        int totalStrokes = historyArray.Length;

        if (totalStrokes == 0) return;

        // Send total count first so the client knows when rendering is finished
        RPC_TargetStartHistoryBatch(requestingPlayer, totalStrokes);

        // Batch size: 15 strokes (~420 bytes) stays safely under Fusion's 512-byte RPC limit
        int batchSize = 15;

        for (int i = 0; i < totalStrokes; i += batchSize)
        {
            int currentBatchCount = Mathf.Min(batchSize, totalStrokes - i);
            DrawStroke[] batch = new DrawStroke[currentBatchCount];

            System.Array.Copy(historyArray, i, batch, 0, currentBatchCount);

            // Send each chunk individually
            RPC_TargetSendStrokeBatch(requestingPlayer, batch);
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_TargetStartHistoryBatch([RpcTarget] PlayerRef targetPlayer, int totalStrokes)
    {
        // Clear canvas before replaying history to prevent overlapping stale data
        _drawableCS.ResetCanvas();
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_TargetSendStrokeBatch([RpcTarget] PlayerRef targetPlayer, DrawStroke[] batch)
    {
        // Replay the incoming batch of strokes on the late-joiner's local texture
        foreach (var stroke in batch)
        {
            _drawableCS.Pen_Colour = stroke.Color;
            _drawableCS.Pen_Width = stroke.Width;
            _drawableCS.ColourBetween(stroke.StartPixel, stroke.EndPixel, stroke.Width, stroke.Color);
        }

        // Apply pixel changes locally after each batch arrives
        _drawableCS.ApplyMarkedPixelChanges();
    }

    #endregion
}