using Fusion;
using Paroxe.PdfRenderer;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class InteractablePDF : BaseInteractable
{
    [Header("Necessary SO")]
    [SerializeField] InteractablePDFRefSO _interactablePDFSO;
    [SerializeField] InteractableChairRefSO _interactableChairSO;

    [Header("Reference from Itself")]
    [SerializeField] PDFViewer _pdfViewerCS;

    [Header("Reference from PDF Canvas")]
    [SerializeField] Canvas _pdfCanvasOverlay;
    [SerializeField] GameObject _enterURLPanel;
    [SerializeField] Scrollbar _verticalScrollbar;

    [Header("Reference from EnterURLPanel")]
    [SerializeField] TMP_InputField _enterUrlInput;

    [Header("Reference from Its Child")]
    [SerializeField] Transform _standPosition;

    [Networked, OnChangedRender(nameof(OnPDFURLChanged))]
    public NetworkString<_512> networkedPDFURL { get; set; }

    /*
    [Networked, OnChangedRender(nameof(OnFloatValueChanged))]
    public float netWorkedZoomValue{ get; set; }
    */

    public float verticalScrollbarValue;

    #region Unity Functions
    private void Awake()
    {
        _pdfCanvasOverlay.gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        _interactablePDFSO.Instance = this;
        //_pdfViewerCS.OnZoomValueChanged += HandleZoomValueChanged;
    }

    private void OnDisable()
    {
        _interactablePDFSO.Instance = null;
        //_pdfViewerCS.OnZoomValueChanged -= HandleZoomValueChanged;

        if (currentlyInUsed)
        {
            if (Object.HasStateAuthority)
            {
                ClickExit();
            }
            else
            {
                ClickExit(); //inside this filter if we are the operator or not
            }
        }
    }
    #endregion

    #region GameEvent Overrides


    //This is called when Interactable is collided with player
    public override void CallWhenInteracted()
    {
        base.CallWhenInteracted();
    }
    protected override void OnLevelRequirementPassed()
    {
        base.OnLevelRequirementPassed();

        //Focus the Interactable Camera on this one, we cannot add this one Abstract Class because not all interactable need focus immediately like chairs
        FocusInteractableCamera(10);

        _gameManagerSO.Instance.TogglePlayerCanvas(false); //Disable Player Canvas
        StartCoroutine(DelayLevelPassed());
    }

    IEnumerator DelayLevelPassed()
    {
        yield return new WaitForSeconds(1.5f);
        _pdfCanvasOverlay.gameObject.SetActive(true);
        _playerManagerSO.Instance.RepositionPlayer(_standPosition); //Reposition Player it includes the rotation already                                                                  
        //Camera.main.cullingMask = ~LayerMask.GetMask("Player");// Render EVERYTHING EXCEPT the "Player" layer
    }

    protected override void OnLevelRequirementFailed()
    {
        base.OnLevelRequirementFailed();

        Debug.Log("Displaying 'Level Too Low' notification UI to player.");
    }

    public override void ViewerInteraction()
    {
        base.ViewerInteraction();

        _pdfCanvasOverlay.gameObject.SetActive(true); //Enable the Canvas again
    }

    public override void ExitInteractable()
    {
        base.ExitInteractable();

        _gameManagerSO.Instance.TogglePlayerCanvas(true); //Enable Player Canvas
        _pdfCanvasOverlay.gameObject.SetActive(false);

        //Camera.main.cullingMask = -1; //Include the rendering on player
    }
    #endregion

    #region Button Clicks
    public void ClickUploadPDF()
    {
        if (_playerOperator != null)
        {
            ToggleEnterURLPanel(true);
        }
        else
        {
            _rememberMeSO.Instance.EnableWarning("Only the player operating the PDF, can upload a file");
        }
    }

    public void ClickLoadPDF()
    {
        networkedPDFURL = _enterUrlInput.text; //When the networkedPDFURL value changed the Network Function will be  triggered
        ToggleEnterURLPanel(false);
    }

    public void ClickExit()
    {
        if (_playerOperator == null) //If we are not the operator then most likely we are just a viewer
        {
            if (_interactableChairSO != null) //We Mark the Chair Interactable 2nd interaction disable, cause even tho this is Whiteboard the Viewer can only access this using in InteractableChair.cs
            {
                _interactableChairSO.Instance.ExitInteractable2(); //This enable the rest of options

                FocusInteractableCamera(0);
            }
            else
            {
                Debug.LogError("There is no Chair Reference");
            }

            _pdfCanvasOverlay.gameObject.SetActive(false);
        }
        else
        {
            ExitInteractable();
            Debug.LogError("PDF");
        }
    }
   
    #endregion

    #region OnValueChanged
    //Calls on VerticalScrollBar OnValueChanged()
    public void ChangedVerticalScrollBar()
    {
        verticalScrollbarValue = _verticalScrollbar.value;
    }

    /*
    private void HandleZoomValueChanged(float newZoomValue) //Subscribe to PDFViewer.cs eveytime the m_ZoomFactorIsChanged
    {
        netWorkedZoomValue = newZoomValue;
    }
    */

    #endregion

    #region Functions
    void ToggleEnterURLPanel(bool on)
    {
        _enterURLPanel.SetActive(on);
    }

    #endregion

    #region Networked Functions
    private void OnPDFURLChanged()
    {
        _pdfViewerCS.LoadDocumentFromWeb(networkedPDFURL.ToString(), 0);
    }

    /*
    private void OnFloatValueChanged()
    {
        if (Object.HasInputAuthority || Object.HasStateAuthority)
        {
            Debug.LogWarning("You are the owner no need to sync this for yourself");
        }
        else
        {
            _pdfViewerCS.ZoomTo(netWorkedZoomValue);
        }

        // Logic to execute on the local client when FloatValue changes
    }
    */
    #endregion
}
