using Fusion;
using UnityEngine;
using System.Threading.Tasks; // Make sure this is at the top of your file

[CreateAssetMenu(menuName = "SO/Passed/PassedSO")]
public class PassedSO : ScriptableObject
{
    [Header("Necessary SO")]
    [SerializeField] AdditiveLevelLoaderRefSO _additiveLevelLoader;

    [Header("Optional")]
    [ScenePath] string sceneToLoad;
    public Animator doorAnimator;

    public bool loadNetworkScene;
    public bool openDoor;


    public void DoWhenPassed()
    {
        if (loadNetworkScene)
        {
            _=LoadNetworkScene();
            return;
        }

        if (openDoor)
        {

        }
    }



    private async Task LoadNetworkScene()
    {
        await _additiveLevelLoader.Instance.LoadStageAsync(sceneToLoad);
    }

    public void OpenDoorAnimation()
    {

    }

}
