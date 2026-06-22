using System;
using UnityEngine.UIElements;

namespace LumieComponentInspector.Inspector;

partial class LumieCI
{
    private ToolbarsHolder _toolbarsHolder;

    private void TryInitToolbarsHolder()
    {
        if (_toolbarsHolder == null)
        {
            _toolbarsHolder = new(this);

            _actionToolbar = new(this);
            _toolbarsHolder.Add(_actionToolbar);

            _componentToolbar = new(this);
            _toolbarsHolder.Add(_componentToolbar);
        }
    }

    private class ToolbarsHolder : VisualElement, IDisposable
    {
        private readonly LumieCI _lci;

        public ToolbarsHolder(LumieCI lci) : base()
        {
            _lci = lci;
        }

        public void Dispose()
        {
            this.RemoveFromHierarchy();
        }
    }
}