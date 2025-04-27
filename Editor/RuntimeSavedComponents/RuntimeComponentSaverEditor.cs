#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Component), true)]
public class RuntimeComponentSaverEditor : Editor
{
    private void OnEnable()
    {
        hideFlags = HideFlags.NotEditable;
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
    }

    [MenuItem("CONTEXT/Component/💾 Save Runtime Changes")]
    private static void SaveRuntimeChanges(MenuCommand command)
    {
        Debug.Log("Saving runtime changes...");
        if (!Application.isPlaying)
        {
            Debug.LogWarning("This only works in Play Mode.");
            return;
        }

        var component = command.context as Component;
        if (component == null)
        {
            Debug.LogError("Context is not a Component.");
            return;
        }

        RuntimeSavedComponentsManager.SaveComponent(component);
    }
}
#endif
