using UnityEngine;


[CreateAssetMenu(fileName = "New Boolean Variable", menuName = "Variable/Bool")]
public class SOBoolean : SOVariable<bool>
{
    public override void ResetValue()
    {
        Value = false;
    }
}
