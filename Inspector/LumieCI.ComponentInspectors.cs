using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace LumieComponentInspector.Inspector;

partial class LumieCI
{
    private ComponentInspectors _componentInspectors;

    private class ComponentInspectors
    {
        private readonly LumieCI _lci;
        private static Type editorElementType;
        private static FieldInfo componentHeaderField;

        public ComponentInspectors(LumieCI lci) : base()
        {
            _lci = lci;

            _lci._rootScrollView.styleSheets.Add(_lci._inspectorConfigs.ComponentInspectorsStyleSheet);

            editorElementType ??= typeof(Editor).Assembly
                .GetType("UnityEditor.UIElements.EditorElement");
            componentHeaderField ??= editorElementType
                .GetField("m_Header", BindingFlags.Instance | BindingFlags.NonPublic);
        }

        public void Refresh()
        {
            const string lciHeaderAddedTag = "editor-element-contains-lci-header";
            var component2InspectorMap = _lci._component2InspectorMap;
            var components = _lci._components;

            foreach (var c in components)
            {
                var editorElement = component2InspectorMap[c].parent;

                if (editorElement.ClassListContains(lciHeaderAddedTag))
                    continue;

                editorElement.AddToClassList(lciHeaderAddedTag);
                var nativeComponentHeader = (VisualElement)componentHeaderField.GetValue(editorElement);
                int headerOriginalIndex = editorElement.IndexOf(nativeComponentHeader);

                var lciHeader = new VisualElement();
                lciHeader.name = "LCI_Header";
                lciHeader.AddToClassList("lci-header");

                nativeComponentHeader.AddToClassList("native-component-header");

                var subComponentHeader = new VisualElement();
                subComponentHeader.AddToClassList("sub-component-header");
                subComponentHeader.Add(CreateHideButton(c));

                lciHeader.Add(nativeComponentHeader);
                lciHeader.Add(subComponentHeader);
                editorElement.Insert(headerOriginalIndex, lciHeader);
            }
        }

        private Button CreateHideButton(Component component)
        {
            var hideBtn = new Button();
            hideBtn.text = "👁";
            hideBtn.AddToClassList("util-button");

            hideBtn.clicked += () =>
            {
                _lci.SetSelectedSingleComponent(component, false);
                _lci._componentToolbar.Refresh();
            };

            return hideBtn;
        }
    }
}