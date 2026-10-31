using UnityEngine;

namespace App.Levels
{
    public class LevelManager : MonoBehaviour
    {
        [Header("Necessary SO")]
        [SerializeField] RememberMeRefSO _rememberMeSO;
        [SerializeField] GameManagerRefSO _gameManagerSO;

        [SerializeField] GameEvent OnLevelLoaded;
        [SerializeField] GameEvent OnLevelUnloaded;

        private void Start()
        {
            if (OnLevelLoaded != null)
                OnLevelLoaded.Raise();

            if(_gameManagerSO.Instance == null)
            {
                //Debug.LogWarning("No GameManager Instance Yet");
                return;
            }

            _gameManagerSO.Instance.SetupPrivateRoom(); //For now all scene will display PlayerList

            /*
            //This is to check if the additive scene we will be loading is public scene or not (if public scene then we should not display player list)
            if (_rememberMeSO.Instance.sceneTransitionSO.autoJoin)
            {
                _gameManagerSO.Instance.SetUpPublicRoom();
            }
            else
            {
                _gameManagerSO.Instance.SetupPrivateRoom();
            }
            */
        }

        private void OnDestroy()
        {
            if (OnLevelUnloaded != null)
                OnLevelUnloaded.Raise();
        }

        //TODO: add mmore meta data and flow logic for levels here
    }
}
