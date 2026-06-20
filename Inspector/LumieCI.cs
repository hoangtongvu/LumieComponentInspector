using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace LumieComponentInspector.Inspector;

internal partial class LumieCI
{
    private static LumieCI _instance;
    const string anchorEditorWindowName = "InspectorWindow";

    private LCIConfigsSO _inspectorConfigs;
    private GameObject _targetGO;

    private VisualElement _inspectorEditorsList;

    private readonly List<Component> _components = new();
    private readonly Dictionary<Component, ComponentInspectorState> _componentInspectorStates = new();
    private readonly Dictionary<Component, InspectorElement> _component2InspectorMap = new();
    private readonly List<Component> _copiedComponents = new();

    // first int: id of the game object
    // second int: id of the component
    private readonly Dictionary<int, Dictionary<int, ComponentInspectorState>> _cachedInspectorStateByGameObject = new();

    [InitializeOnLoadMethod]
    static void Initialize()
    {
        _instance = new();
    }

    private LumieCI()
    {
        _inspectorConfigs = Resources.Load<LCIConfigsSO>(LCIConfigsSO.DefaultAssetPath);
        Selection.selectionChanged += OnSelectionChanged;
        EditorApplication.hierarchyChanged += OnHierarchyChanged;
    }

    private void OnSelectionChanged()
    {
        EditorApplication.delayCall += OnSelectionChanged1;
    }

    private void OnHierarchyChanged()
    {
        if (!_targetGO) return;

        InitComponentList();
        InitInspectorStates();

        _componentToolBar.Refresh();
    }

    private void OnSelectionChanged1()
    {
        EditorApplication.delayCall -= OnSelectionChanged1;

        SaveInspectorStates();

        _targetGO = Selection.activeGameObject;
        if (!_targetGO) return;

        InitComponentList();
        InitInspectorStates();

        _componentToolBar.Refresh();
    }

    private void SaveInspectorStates()
    {
        if (!_targetGO) return;

        var temp = new Dictionary<int, ComponentInspectorState>();
        foreach (var kVPair in _componentInspectorStates)
            temp.Add(kVPair.Key.GetEntityId(), kVPair.Value);

        _cachedInspectorStateByGameObject[_targetGO.GetEntityId()] = temp;
    }

    private void InitInspectorStates()
    {
        _componentInspectorStates.Clear();

        bool canRetrieveSavedEditorDatas = _cachedInspectorStateByGameObject
            .TryGetValue(_targetGO.GetEntityId(), out var savedInspectorStates);

        foreach (var c in _components)
        {
            ComponentInspectorState state;

            if (canRetrieveSavedEditorDatas)
            {
                bool canRetrieveState = savedInspectorStates.TryGetValue(c.GetEntityId(), out var savedState);
                state = canRetrieveState ? savedState : new();
            }
            else
            {
                state = new();
            }

            _componentInspectorStates[c] = state;
            SetSelectedSingleComponent(c, state.IsSelected);
        }
    }

    private void InitComponentList()
    {
        _components.Clear();
        _component2InspectorMap.Clear();

        // TODO: cache these elements
        var unityInspectorWindow = Resources.FindObjectsOfTypeAll<EditorWindow>()
            .FirstOrDefault(w => w.GetType().Name == anchorEditorWindowName);

        var contentContainer = unityInspectorWindow.rootVisualElement.Q<VisualElement>(name: "unity-content-container");
        _inspectorEditorsList = contentContainer.Q<VisualElement>(className: "unity-inspector-editors-list");

        var gameObjInspector = _inspectorEditorsList.Q<VisualElement>(className: "game-object-inspector");
        var gameObjInspectorIndex = _inspectorEditorsList.IndexOf(gameObjInspector);

        var editorField = typeof(InspectorElement).GetField("m_Editor", BindingFlags.Instance | BindingFlags.NonPublic);
        var inspectors = _inspectorEditorsList.Query<InspectorElement>().ToList();

        foreach (var inspectorElement in inspectors)
        {
            if (inspectorElement.parent.ClassListContains("game-object-inspector")) continue;

            var editor = (Editor)editorField.GetValue(inspectorElement);
            var editorTarget = editor.target;

            if (editorTarget is not Component component) continue;

            _components.Add(component);
            _component2InspectorMap.Add(component, inspectorElement);
        }

        // TODO: Move this logic else where
        if (_toolbarsHolder == null)
        {
            _toolbarsHolder = new(this);

            _actionToolbar = new(this);
            _toolbarsHolder.Add(_actionToolbar);

            _componentToolBar = new(this);
            _toolbarsHolder.Add(_componentToolBar);
        }

        _inspectorEditorsList.Insert(gameObjInspectorIndex + 1, _toolbarsHolder);
    }

    private void ToggleSelectedSingleComponent(Component c)
    {
        bool isSelected = !_componentInspectorStates[c].IsSelected;

        _componentInspectorStates[c].IsSelected = isSelected;
        _component2InspectorMap[c].parent.style.display = isSelected ? DisplayStyle.Flex : DisplayStyle.None;
    }

    private void SetSelectedSingleComponent(Component c, bool value)
    {
        _componentInspectorStates[c].IsSelected = value;
        _component2InspectorMap[c].parent.style.display = value ? DisplayStyle.Flex : DisplayStyle.None;
    }

    private void SetAllSelected(bool value)
    {
        foreach (var c in _components)
        {
            _componentInspectorStates[c].IsSelected = value;
            _component2InspectorMap[c].parent.style.display = value ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}