using UnityEngine;

[CreateAssetMenu(fileName = "New Integer Variable", menuName = "Variable/Integer")]
public class SOInteger : SOVariable<int>
{
    public override void ResetValue()
    {
        Value = 0;
    }
}
