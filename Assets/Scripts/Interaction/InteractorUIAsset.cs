using UnityEngine;

public abstract class InteractorUIAsset : ScriptableObject
{
    [SerializeField] protected SOInteractor _interactor;
    public abstract void InitializeUIAsset();
}
