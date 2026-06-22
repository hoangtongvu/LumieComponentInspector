using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace LumieComponentInspector.Inspector;

internal class Component2InspectorMap : IDisposable
{
    private Dictionary<Component, InspectorElement> value;
    public bool IsValid = false;
    public bool IsUpdating = false;
    public event Action OnFinishedUpdating;

    private int _componentCount;
    private VisualElement _inspectorEditorsList;

    public InspectorElement this[Component key]
    {
        get => value[key];
        set => this.value[key] = value;
    }

    public void Dispose()
    {
        TryCancelUpdate();
    }

    public void Clear() => value.Clear();

    public void Add(Component key, InspectorElement value) => this.value.Add(key, value);

    public void TriggerUpdate(VisualElement inspectorEditorsList, int componentCount)
    {
        TryCancelUpdate();

        IsValid = false;
        IsUpdating = true;

        _inspectorEditorsList = inspectorEditorsList;
        _componentCount = componentCount;

        EditorApplication.update += Update;
    }

    private void TryCancelUpdate()
    {
        EditorApplication.update -= Update;
        OnFinishedUpdating = null;
    }

    private void Update()
    {
        var tempMap = new Dictionary<Component, InspectorElement>();
        var editorField = typeof(InspectorElement).GetField("m_Editor", BindingFlags.Instance | BindingFlags.NonPublic);
        var inspectors = _inspectorEditorsList.Query<InspectorElement>().ToList();

        foreach (var inspectorElement in inspectors)
        {
            bool isHeader = inspectorElement.parent.ClassListContains("game-object-inspector");
            if (isHeader) continue;

            var editor = (Editor)editorField.GetValue(inspectorElement);
            var editorTarget = editor.target;

            if (editorTarget is not Component component) continue;

            tempMap.Add(component, inspectorElement);
        }

        if (tempMap.Count != _componentCount) return;

        EditorApplication.update -= Update;

        IsUpdating = false;
        IsValid = true;

        value = tempMap;
        OnFinishedUpdating?.Invoke();
    }
}