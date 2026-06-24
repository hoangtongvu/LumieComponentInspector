using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace LumieComponentInspector.Inspector;

internal static class EditorContextMenuUtil
{
    static MethodInfo _method;

    static EditorContextMenuUtil()
    {
        var editorAssembly = typeof(Editor).Assembly;

        var editorUtilityType = editorAssembly.GetType("UnityEditor.EditorUtility");

        _method = editorUtilityType?.GetMethod(
            "DisplayObjectContextMenu",
            BindingFlags.Static | BindingFlags.NonPublic,
            null,
            new System.Type[]
            {
            typeof(Rect),
            typeof(UnityEngine.Object[]),
            typeof(int)
            },
            null
        );
    }

    public static void Show(Rect rect, UnityEngine.Object target)
    {
        if (_method == null)
        {
            EditorUtility.DisplayPopupMenu(rect, "CONTEXT/Component", new(target));
            return;
        }

        _method.Invoke(null, new object[]
        {
        rect,
        new UnityEngine.Object[] { target },
        0
        });
    }
}