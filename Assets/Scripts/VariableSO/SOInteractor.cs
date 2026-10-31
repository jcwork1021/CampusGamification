using UnityEngine;

[CreateAssetMenu(menuName = "SO/Variable/SOInteractor")]
public class SOInteractor : SOVariable<PlayerInteractor>
{
    public override void ResetValue()
    {
        Value = null;
    }
}
