using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace LumieComponentInspector.Inspector;

partial class LumieCI
{
    private ComponentToolbar _componentToolbar;

    private class ComponentToolbar : VisualElement
    {
        private class ToolbarButton : Button
        {
            public ToolbarButton() : base()
            {
                this.AddToClassList("toolbar-button");
            }
        }

        private class ComponentButton : ToolbarButton
        {
            private readonly ComponentToolbar _componentToolbar;
            private Component _targetComponent;
            public VisualElement content = new() { name = "ContentContainer" };
            public VisualElement header = new() { name = "Header" };
            public VisualElement body = new() { name = "Body" };
            public VisualElement footer = new() { name = "Footer" };

            public ComponentButton(ComponentToolbar componentToolbar, Component c) : base()
            {
                _componentToolbar = componentToolbar;

                content.AddToClassList("content");
                content.Add(header);
                content.Add(body);


                BindComponent(c);
                Add(content);
                footer.AddToClassList("footer");
                Add(footer);
            }

            private void BindComponent(Component c)
            {
                _targetComponent = c;

                // Icon
                var icon = new Image();
                icon.image = AssetPreview.GetMiniThumbnail(c);
                header.Add(icon);

                // Label
                var label = new Label(c.GetType().Name);
                body.Add(label);

                clicked += () => _componentToolbar.ToggleSelectedSingleComponent(c);

                RegisterCallback<MouseDownEvent>(evt =>
                {
                    if (evt.button != 1) return; // Right mouse button

                    var mouseRect = new Rect(Event.current.mousePosition, Vector2.zero);
                    EditorContextMenuUtil.Show(mouseRect, c);

                    evt.StopPropagation();
                });
            }

            public void ShowDropIndicator()
            {
                footer.AddToClassList("drop-indicator");
            }

            public void HideDropIndicator()
            {
                footer.RemoveFromClassList("drop-indicator");
            }
        }

        private readonly LumieCI _lci;

        private bool _toggledAllVisible = true;
        private Component _draggedComponent;
        private int _dropTargetIndex;
        private ComponentButton _draggedButton;

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

            this.Add(CreateToggleAllButton());

            var components = _lci._components;
            var inspectorStates = _lci._componentInspectorStates;

            foreach (var c in components)
            {
                bool visible = inspectorStates[c].IsSelected;

                var button = CreateSingleComponentButton(c);

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

        private ToolbarButton CreateToggleAllButton()
        {
            var button = new ToolbarButton();

            var iconSize = new Length(15, LengthUnit.Pixel);
            UpdateToggleAllButtonUI();

            button.clicked += () =>
            {
                _toggledAllVisible = !_toggledAllVisible;
                _lci.SetAllSelected(_toggledAllVisible);
                UpdateToggleAllButtonUI();
                Refresh();
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

        private ToolbarButton CreateSingleComponentButton(Component c)
        {
            var button = new ComponentButton(this, c);

            // Handle move component events
            button.RegisterCallback<MouseMoveEvent>(evt =>
            {
                if (evt.pressedButtons != 1) return;
                if (_draggedComponent != null) return;

                BeginDrag(c, button);
                evt.StopPropagation();
            });

            button.RegisterCallback<MouseEnterEvent>(evt =>
            {
                if (_draggedComponent == null) return;

                int hoverIdx = _lci._components.IndexOf(c);
                if (hoverIdx < 0) return;

                _dropTargetIndex = hoverIdx + 1;
                button.ShowDropIndicator();
            });

            button.RegisterCallback<MouseLeaveEvent>(evt =>
            {
                button.HideDropIndicator();
            });

            button.RegisterCallback<MouseUpEvent>(evt =>
            {
                if (_draggedComponent == null) return;

                CommitDrag();
                evt.StopPropagation();
            });

            return button;
        }

        // TODO: Cache the Reflection types
        private ToolbarButton CreateAddComponentButton()
        {
            var button = new ToolbarButton();
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

        private void ToggleSelectedSingleComponent(Component c)
        {
            _lci.ToggleSelectedSingleComponent(c);
            Refresh();
        }

        private void BeginDrag(Component c, ComponentButton button)
        {
            _dropTargetIndex = _lci._components.IndexOf(c);
            _draggedComponent = c;
            _draggedButton = button;
            button.AddToClassList("button-dragging");

            // Global mouse-up ends the drag regardless of where the pointer lands.
            this.RegisterCallback<MouseLeaveEvent>(OnMouseLeave);
        }

        private void OnMouseLeave(MouseLeaveEvent evt) => CancelDrag();

        private void CommitDrag()
        {
            if (_draggedComponent != null && _dropTargetIndex >= 0)
            {
                int from = _lci._components.IndexOf(_draggedComponent);
                if (from >= 0 && from != _dropTargetIndex)
                    MoveComponent(_draggedComponent, from, _dropTargetIndex);
            }

            EndDrag();
        }

        private void CancelDrag() => EndDrag();

        private void EndDrag()
        {
            _draggedButton?.RemoveFromClassList("button-dragging");
            _draggedButton?.HideDropIndicator();

            _draggedComponent = null;
            _draggedButton = null;
            _dropTargetIndex = -1;

            this.UnregisterCallback<MouseLeaveEvent>(OnMouseLeave);
        }

        private void MoveComponent(Component component, int fromIndex, int toIndex)
        {
            Undo.RecordObject(_lci._targetGO, $"Move {component.GetType().Name}");

            if (toIndex < fromIndex)
            {
                // Move up (toward index 0) one step at a time.
                for (int i = fromIndex; i > toIndex; i--)
                    UnityEditorInternal.ComponentUtility.MoveComponentUp(component);
            }
            else
            {
                // Move down one step at a time.
                for (int i = fromIndex; i < toIndex; i++)
                    UnityEditorInternal.ComponentUtility.MoveComponentDown(component);
            }

            _lci.InitComponentList();
            _lci.InitInspectorStates();
            Refresh();
        }
    }
}