using Fusion;
using System.Collections;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using UnityEngine.Splines;

public class PlayerManager : NetworkBehaviour
{
    [Header("Necessary SO")]
    [SerializeField] RememberMeRefSO _rememberMeSO;
    [SerializeField] GameManagerRefSO _gameManagerSO;
    [SerializeField] ChatManagerRefSO _chatManagerRefSO;
    [SerializeField] PlayerManagerRefSO _playerManagerSO;

    [Header("Reference from Itself")]
    [SerializeField] CharacterController _characterController;
    [SerializeField] PlayerController _playerController;
    [SerializeField] PlayerInput _playerInput;
    [SerializeField] PlayerInteractor _playerInteractor;
    [SerializeField] AvatarApplier _avatarApplier;
    public NetworkTransform _networkTransform;

    [Header("Reference from its Child")]
    [SerializeField] Transform _headTarget;

    [Header("Reference from Bubble_Canvas")]
    [SerializeField] Canvas _bubbleCanvas;
    [SerializeField] TMP_Text _nameTxt;
    [SerializeField] CanvasGroup _nameTxtCanvasGroup;
    [SerializeField] GameObject _messageHolder;
    [SerializeField] TMP_Text _messageTxt;

    [Header("From Project")]
    [SerializeField] GameObject _shadowQuadPrefab;

    [Header("Player Components")]
    public Animator animator;

    [Header("Read-Only")]
    [SerializeField] GameObject _spawnedBlobShadow;

    [Networked, OnChangedRender(nameof(OnPlayerNameChanged))]
    public NetworkString<_16> PlayerName { get; set; }

    [Networked, OnChangedRender(nameof(OnPlayerMicStatusChanged))]
    public bool networkedMicMuted { get; set; }

    [Networked, OnChangedRender(nameof(OnPlayerRaiseHandChanged))]
    public bool networkedRaisedHand { get; set; } //this is changed in InteractableChair.cs

    #region Unity Function

    private void OnDestroy()
    {
        if (HasStateAuthority)
        {
            _playerManagerSO.Instance = null;
        }
    }

    #endregion

    #region Fusion Callbacks
    public override void Spawned()
    {
        // Only run owner setup logic for the LOCAL player
        if (HasStateAuthority)
        {
            _playerManagerSO.Instance = this; // Reference the PlayerManager here

            // Set the networked property value (as the owner)
            PlayerName = _rememberMeSO.Instance.playerName;

            // Enable Player need from GameManager.cs reference
            if (_gameManagerSO != null)
            {
                _gameManagerSO.Instance.EnableWhatPlayerNeed(_headTarget);
            }
            else
            {
                Debug.LogWarning("Player has no reference to GameManager.");
                return;
            }

            // Enable Player Components
            _playerInput.enabled = true;
            _playerController.enabled = true;
            _playerInteractor.enabled = true;
            _characterController.enabled = true;

            // Enable Player component for LocalPlayer Only
            StartCoroutine(DelayDisableLoadingPanel());
        }

        // Generate Fake Shadow for Player (Runs for all instances)
        if (_shadowQuadPrefab != null)
        {
            _spawnedBlobShadow = Instantiate(_shadowQuadPrefab);

            var tracker = _spawnedBlobShadow.GetComponent<BlobShadowTracker>();
            if (tracker != null)
            {
                tracker.TargetPlayer = transform;
            }
        }

        // Updates 3D overhead text AND creates/updates the UI list button for ALL players
        OnPlayerNameChanged();

        OnPlayerMicStatusChanged();

        OnPlayerRaiseHandChanged();
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (_spawnedBlobShadow != null)
        {
            Destroy(_spawnedBlobShadow);
        }

        // Remove the player's button from UI when they disconnect/despawn
        if (_gameManagerSO != null && _gameManagerSO.Instance != null)
        {
            _gameManagerSO.Instance.RemovedPlayerButton(Object.InputAuthority);
        }
    }
    #endregion

    #region Network Functions

    // Called automatically when another player's PlayerName finishes syncing to you
    private void OnPlayerNameChanged()
    {
        string formattedName = PlayerName.ToString();

        // 1. Update 3D overhead text tag & hierarchy name
        if (_nameTxt != null)
        {
            _nameTxt.text = formattedName;
            this.gameObject.name = formattedName;
        }

        // 2. Spawn / Update UI Button via GameManager
        if (_gameManagerSO != null && _gameManagerSO.Instance != null)
        {
            // Object.InputAuthority is the PlayerRef (Network ID of the player controlling this object)
            _gameManagerSO.Instance.CreatePlayerButton(Object.InputAuthority, formattedName);
        }
    }

    public void OnPlayerMicStatusChanged()
    {
        _gameManagerSO.Instance.UpdateMicStatusNetworked(Object.InputAuthority, networkedMicMuted);
    }

    public void OnPlayerRaiseHandChanged()
    {
        _gameManagerSO.Instance.UpdateRaiseHandNetworked(Object.InputAuthority, networkedRaisedHand);

        if (!Object.HasStateAuthority)
        {
            return;
        }

        if (networkedRaisedHand)
        {
            _gameManagerSO.Instance.ToggleRaiseHand(true);
        }
        else
        {
            _gameManagerSO.Instance.ToggleRaiseHand(false);
        }

        _playerManagerSO.Instance.animator.SetBool("RaiseHand", networkedRaisedHand);
    }

    #endregion

    #region Functions
    IEnumerator DelayDisableLoadingPanel()
    {
        yield return new WaitForSeconds(2);

        // Disable RememberMeCanvas or the loading panel
        if (_rememberMeSO != null && _rememberMeSO.Instance != null)
        {
            _rememberMeSO.Instance.ToggleRememberMeCanvas(false);
        }
    }
    #endregion

    #region Toggles

    public void ToggleNameText(bool visible)
    {
        TogglePanel(_nameTxtCanvasGroup, visible);
    }

    private void TogglePanel(CanvasGroup cg, bool visible)
    {
        // 0 = no display, 1 = fully visible
        cg.alpha = visible ? 1f : 0f;
        // Prevents interacting with buttons on this panel when false
        cg.interactable = visible;
        // Stops this panel from blocking clicks to elements/gameplay behind it when false
        cg.blocksRaycasts = visible;
    }
    #endregion

    #region Calls by other scripts
    public void EnableMovement(bool yeah)
    {
        _characterController.enabled = yeah;
        _playerController.enabled = yeah;

        if (yeah)
        {
            _playerController.EnableMovement();
        }
        else
        {
            _playerController.DisableMovement();
        }
    }

    public void EnablePlayerBubbleCanvas(bool yeah)
    {
        if (_bubbleCanvas != null)
        {
            _bubbleCanvas.gameObject.SetActive(yeah);
        }
    }

    public void RepositionPlayer(Transform newPosition)
    {
        if (!HasStateAuthority)
            return;

        _networkTransform.Teleport(newPosition.position, newPosition.rotation);
    }

    #endregion
}