using UnityEngine;

[CreateAssetMenu(menuName = "SO/SceneTransition")]
public class SceneTransitionSO : ScriptableObject
{
    [Header("Scene Data")]
    public string sceneName;
    public string receiverName;
    public bool autoJoin;
}
