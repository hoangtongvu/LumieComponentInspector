using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace LumieComponentInspector.Inspector;

internal partial class LumieCI : IDisposable
{
    private static LumieCI _instance;

    const string MenuPath = "Tools/LumieCI";
    const string PrefKey = "LumieCI.Enabled";

    const string anchorEditorWindowName = "InspectorWindow";

    private LCIConfigsSO _inspectorConfigs;
    private GameObject _targetGO;

    private ScrollView _rootScrollView;
    private VisualElement _inspectorEditorsList;
    private VisualElement _gameObjInspectorHeader;

    private readonly List<Component> _components = new();
    private readonly Dictionary<Component, InspectorElement> _component2InspectorMap = new();
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
            var unityInspectorWindow = Resources.FindObjectsOfTypeAll<EditorWindow>()
            .FirstOrDefault(w => w.GetType().Name == anchorEditorWindowName);

            _rootScrollView = unityInspectorWindow.rootVisualElement.Q<ScrollView>(className: "unity-inspector-root-scrollview");
            var contentContainer = unityInspectorWindow.rootVisualElement.Q<VisualElement>(name: "unity-content-container");
            _inspectorEditorsList = contentContainer.Q<VisualElement>(className: "unity-inspector-editors-list");

            _stickyHeader = new(this);
            _stickyHeader.Initialize(_rootScrollView);
            //_rootScrollView.parent.Add(_stickyHeader);
        };
    }

    public void Dispose()
    {
        _instance = null;

        _toolbarsHolder?.Dispose();
        _stickyHeader?.Dispose();

        Selection.selectionChanged -= OnSelectionChanged;
        EditorApplication.hierarchyChanged -= OnHierarchyChanged;
    }

    private void OnSelectionChanged()
    {
        _stickyHeader.UnBind();
        if (!Selection.activeGameObject)
        {
            _stickyHeader.RemoveFromHierarchy();
        }

        EditorApplication.delayCall += OnSelectionChanged1;
    }

    private void OnHierarchyChanged()
    {
        if (!_targetGO) return;

        _stickyHeader.UnBind();
        InitComponentList();
        InitInspectorStates();

        _componentToolbar.Refresh();
    }

    private void OnSelectionChanged1()
    {
        EditorApplication.delayCall -= OnSelectionChanged1;

        SaveInspectorStates();

        _targetGO = Selection.activeGameObject;
        if (!_targetGO) return;

        InitComponentList();
        InitInspectorStates();

        _componentToolbar.Refresh();
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
        _component2InspectorMap.Clear();

        var editorField = typeof(InspectorElement).GetField("m_Editor", BindingFlags.Instance | BindingFlags.NonPublic);
        var inspectors = _inspectorEditorsList.Query<InspectorElement>().ToList();

        foreach (var inspectorElement in inspectors)
        {
            bool isHeader = inspectorElement.parent.ClassListContains("game-object-inspector");
            if (isHeader) continue;

            var editor = (Editor)editorField.GetValue(inspectorElement);
            var editorTarget = editor.target;

            if (editorTarget is not Component component) continue;

            _components.Add(component);
            _component2InspectorMap.Add(component, inspectorElement);
        }

        // Move these logic to another function
        _gameObjInspectorHeader = _inspectorEditorsList.Q<VisualElement>(className: "game-object-inspector");
        var headerIndex = _inspectorEditorsList.IndexOf(_gameObjInspectorHeader);

        TryInitToolbarsHolder();
        _inspectorEditorsList.Insert(headerIndex + 1, _toolbarsHolder);

        _stickyHeader.Bind(_toolbarsHolder, _gameObjInspectorHeader);
        _rootScrollView.parent.Add(_stickyHeader);
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