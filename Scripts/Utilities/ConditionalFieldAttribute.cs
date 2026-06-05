using UnityEngine;

[System.AttributeUsage(System.AttributeTargets.Field, AllowMultiple = true)]
public class ConditionalFieldAttribute : PropertyAttribute
{
    public enum DrawMode { HideCompletely, Disable }

    public string controllingField;
    public int[] showForEnumValues;   // for enum controllers
    public bool? showWhenBoolIs;      // for bool controllers
    public DrawMode Mode { get; private set; }

    // Enum-based constructor
    public ConditionalFieldAttribute(string controllingField, params int[] showForEnumValues)
    {
        this.controllingField = controllingField;
        this.showForEnumValues = showForEnumValues;
        this.showWhenBoolIs = null;
        this.Mode = DrawMode.HideCompletely;
    }

    // Bool-based constructor
    public ConditionalFieldAttribute(string controllingField, bool showWhenBoolIs, DrawMode mode = DrawMode.HideCompletely)
    {
        this.controllingField = controllingField;
        this.showForEnumValues = null;
        this.showWhenBoolIs = showWhenBoolIs;
        this.Mode = mode;
    }
}
