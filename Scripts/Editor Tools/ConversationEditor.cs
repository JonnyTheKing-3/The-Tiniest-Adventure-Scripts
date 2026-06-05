#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Conversation))]
public class ConversationEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // Fields
        var endProp             = serializedObject.FindProperty("ConversationEnd");
        var sentencesProp       = serializedObject.FindProperty("sentences");
        var choicesProp         = serializedObject.FindProperty("choiceResults");
        var customEndActionProp = serializedObject.FindProperty("customEndAction");

        // Draw enum first
        EditorGUILayout.PropertyField(endProp);

        // Draw sentences (your ConditionalField drawer will still work inside)
        EditorGUILayout.PropertyField(sentencesProp, true);

        // Only draw choices when ConversationEnd == Choice
        if (endProp.enumValueIndex == (int)Conversation.ConversationEndType.Choice)
        {
            EditorGUILayout.PropertyField(choicesProp, true);
        }

        // Only draw custom action when ConversationEnd == Custom
        if (endProp.enumValueIndex == (int)Conversation.ConversationEndType.Custom)
        {
            EditorGUILayout.PropertyField(customEndActionProp);
        }

        serializedObject.ApplyModifiedProperties();
    }
}
#endif
