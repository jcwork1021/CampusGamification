using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LoginManager : MonoBehaviour
{
    [Header("Necessary SO")]
    [SerializeField] RememberMeRefSO _rememberMeSO;
    [SerializeField] ChatManagerRefSO _chatManagerSO;

    [Header("Reference from Login_Canvas")]
    [SerializeField] TMP_InputField _nameInput;

    public void ClickLogin()
    {
        if (string.IsNullOrWhiteSpace(_nameInput.text))
        {
            Debug.LogWarning("Please Enter a valid name");
            return;
        }

        _rememberMeSO.Instance.playerName = _nameInput.text;
        _rememberMeSO.Instance.EnableLoading("Loading Lobby Please Wait...");

        //Load the Lobby scene
        SceneManager.LoadSceneAsync("Lobby");
    }
}
