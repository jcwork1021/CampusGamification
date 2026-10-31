using Fusion;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;
using UnityEngine.Video;

public class InteractableVideoPlayer : BaseInteractable
{
    [Header("Necessary SO")]
    [SerializeField] InteractableVideoPlayerRefSO _interactableVideoPlayerSO;
    [SerializeField] InteractableChairRefSO _interactableChairSO;

    [Header("Reference from itself")]
    [SerializeField] Transform _standPosition;

    [Header("Reference from VideoPlayer_Canvas")]
    [SerializeField] Canvas _videoPlayerCanvas;
    [SerializeField] Canvas _viewerCanvas;
    [SerializeField] GameObject _loadingPanel;
    [SerializeField] GameObject _controlsPanel;
    [SerializeField] GameObject _uploadPanel;
    [SerializeField] GameObject _blockadePanel;

    [Header("Reference from Controls_Panel")]
    [SerializeField] Button _playBtn;
    [SerializeField] Slider _durationSlider;
    [SerializeField] TMP_Text _durationTxt;
    [SerializeField] TMP_Text _elapsedTimeTxt;
    [SerializeField] Slider _volumeSlider;

    [Header("Reference from Upload_Panel")]
    [SerializeField] TMP_InputField _urlInputField;

    [Header("Video Components")]
    [SerializeField] VideoPlayer _videoPlayer;

    [Header("Reference from Project")]
    [SerializeField] Sprite _playSprite;
    [SerializeField] Sprite _pauseSprite;
    [SerializeField] RenderTexture _videoRenderTexture;
    

    [Header("Read-Only")]
    [SerializeField] bool isDraggingSlider = false;
    private bool _isSeeking = false; // Block Update() while async seek processes
    [SerializeField] private ushort trackIndex = 0; // Usually track 0 for standard

    [Networked, OnChangedRender(nameof(OnVideoURLChanged))]
    public NetworkString<_512> networkedVideoURL { get; set; }

    // Replace networkedVideoElapsedTime with these two variables:
    [Networked, OnChangedRender(nameof(OnVideoStateChanged))]
    public double networkedVideoStartTime { get; set; }

    [Networked, OnChangedRender(nameof(OnVideoStateChanged))]
    public NetworkBool networkedIsPlaying { get; set; }

    #region Unity Functions

    private void Awake()
    {
        _loadingPanel.SetActive(false);
        _uploadPanel.SetActive(false);
        _blockadePanel.SetActive(true);
        _viewerCanvas.gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        _interactableVideoPlayerSO.Instance = this;
    }

    private void OnDisable()
    {
        _videoPlayer.Stop();
        ClearVideoScreen(Color.white); // Clears screen to white after stopping

        if (currentlyInUsed)
        {
            if (Object.HasStateAuthority)
            {
                networkedVideoURL = null;
                ClickExit();
            }
            else
            {
                ClickViewerExit();
            }
        }
    }

    private void Start()
    {
        _videoPlayer.prepareCompleted += OnVideoPrepared;
        _videoPlayer.errorReceived += OnVideoError;
        _videoPlayer.seekCompleted += OnVideoSeekCompleted; // Subscription for async seek tracking


        //setup video volume
        _volumeSlider.minValue = 0f;
        _volumeSlider.maxValue = 1f;
        _volumeSlider.value = 0.5f;


        _volumeSlider.onValueChanged.AddListener(SetVolume);

        ToggleMuteVideo(true); //Mute the video player at start

    }

    private void OnDestroy()
    {
        _videoPlayer.prepareCompleted -= OnVideoPrepared;
        _videoPlayer.errorReceived -= OnVideoError;
        _videoPlayer.seekCompleted -= OnVideoSeekCompleted;
    }

    private void Update()
    {
        // Update slider position ONLY if playing, not dragging, and not actively waiting for a seek frame
        if (_videoPlayer.isPlaying && !isDraggingSlider && !_isSeeking && _durationSlider != null)
        {
            _durationSlider.value = (float)_videoPlayer.time;
            _elapsedTimeTxt.text = FormatTime(_videoPlayer.time);
        }
    }
    #endregion

    #region GameEvent Overrides

    public override void CallWhenInteracted()
    {
        base.CallWhenInteracted();
    }

    protected override void OnLevelRequirementPassed()
    {
        base.OnLevelRequirementPassed();
        FocusInteractableCamera(10);

        _blockadePanel.SetActive(false);
        _controlsPanel.SetActive(true);
        _gameManagerSO.Instance.TogglePlayerCanvas(false);

        _playerManagerSO.Instance.RepositionPlayer(_standPosition);
        //Camera.main.cullingMask = ~LayerMask.GetMask("Player");

        //Unmute the video since we are the operator
        ToggleMuteVideo(false);
    }

    protected override void OnLevelRequirementFailed()
    {
        base.OnLevelRequirementFailed();
        Debug.Log("Displaying 'Level Too Low' notification UI to player.");
    }

    public override void ViewerInteraction()
    {
        base.ViewerInteraction();
        _viewerCanvas.gameObject.SetActive(true);
        _blockadePanel.SetActive(true);

        //Unmute the video player, since a player is viewing
        ToggleMuteVideo(false);
    }

    public override void ExitInteractable()
    {
        base.ExitInteractable();
        _gameManagerSO.Instance.TogglePlayerCanvas(true);
        _blockadePanel.SetActive(true);
        ToggleMuteVideo(true); //Mute since we exited the interaction
        //Camera.main.cullingMask = -1;
    }
    #endregion

    #region Functions

    private string FormatTime(double timeInSeconds)
    {
        int minutes = (int)(timeInSeconds / 60);
        int seconds = (int)(timeInSeconds % 60);
        return string.Format("{0:00}:{1:00}", minutes, seconds);
    }

    private void ToggleMuteVideo(bool yeah)
    {
        _videoPlayer.SetDirectAudioMute(trackIndex, yeah);
    }

    public void ClearVideoScreen(Color clearColor)
    {
        if (_videoRenderTexture == null) return;

        // Store active RT to avoid disrupting active camera pass
        RenderTexture activeBuffer = RenderTexture.active;

        RenderTexture.active = _videoRenderTexture;

        // Clears depth & color buffers to your designated color (e.g., Color.black or Color.clear)
        GL.Clear(true, true, clearColor);

        // Restore previous active RT
        RenderTexture.active = activeBuffer;
    }
    #endregion

    #region Fusion Callbacks

    public override void Spawned()
    {
        OnVideoURLChanged();
    }
    #endregion

    #region Fusion Function

    private void OnVideoURLChanged()
    {
        if (string.IsNullOrEmpty(networkedVideoURL.ToString()))
        {
            return;
        }

        _uploadPanel.SetActive(false);
        _loadingPanel.SetActive(true);
        _videoPlayer.Stop();
        _videoPlayer.url = networkedVideoURL.ToString();
        _videoPlayer.Prepare();
    }

    #endregion

    #region UI Clicks

    public void ClickPlayBtn()
    {
        if (!HasStateAuthority) return; // Only authority controls state

        if (_videoPlayer.isPlaying)
        {
            // Pause: Save the current time offset as StartTime while paused
            networkedVideoStartTime = _videoPlayer.time;
            networkedIsPlaying = false;
        }
        else if (_videoPlayer.isPrepared)
        {
            // Play/Resume: Save the calculated start timestamp
            networkedVideoStartTime = Runner.SimulationTime - _videoPlayer.time;
            networkedIsPlaying = true;
        }
    }

    public void ClickLoadVideoFromURL()
    {
        string inputUrl = _urlInputField.text.Trim();
        if (string.IsNullOrEmpty(inputUrl)) return;

        if (HasStateAuthority)
        {
            _uploadPanel.SetActive(false);
            networkedVideoURL = inputUrl;
            networkedIsPlaying = true;

            // Reset start time temporarily so late-joiners during loading don't get negative times
            networkedVideoStartTime = 0;
        }
    }

    public void ClickOpenUploadPanel() => _uploadPanel.SetActive(true);
    public void ClickExitUploadPanel() => _uploadPanel.SetActive(false);

    public void ClickExit()
    {
        ExitInteractable();
    }

    public void ClickViewerExit()
    {
        if (_interactableChairSO != null)
        {
            _viewerCanvas.gameObject.SetActive(false);
            _interactableChairSO.Instance.ExitInteractable2();
        }
        else
        {
            Debug.LogError("There is no Chair Reference");
        }

        ToggleMuteVideo(true); //Mute since the viewer exited
        FocusInteractableCamera(0);
    }

    #endregion

    #region Video Callbacks

    private void OnVideoPrepared(VideoPlayer source)
    {
        _loadingPanel.SetActive(false);

        if (_durationSlider != null)
        {
            _durationSlider.minValue = 0;
            _durationSlider.maxValue = (float)source.length;
            _durationSlider.value = 0;
        }

        _durationTxt.text = FormatTime(source.length);

        if (HasStateAuthority)
        {
            // If start time was reset to 0, stamp the current simulation time as 0:00 playback point
            if (networkedVideoStartTime == 0)
            {
                networkedVideoStartTime = Runner.SimulationTime;
            }
        }

        // Sync state once prepared
        SyncVideoWithNetworkState();
    }

    private void SyncVideoWithNetworkState()
    {
        if (!_videoPlayer.isPrepared) return;

        if (networkedIsPlaying)
        {
            // Calculate exact current playback position
            double currentPlaybackTime = Runner.SimulationTime - networkedVideoStartTime;

            // Clamp to avoid seeking past video duration
            if (currentPlaybackTime < 0) currentPlaybackTime = 0;
            if (currentPlaybackTime > _videoPlayer.length) currentPlaybackTime = _videoPlayer.length;

            _videoPlayer.time = currentPlaybackTime;
            _videoPlayer.Play();
            if (_playBtn != null && _playSprite != null) _playBtn.GetComponent<Image>().sprite = _playSprite;
        }
        else
        {
            _videoPlayer.time = networkedVideoStartTime; // When paused, StartTime holds the paused offset
            _videoPlayer.Pause();
            if (_playBtn != null && _pauseSprite != null) _playBtn.GetComponent<Image>().sprite = _pauseSprite;
        }
    }

    private void OnVideoStateChanged()
    {
        // Render callback triggered when play, pause, or seek changes on the network
        SyncVideoWithNetworkState();
    }

    private void OnVideoError(VideoPlayer source, string message)
    {
        _rememberMeSO.Instance.EnableWarning($"Video Player Error: {message}");
        Debug.LogError($"Video Player Error: {message}");
    }

    private void OnVideoSeekCompleted(VideoPlayer source)
    {
        _isSeeking = false; // Re-enable Update() once seek completes
    }
    #endregion

    #region Slider Control Methods

    public void OnSliderBeginDrag()
    {
        isDraggingSlider = true;
        if (_videoPlayer.isPlaying)
        {
            _videoPlayer.Pause();
        }
    }

    public void OnSliderValueDragging(float value)
    {
        if (_elapsedTimeTxt != null)
        {
            _elapsedTimeTxt.text = FormatTime(value);
        }
    }

    public void OnSliderEndDrag(float seekTime)
    {
        if (_videoPlayer.isPrepared && HasStateAuthority)
        {
            _isSeeking = true;

            if (networkedIsPlaying)
            {
                // Recalculate Start Time based on new seek position
                networkedVideoStartTime = Runner.SimulationTime - seekTime;
            }
            else
            {
                networkedVideoStartTime = seekTime;
            }
        }

        isDraggingSlider = false;
    }

    void SetVolume(float volume)
    {
        _videoPlayer.SetDirectAudioVolume(0, volume);
    }

    #endregion
}