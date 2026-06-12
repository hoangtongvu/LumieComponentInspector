using UnityEditor;
using UnityEngine;

namespace LumieComponentInspector;

partial class LumieCI : EditorWindow
{
    private LockInspectorButton _lockInspectorButton;

    private class LockInspectorButton : EditorWindowTitleButton
    {
        public LockInspectorButton(LumieCI lci) : base(lci)
        {
        }

        protected override void DrawButton(Rect rect)
        {
            var icon = EditorGUIUtility.IconContent(
                _lci._isLocked ? "LockIcon-On" : "LockIcon"
            );

            if (GUI.Button(rect, GUIContent.none, EditorStyles.iconButton))
            {
                bool wasLocked = _lci._isLocked;
                _lci._isLocked = !_lci._isLocked;

                if (wasLocked && !_lci._isLocked)
                    _lci.RefreshSelection();
            }

            const float iconSize = 11f;

            var iconRect = new Rect(
                rect.x + (rect.width - iconSize) * 0.5f,
                rect.y + (rect.height - iconSize) * 0.5f,
                iconSize,
                iconSize
            );

            GUI.DrawTexture(iconRect, (Texture2D)icon.image, ScaleMode.ScaleToFit);
        }
    }
}