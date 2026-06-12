using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace LumieComponentInspector;

partial class LumieCI : EditorWindow
{
    private ComponentToolbar _componentToolBar;

    private class ComponentToolbar : VisualElement
    {
        private class ToolBarButton : Button
        {
            public ToolBarButton() : base()
            {
                this.AddToClassList("toolbar-button");
            }
        }

        private readonly LumieCI _lci;
        private bool _toggledAllVisible = true;

        public ComponentToolbar(LumieCI lci) : base()
        {
            _lci = lci;
            CreateComponentToolbar();
        }

        private void CreateComponentToolbar()
        {
            this.styleSheets.Add(_lci._inspectorConfigs.ComponentToolbarStyleSheet);
            this.AddToClassList("container");
        }

        public void Refresh()
        {
            this.Clear();

            var target = _lci._targetGO;
            if (!target) return;

            this.Add(CreateToggleAllButton());

            var components = target.GetComponents<Component>();
            var inspectorStates = _lci._componentInspectorStates;

            foreach (var c in components)
            {
                if (c == null) continue;

                bool visible;

                if (inspectorStates.TryGetValue(c, out var state))
                    visible = state.IsVisible;
                else
                    visible = true;

                var button = CreateSingleComponentButton(c, visible);

                if (visible)
                {
                    button.AddToClassList("button-toggle-on");
                    button.RemoveFromClassList("button-toggle-off");
                }
                else
                {
                    button.AddToClassList("button-toggle-off");
                    button.RemoveFromClassList("button-toggle-on");
                }

                this.Add(button);
            }

            this.Add(CreateAddComponentButton());
        }

        private ToolBarButton CreateToggleAllButton()
        {
            var button = new ToolBarButton();

            var iconSize = new Length(15, LengthUnit.Pixel);
            UpdateToggleAllButtonUI();

            button.clicked += () =>
            {
                _toggledAllVisible = !_toggledAllVisible;
                _lci.SetAllVisibility(_toggledAllVisible);
                UpdateToggleAllButtonUI();
            };

            void UpdateToggleAllButtonUI()
            {
                // Update icon
                var iconName = _toggledAllVisible ? "d_scenevis_visible_hover" : "d_scenevis_hidden";
                button.style.backgroundImage = UnityEditor.EditorGUIUtility.IconContent(iconName).image as Texture2D;
                button.style.backgroundSize = new BackgroundSize(iconSize, iconSize);

                // Update color
                if (_toggledAllVisible)
                {
                    button.AddToClassList("button-toggle-on");
                    button.RemoveFromClassList("button-toggle-off");
                }
                else
                {
                    button.AddToClassList("button-toggle-off");
                    button.RemoveFromClassList("button-toggle-on");
                }
            }

            return button;
        }

        private ToolBarButton CreateSingleComponentButton(Component c, bool visible)
        {
            var button = new ToolBarButton();

            var content = new VisualElement();
            content.AddToClassList("content");

            // Icon
            var icon = new Image();
            icon.image = AssetPreview.GetMiniThumbnail(c);

            // Label
            var label = new Label(c.GetType().Name);

            // Assemble
            content.Add(icon);
            content.Add(label);
            button.Add(content);

            button.clicked += () =>
            {
                _lci._componentInspectorStates[c].IsVisible = !visible;
                _lci.RepaintWindow();
            };

            return button;
        }

        private ToolBarButton CreateAddComponentButton()
        {
            var button = new ToolBarButton();
            button.style.width = button.style.height;

            button.clicked += () =>
            {
                // Use 230 width so the popup renders at the correct size,
                // regardless of the button's actual visual width
                var screenRect = new Rect(Event.current.mousePosition, new(230, 0));

                var addComponentWindow = System.Type.GetType(
                    "UnityEditor.AddComponent.AddComponentWindow, UnityEditor");

                if (addComponentWindow != null)
                {
                    var method = addComponentWindow.GetMethod(
                        "Show",
                        BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);

                    method?.Invoke(null, new object[] { screenRect, new[] { _lci._targetGO } });
                }
            };

            var icon = EditorGUIUtility.IconContent("d_CreateAddNew").image as Texture2D;
            button.style.backgroundImage = icon;
            button.style.backgroundSize = new BackgroundSize(
                new Length(12, LengthUnit.Pixel),
                new Length(12, LengthUnit.Pixel)
            );

            return button;
        }
    }
}