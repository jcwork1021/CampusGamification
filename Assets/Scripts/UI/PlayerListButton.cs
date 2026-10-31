using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Fusion;

public class PlayerListButton : MonoBehaviour
{
    [SerializeField] TMP_Text _playerName;
    [SerializeField] Image _micStatusImg;
    [SerializeField] Image _raiseHandImg;

    //Calls in GameManager.cs when player is adding themselves on the list
    public void SetupContent(string playerName)
    {
        _playerName.text = playerName;
    }

    //Calls in GameManager.cs when updating their mic status on playerlist
    public void SetupMicStatus(Sprite micStatusSprite)
    {
        _micStatusImg.sprite = micStatusSprite;
    }

    public void SetupRaiseHand(bool raisedHand)
    {
        _raiseHandImg.enabled = raisedHand;
    }
}
