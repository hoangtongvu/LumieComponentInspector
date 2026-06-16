using UnityEditor;
using UnityEngine.UIElements;

namespace LumieComponentInspector;

partial class LumieCI : EditorWindow
{
    private RootScrollView _rootScrollView;

    private class RootScrollView : ScrollView
    {
        private readonly LumieCI _lci;

        public RootScrollView(LumieCI lci) : base()
        {
            _lci = lci;
            this.verticalScrollerVisibility = ScrollerVisibility.AlwaysVisible;

            this.styleSheets.Add(_lci._inspectorConfigs.ComponentInspectorsStyleSheet);
        }
    }
}