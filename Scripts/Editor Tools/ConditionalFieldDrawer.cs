#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// ConditionalField drawer:
/// - Robust controller resolution (root → sibling scan).
/// - FAILS CLOSED: if controller isn't found, field is hidden (instead of always shown).
/// - No Update/Apply calls here; the parent inspector owns that lifecycle.
/// </summary>
[CustomPropertyDrawer(typeof(ConditionalFieldAttribute))]
public class ConditionalFieldDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        var cond = (ConditionalFieldAttribute)attribute;

        var controller = ResolveController(property, cond.controllingField);

        // FAIL CLOSED: if we can't find the controller, hide (prevents "always visible")
        bool shouldShow = controller != null && EvaluateCondition(controller, cond);

        using (new EditorGUI.PropertyScope(position, label, property))
        {
            if (shouldShow)
            {
                EditorGUI.PropertyField(position, property, label, true);
            }
            else if (cond.Mode == ConditionalFieldAttribute.DrawMode.Disable)
            {
                using (new EditorGUI.DisabledScope(true))
                    EditorGUI.PropertyField(position, property, label, true);
            }
            // HideCompletely: draw nothing
        }
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        var cond = (ConditionalFieldAttribute)attribute;

        var controller = ResolveController(property, cond.controllingField);

        bool shouldShow = controller != null && EvaluateCondition(controller, cond);

        if (shouldShow || cond.Mode == ConditionalFieldAttribute.DrawMode.Disable)
            return EditorGUI.GetPropertyHeight(property, label, true);

        // Hidden completely
        return 0f;
    }

    // ---------- Controller resolution ----------

    private static SerializedProperty ResolveController(SerializedProperty property, string controllerName)
    {
        if (property == null || property.serializedObject == null || string.IsNullOrEmpty(controllerName))
            return null;

        var so = property.serializedObject;

        // 0) Try root first (most reliable for top-level fields on SOs)
        var root = so.FindProperty(controllerName);
        if (root != null)
            return root;

        // 1) If property has a parent path, try sibling resolution
        string path = property.propertyPath;
        int lastDot = path.LastIndexOf('.');
        if (lastDot >= 0)
        {
            string parentPath = path.Substring(0, lastDot);
            var parent = so.FindProperty(parentPath);

            // direct sibling
            if (parent != null)
            {
                var sibling = parent.FindPropertyRelative(controllerName);
                if (sibling != null)
                    return sibling;

                // scan immediate children at same depth
                var iter = parent.Copy();
                var end = iter.GetEndProperty();
                int targetDepth = parent.depth + 1;
                bool enterChildren = true;

                while (iter.NextVisible(enterChildren) && !SerializedProperty.EqualContents(iter, end))
                {
                    enterChildren = false;

                    if (iter.depth <= parent.depth)
                        break;

                    if (iter.depth == targetDepth && iter.name == controllerName)
                        return iter.Copy();
                }
            }
        }

        // Not found
        return null;
    }

    // ---------- Condition evaluation ----------

    private static bool EvaluateCondition(SerializedProperty controller, ConditionalFieldAttribute cond)
    {
        // If editing multiple objects and values differ, default to "show" (avoid hiding unpredictably)
        if (controller.hasMultipleDifferentValues)
            return true;

        switch (controller.propertyType)
        {
            case SerializedPropertyType.Boolean:
                if (!cond.showWhenBoolIs.HasValue) return true;
                return controller.boolValue == cond.showWhenBoolIs.Value;

            case SerializedPropertyType.Enum:
            {
                if (cond.showForEnumValues == null || cond.showForEnumValues.Length == 0)
                    return true;

                int idx = controller.enumValueIndex; // Enum index in declaration order
                for (int i = 0; i < cond.showForEnumValues.Length; i++)
                    if (idx == cond.showForEnumValues[i]) return true;
                return false;
            }

            case SerializedPropertyType.Integer:
            {
                // Fallback if Unity reports enum as int
                if (cond.showForEnumValues == null || cond.showForEnumValues.Length == 0)
                    return true;

                int val = controller.intValue;
                for (int i = 0; i < cond.showForEnumValues.Length; i++)
                    if (val == cond.showForEnumValues[i]) return true;
                return false;
            }

            default:
                // Unknown controller type → safest is to hide (fail closed)
                return false;
        }
    }
}
#endif
