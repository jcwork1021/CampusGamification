using UnityEngine;

[CreateAssetMenu(menuName = "SO/Variable/SOString")]
public class SOString : SOVariable<string>
{
    public override void ResetValue()
    {
        Value = "";
    }
}
