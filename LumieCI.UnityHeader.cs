using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace LumieComponentInspector;

partial class LumieCI : EditorWindow
{
    private UnityHeader _unityHeader;

    private class UnityHeader : IMGUIContainer
    {
        private readonly LumieCI _lci;
        private Editor _editor;

        public UnityHeader(LumieCI lci) : base()
        {
            _lci = lci;
            CreateHeader();
        }

        private void CreateHeader()
        {
            this.style.flexShrink = 0;
            this.style.flexGrow = 0;

            this.onGUIHandler += () =>
            {
                var target = _lci._targetObject;
                if (!target) return;

                if (_editor == null || _editor.target != target)
                {
                    if (_editor != null)
                        Object.DestroyImmediate(_editor);

                    _editor = Editor.CreateEditor(target);
                }

                EditorGUI.BeginChangeCheck();

                _editor.DrawHeader();

                if (EditorGUI.EndChangeCheck())
                {
                    EditorUtility.SetDirty(target);
                    this.MarkDirtyRepaint();
                }
            };
        }
    }
}