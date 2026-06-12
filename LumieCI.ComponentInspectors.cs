using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace LumieComponentInspector;

partial class LumieCI : EditorWindow
{
    private ComponentInspectors _componentInspectors;

    private class ComponentInspectors : ScrollView
    {
        private readonly LumieCI _lci;

        public ComponentInspectors(LumieCI lci) : base()
        {
            _lci = lci;
            this.verticalScrollerVisibility = ScrollerVisibility.AlwaysVisible;

            this.styleSheets.Add(_lci._inspectorConfigs.ComponentInspectorsStyleSheet);
        }

        public void Refresh()
        {
            this.Clear();

            var targetGO = _lci._targetGO;
            if (!targetGO)
            {
                // Draw Inspector for non-GameObject Object
                var targetObject = _lci._targetObject;
                var objectInspector = new InspectorElement(targetObject);
                this.Add(objectInspector);

                return;
            }

            var inspectorStates = _lci._componentInspectorStates;
            var components = targetGO.GetComponents<Component>();

            foreach (var c in components)
            {
                if (inspectorStates.ContainsKey(c) && !inspectorStates[c].IsVisible) continue;

                var element = CreateComponentElement(c);
                this.Add(element);
            }
        }

        private VisualElement CreateComponentElement(Component component)
        {
            var container = new VisualElement();
            container.AddToClassList("component-element");

            // ===== Inspector =====
            if (!_lci._componentInspectorStates.TryGetValue(component, out var componentInspectorState))
            {
                componentInspectorState = new();
                _lci._componentInspectorStates[component] = componentInspectorState;
            }

            var titleBar = new VisualElement();
            titleBar.AddToClassList("title-bar");

            var nativeComponentHeader = new IMGUIContainer(() =>
            {
                EditorGUILayout.InspectorTitlebar(
                    componentInspectorState.IsExpanded,
                    component);
            });

            nativeComponentHeader.AddToClassList("native-component-header");

            var subComponentHeader = new VisualElement();
            subComponentHeader.AddToClassList("sub-component-header");

            subComponentHeader.Add(CreateHideButton(component));

            var inspector = new InspectorElement(component);

            inspector.style.marginTop = 4;
            inspector.style.marginBottom = 4;
            inspector.style.display = componentInspectorState.IsExpanded
                ? DisplayStyle.Flex
                : DisplayStyle.None;

            nativeComponentHeader.RegisterCallback<MouseDownEvent>(evt =>
            {
                if (evt.button != 0) return; // Left mouse button

                componentInspectorState.IsExpanded = !componentInspectorState.IsExpanded;

                inspector.style.display = componentInspectorState.IsExpanded
                    ? DisplayStyle.Flex
                    : DisplayStyle.None;

                evt.StopPropagation();
            });

            titleBar.Add(nativeComponentHeader);
            titleBar.Add(subComponentHeader);
            container.Add(titleBar);
            container.Add(inspector);

            return container;
        }

        private Button CreateHideButton(Component component)
        {
            var hideBtn = new Button();
            hideBtn.text = "👁";
            hideBtn.AddToClassList("util-button");

            hideBtn.clicked += () =>
            {
                _lci._componentInspectorStates[component].IsVisible = false;
                _lci.RepaintWindow();
            };

            return hideBtn;
        }
    }
}