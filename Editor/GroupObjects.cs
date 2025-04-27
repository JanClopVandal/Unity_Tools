using UnityEditor;
using UnityEngine;

public class GroupObjects
{
    [MenuItem("GameObject/Group Selected %g", false, 0)] // Ctrl+G / Cmd+G
    static void GroupSelected()
    {
        var selected = Selection.transforms;
        if (selected.Length == 0) return;

        // Создаем новый пустой объект
        GameObject group = new GameObject("Group");
        Undo.RegisterCreatedObjectUndo(group, "Create Group");

        // Определяем, у всех ли один родитель
        Transform commonParent = selected[0].parent;
        bool sameParent = true;
        foreach (var t in selected)
        {
            if (t.parent != commonParent)
            {
                sameParent = false;
                break;
            }
        }

        // Ставим группу в нужное место в иерархии
        if (sameParent)
        {
            group.transform.SetParent(commonParent, false);
        }

        // Ставим группу на среднюю позицию выделенных объектов
        Vector3 center = Vector3.zero;
        foreach (var t in selected)
            center += t.position;
        center /= selected.Length;
        group.transform.position = center;

        // Переносим выделенные объекты в группу с сохранением мирового положения
        foreach (var t in selected)
        {
            Undo.SetTransformParent(t, group.transform, "Group Selected");
        }

        Selection.activeGameObject = group;
    }
}
