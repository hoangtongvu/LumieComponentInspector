using UnityEngine.UIElements;

namespace LumieComponentInspector.Inspector;

partial class LumieCI
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