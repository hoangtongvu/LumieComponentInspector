using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace LumieComponentInspector.Inspector;

internal partial class LumieCI : IDisposable
{
    private static LumieCI _instance;

    const string MenuPath = "Tools/LumieCI";
    const string PrefKey = "LumieCI.Enabled";

    const string anchorEditorWindowName = "InspectorWindow";

    private bool _initialized = false; // Used to prevent OnSelectionChanged or OnHierarchyChanged being called before initialization
    private LCIConfigsSO _inspectorConfigs;
    private GameObject _targetGO;

    private ScrollView _rootScrollView;
    private VisualElement _inspectorEditorsList;
    private VisualElement _gameObjInspectorHeader;

    private readonly List<Component> _components = new();
    private readonly Component2InspectorMap _component2InspectorMap = new();
    private readonly Dictionary<Component, ComponentInspectorState> _componentInspectorStates = new();
    private readonly List<Component> _copiedComponents = new();

    // first int: id of the game object
    // second int: id of the component
    private readonly Dictionary<int, Dictionary<int, ComponentInspectorState>> _cachedInspectorStateByGameObject = new();

    [MenuItem(MenuPath)]
    static void Toggle()
    {
        bool enabled = !EditorPrefs.GetBool(PrefKey, false);
        EditorPrefs.SetBool(PrefKey, enabled);

        Menu.SetChecked(MenuPath, enabled);

        if (enabled)
        {
            if (_instance == null)
                CreateLumieInstance();
        }
        else
        {
            _instance?.Dispose();
        }
    }

    [MenuItem(MenuPath, true)]
    static bool ValidateToggle()
    {
        Menu.SetChecked(
            MenuPath,
            EditorPrefs.GetBool(PrefKey, false));

        return true;
    }

    [InitializeOnLoadMethod]
    static void InitializeOnLoad()
    {
        bool enabled = EditorPrefs.GetBool(PrefKey, false);
        if (!enabled) return;

        CreateLumieInstance();
    }

    static void CreateLumieInstance()
    {
        _instance = new();
    }

    private LumieCI()
    {
        _inspectorConfigs = Resources.Load<LCIConfigsSO>(LCIConfigsSO.DefaultAssetPath);
        Selection.selectionChanged += OnSelectionChanged;
        EditorApplication.hierarchyChanged += OnHierarchyChanged;

        EditorApplication.delayCall += () =>
        {
            _initialized = true;
            var unityInspectorWindow = Resources.FindObjectsOfTypeAll<EditorWindow>()
            .FirstOrDefault(w => w.GetType().Name == anchorEditorWindowName);

            _rootScrollView = unityInspectorWindow.rootVisualElement.Q<ScrollView>(className: "unity-inspector-root-scrollview");
            var contentContainer = unityInspectorWindow.rootVisualElement.Q<VisualElement>(name: "unity-content-container");
            _inspectorEditorsList = contentContainer.Q<VisualElement>(className: "unity-inspector-editors-list");

            _stickyHeader = new(this);
            _stickyHeader.Initialize(_rootScrollView);
            //_rootScrollView.parent.Add(_stickyHeader);

            _componentInspectors = new(this);
        };
    }

    public void Dispose()
    {
        _instance = null;

        _component2InspectorMap.Dispose();

        _toolbarsHolder?.Dispose();
        _stickyHeader?.Dispose();

        Selection.selectionChanged -= OnSelectionChanged;
        EditorApplication.hierarchyChanged -= OnHierarchyChanged;
    }

    private void OnSelectionChanged()
    {
        if (!_initialized) return;

        _stickyHeader?.UnBind();

        SaveInspectorStates();

        _targetGO = Selection.activeGameObject;
        if (!_targetGO)
        {
            _stickyHeader.RemoveFromHierarchy();
            return;
        }

        InitComponentList();
        InitInspectorStates();

        InjectUIElements();
        _toolbarsHolder.Disable();
        _componentToolbar?.Refresh();
        _component2InspectorMap.TriggerUpdate(_inspectorEditorsList, _components.Count);
        _component2InspectorMap.OnFinishedUpdating += SetAllSelectedByStates;
        _component2InspectorMap.OnFinishedUpdating += _toolbarsHolder.Enable;
        _component2InspectorMap.OnFinishedUpdating += _componentInspectors.Refresh;
    }

    private void OnHierarchyChanged()
    {
        if (!_initialized) return;

        _stickyHeader?.UnBind();

        if (!_targetGO) return;

        InitComponentList();
        InitInspectorStates();

        InjectUIElements();
        _toolbarsHolder.Disable();
        _componentToolbar?.Refresh();
        _component2InspectorMap.TriggerUpdate(_inspectorEditorsList, _components.Count);
        _component2InspectorMap.OnFinishedUpdating += SetAllSelectedByStates;
        _component2InspectorMap.OnFinishedUpdating += _toolbarsHolder.Enable;
        _component2InspectorMap.OnFinishedUpdating += _componentInspectors.Refresh;
    }

    private void SaveInspectorStates()
    {
        if (!_targetGO) return;

        var temp = new Dictionary<int, ComponentInspectorState>();
        foreach (var kVPair in _componentInspectorStates)
            temp.Add(kVPair.Key.GetEntityId(), kVPair.Value);

        _cachedInspectorStateByGameObject[_targetGO.GetEntityId()] = temp;
    }

    private void InitComponentList()
    {
        _components.Clear();
        _components.AddRange(_targetGO.GetComponents<Component>());
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
        }
    }

    private void InjectUIElements()
    {
        _gameObjInspectorHeader = _inspectorEditorsList.Q<VisualElement>(className: "game-object-inspector");
        var headerIndex = _inspectorEditorsList.IndexOf(_gameObjInspectorHeader);

        TryInitToolbarsHolder();
        _inspectorEditorsList.Insert(headerIndex + 1, _toolbarsHolder);

        _stickyHeader.Bind(_toolbarsHolder, _gameObjInspectorHeader);
        _rootScrollView.parent.Add(_stickyHeader);
    }

    private void SetAllSelectedByStates()
    {
        foreach (var c in _components)
        {
            var state = _componentInspectorStates[c];
            SetSelectedSingleComponent(c, state.IsSelected);
        }
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