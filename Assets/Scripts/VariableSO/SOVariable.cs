using System;
using UnityEngine;

public abstract class SOVariable<T> : ScriptableObject
{
    [SerializeField] private T value;

    public Action<T> OnValueChanged;

#if UNITY_EDITOR
    [SerializeField] private string _variableDescription;
#endif
    public T Value
    {
        get => value;
        set
        {
            this.value = value;
            OnValueChanged?.Invoke(value);
        }
    }


    public abstract void ResetValue();
}

[Serializable]
public class AnimReference<T>
{
    public string nameValue;
    public T animValue;
}