using System;
using UnityEngine;

[AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
public class LogRangeAttribute : PropertyAttribute
{
    public readonly float min;
    public readonly float max;

    public LogRangeAttribute() : this(-5.0f, 5.0f) {}

    public LogRangeAttribute(float min, float max)
    {
        this.min = min;
        this.max = max;
    }
}
