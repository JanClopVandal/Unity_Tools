#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Reflection;

[InitializeOnLoad]
public static class RuntimeSavedComponentsManager
{
    private static readonly HashSet<string> IgnoredMemberNames = new HashSet<string>
    {
        "hideFlags",
        "useGUILayout",
        "runInEditMode",
        "enabled"
    };

    private class SavedComponentData
    {
        public string path;
        public System.Type componentType;
        public Dictionary<string, object> fieldValues = new Dictionary<string, object>();
    }

    private static List<SavedComponentData> savedComponents = new List<SavedComponentData>();

    private static bool waitingToApply = false;

    static RuntimeSavedComponentsManager()
    {
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    public static void SaveComponent(Component component)
    {
        var data = new SavedComponentData
        {
            path = GetGameObjectPath(component.gameObject),
            componentType = component.GetType()
        };

        var type = component.GetType();

        var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        foreach (var field in fields)
        {
            if (IgnoredMemberNames.Contains(field.Name))
                continue;

            if (field.IsPublic || field.GetCustomAttribute<SerializeField>() != null)
            {
                try
                {
                    var value = field.GetValue(component);
                    data.fieldValues["field:" + field.Name] = value;
                }
                catch
                {
                    Debug.LogWarning($"[RuntimeSaver] Failed to save field: {field.Name}");
                }
            }
        }

        var properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        foreach (var prop in properties)
        {
            if (IgnoredMemberNames.Contains(prop.Name))
                continue;

            if (prop.CanRead && prop.CanWrite && prop.GetIndexParameters().Length == 0)
            {
                try
                {
                    var value = prop.GetValue(component);
                    data.fieldValues["property:" + prop.Name] = value;
                }
                catch
                {
                    Debug.LogWarning($"[RuntimeSaver] Failed to save property: {prop.Name}");
                }
            }
        }

        savedComponents.Add(data);
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingPlayMode)
        {
            waitingToApply = true;
            EditorApplication.update += WaitUntilSceneReadyAndApply;
        }
    }

    private static void WaitUntilSceneReadyAndApply()
    {
        // Ждём полного выхода из Play Mode
        if (!EditorApplication.isPlaying && !EditorApplication.isPlayingOrWillChangePlaymode)
        {
            ApplySavedData();
            EditorApplication.update -= WaitUntilSceneReadyAndApply;
            waitingToApply = false;
        }
    }

    private static void ApplySavedData()
    {
        foreach (var data in savedComponents)
        {
            GameObject obj = GameObject.Find(data.path);
            if (obj == null)
            {
                Debug.LogWarning($"[RuntimeSaver] GameObject {data.path} not found, skipping.");
                continue;
            }

            var component = obj.GetComponent(data.componentType);
            if (component == null)
            {
                Debug.LogWarning($"[RuntimeSaver] Component {data.componentType.Name} not found on {data.path}, skipping.");
                continue;
            }

            Undo.RecordObject(component, $"Apply Saved Runtime Changes: {data.componentType.Name}");

            foreach (var kvp in data.fieldValues)
            {
                string[] parts = kvp.Key.Split(':');
                if (parts.Length != 2) continue;

                string type = parts[0];
                string name = parts[1];

                if (IgnoredMemberNames.Contains(name))
                    continue;

                if (type == "field")
                {
                    var field = component.GetType().GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (field != null)
                    {
                        try
                        {
                            field.SetValue(component, kvp.Value);
                        }
                        catch
                        {
                            Debug.LogWarning($"[RuntimeSaver] Failed to apply field {name} on {data.path}");
                        }
                    }
                }
                else if (type == "property")
                {
                    var prop = component.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (prop != null && prop.CanWrite)
                    {
                        try
                        {
                            prop.SetValue(component, kvp.Value);
                        }
                        catch
                        {
                            Debug.LogWarning($"[RuntimeSaver] Failed to apply property {name} on {data.path}");
                        }
                    }
                }
            }

            EditorUtility.SetDirty(component);
        }

        savedComponents.Clear();
    }

    private static string GetGameObjectPath(GameObject obj)
    {
        string path = obj.name;
        while (obj.transform.parent != null)
        {
            obj = obj.transform.parent.gameObject;
            path = obj.name + "/" + path;
        }
        return path;
    }
}
#endif
