using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace LumieComponentInspector;

partial class LumieCI : EditorWindow
{
    private Header _header;

    private class Header : Toolbar
    {
        private readonly LumieCI _lci;
        private int _headerHeight = 25;

        private Label _toast;
        private IVisualElementScheduledItem _hideToastTask;

        public Header(LumieCI lci) : base()
        {
            _lci = lci;
            CreateHeader();
        }

        private void CreateHeader()
        {
            this.styleSheets.Add(_lci._inspectorConfigs.ComponentHeaderStyleSheet);
            this.AddToClassList("header");

            // Copy component values button
            this.Add(this.CreateCopyComponentValuesButton());

            // Paste component values button
            this.Add(this.CreatePasteComponentValuesButton());

            // Paste values or Add components button
            this.Add(this.CreatePasteValuesOrAddComponentsButton());

            // Create GameObj from copied components button
            this.Add(this.CreateCreateGameObjFromSelectedComponentsButton());

            // Create Toast
            this.Add(CreateToast());
        }

        private Label CreateToast()
        {
            _toast = new();
            _toast.AddToClassList("toast");

            return _toast;
        }

        private void ShowToast(string text)
        {
            _toast.text = text;
            _toast.AddToClassList("show");

            _hideToastTask?.Pause();

            _hideToastTask = _toast.schedule.Execute(() =>
            {
                _toast.RemoveFromClassList("show");
            });

            _hideToastTask.StartingIn(1500);
        }

        private ToolbarButton CreateCopyComponentValuesButton()
        {
            var btn = new ToolbarButton();
            btn.tooltip = "Copy selected Component values";
            var icon = EditorGUIUtility.IconContent("TreeEditor.Duplicate");
            btn.iconImage = icon.image as Texture2D;
            btn.style.width = _headerHeight;

            btn.clicked += () =>
            {
                var inspectorStates = _lci._componentInspectorStates;
                var copiedComponents = _lci._copiedComponents;

                copiedComponents.Clear();

                foreach (var kvPair in inspectorStates)
                {
                    if (!kvPair.Value.IsVisible) continue;

                    copiedComponents.Add(kvPair.Key);
                }

                this.ShowToast($"Copied {copiedComponents.Count} components");
            };

            return btn;
        }

        private ToolbarButton CreatePasteComponentValuesButton()
        {
            var btn = new ToolbarButton();
            btn.tooltip = "Paste selected Component values";
            var icon = EditorGUIUtility.IconContent("d_UnityEditor.ConsoleWindow");
            btn.iconImage = icon.image as Texture2D;
            btn.style.width = _headerHeight;

            btn.clicked += () =>
            {
                var inspectorStates = _lci._componentInspectorStates;
                var copiedComponents = _lci._copiedComponents;

                int pastedCount = 0;

                foreach (var copiedComponent in copiedComponents)
                {
                    foreach (var kvPair in inspectorStates)
                    {
                        var destComponent = kvPair.Key;
                        if (copiedComponent.GetType() != destComponent.GetType()) continue;

                        EditorUtility.CopySerializedIfDifferent(copiedComponent, destComponent);
                        EditorUtility.SetDirty(destComponent);
                        pastedCount++;
                        break;
                    }
                }

                this.ShowToast($"Pasted {pastedCount} components");
            };

            return btn;
        }

        private ToolbarButton CreatePasteValuesOrAddComponentsButton()
        {
            var btn = new ToolbarButton();
            btn.tooltip = "Paste values or Add components";
            var icon = EditorGUIUtility.IconContent("d_Toolbar Plus");
            btn.iconImage = icon.image as Texture2D;
            btn.style.width = _headerHeight;

            btn.clicked += () =>
            {
                var inspectorStates = _lci._componentInspectorStates;
                var copiedComponents = _lci._copiedComponents;

                int pastedCount = 0;
                int createdCount = 0;

                foreach (var copiedComponent in copiedComponents)
                {
                    bool hasDestComponent = false;

                    foreach (var kvPair in inspectorStates)
                    {
                        var destComponent = kvPair.Key;
                        if (copiedComponent.GetType() != destComponent.GetType()) continue;

                        EditorUtility.CopySerializedIfDifferent(copiedComponent, destComponent);
                        EditorUtility.SetDirty(destComponent);
                        pastedCount++;
                        hasDestComponent = true;
                        break;
                    }

                    if (!hasDestComponent)
                    {
                        var destComponent = _lci._targetGO.AddComponent(copiedComponent.GetType());
                        EditorUtility.CopySerialized(copiedComponent, destComponent);
                        EditorUtility.SetDirty(destComponent);
                        createdCount++;
                    }
                }

                this.ShowToast($"Pasted {pastedCount}, Added {createdCount} components");
            };

            return btn;
        }

        private ToolbarButton CreateCreateGameObjFromSelectedComponentsButton()
        {
            var btn = new ToolbarButton();
            btn.tooltip = "Create GameObject from selected components";
            var icon = EditorGUIUtility.IconContent("GameObject Icon");
            btn.iconImage = icon.image as Texture2D;
            btn.style.width = _headerHeight;

            btn.clicked += () =>
            {
                var inspectorStates = _lci._componentInspectorStates;
                var newGO = new GameObject();
                int count = 0;

                foreach (var kvPair in inspectorStates)
                {
                    if (!kvPair.Value.IsVisible) continue;

                    var sourceComponent = kvPair.Key;
                    if (!newGO.TryGetComponent(sourceComponent.GetType(), out var destComponent))
                        destComponent = newGO.AddComponent(sourceComponent.GetType());

                    EditorUtility.CopySerialized(sourceComponent, destComponent);
                    count++;
                }

                EditorUtility.SetDirty(newGO);
                this.ShowToast($"Created a GameObject from {count} components");
            };

            return btn;
        }
    }
}