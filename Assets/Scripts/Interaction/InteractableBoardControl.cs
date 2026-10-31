using Fusion;
using UnityEngine;
using TMPro;

public class InteractableBoardControl : BaseInteractable
{
    [Header("Necessary SO")]
    [SerializeField] InteractableBoardControlRefSO _interactableBoardControlSO;

    [Header("Reference from the scene")] // 0 = whiteboard, 1 = pdfViewer, 2 = videoPlayer
    [SerializeField] GameObject[] boards;
    [SerializeField] TMP_Text[] boardsStatusTxt;

    [Header("Reference from Itself")]
    [SerializeField] Canvas _boardControlCanvas;

    [Networked, OnChangedRender(nameof(OnBoardIndexChanged))]
    public int netWorkedBoardIndex { get; set; }


    #region Fusion Callbacks
    public override void Spawned()
    {
        if (Object.HasStateAuthority)
        {
            ClickToggleBoards(0); //if we are the owner of the object upon spawning then lets enable the whiteboard first
        }

        OnBoardIndexChanged();
    }
    #endregion

    #region GameEvent Overrides
    private void OnEnable()
    {
        ToggleBoardControlCanvas(false);
    }

    private void OnDisable()
    {

    }

    //This is called when Interactable is collided with player
    public override void CallWhenInteracted()
    {
        base.CallWhenInteracted();
        _interactableBoardControlSO.Instance = this;
    }
    protected override void OnLevelRequirementPassed()
    {
        base.OnLevelRequirementPassed();
        ToggleBoardControlCanvas(true); //Enable the Canvas
        _gameManagerSO.Instance.TogglePlayerCanvas(false); //Disable player controls or canvas
    }

    protected override void OnLevelRequirementFailed()
    {
        base.OnLevelRequirementFailed();

        Debug.Log("Displaying 'Level Too Low' notification UI to player.");
    }

    public override void ExitInteractable()
    {
        base.ExitInteractable();
        ToggleBoardControlCanvas(false); //Disable the canvas bruh
        _gameManagerSO.Instance.TogglePlayerCanvas(true); //enable player controls
    }

    #endregion

    #region UI Functions

    public void ClickExitBoardControl()
    {
        ExitInteractable();
    }

    private void ToggleBoardControlCanvas(bool yeah)
    {
        _boardControlCanvas.gameObject.SetActive(yeah);
    }

    public void ClickToggleBoards(int boardsIndex) 
    {
        netWorkedBoardIndex = boardsIndex;
    }

    #endregion

    #region Network Functions
    private void OnBoardIndexChanged()
    {
        /*
        foreach (GameObject obj in boards) // 0 = whiteboard, 1 = pdfViewer, 2 = videoPlayer
        {
            obj.SetActive(false);
        }

        foreach(TMP_Text txt in boardsStatusTxt)
        {
            txt.text = "OFF";
        }

        boards[netWorkedBoardIndex].SetActive(true); //Enable the object we needed
        boardsStatusTxt[netWorkedBoardIndex].text = "ON"; //Set Status
        */

        // Safety guard against index out of bounds
        if (netWorkedBoardIndex < 0 || netWorkedBoardIndex >= boards.Length) return;

        for (int i = 0; i < boards.Length; i++)
        {
            bool isActive = (i == netWorkedBoardIndex);
            boards[i].SetActive(isActive);

            if (i < boardsStatusTxt.Length && boardsStatusTxt[i] != null)
            {
                boardsStatusTxt[i].text = isActive ? "ON" : "OFF";
            }
        }
    }

    #endregion
}
