using UnityEditor;
using UnityEngine.UIElements;

namespace LumieComponentInspector;

partial class LumieCI : EditorWindow
{
    private ToolbarsHolder _toolbarsHolder;

    private class ToolbarsHolder : VisualElement
    {
        private readonly LumieCI _lci;

        public ToolbarsHolder(LumieCI lci) : base()
        {
            _lci = lci;
        }
    }
}