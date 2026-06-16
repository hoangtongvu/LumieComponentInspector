using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace LumieComponentInspector;

internal partial class LumieCI : EditorWindow
{
    private LCIConfigsSO _inspectorConfigs;
    private Object _targetObject;
    private GameObject _targetGO;

    private Dictionary<Component, ComponentInspectorState> _componentInspectorStates = new();
    private List<Component> _copiedComponents = new();

    // first int: id of the game object
    // second int: id of the component
    private Dictionary<int, Dictionary<int, ComponentInspectorState>> _cachedInspectorStateByGameObject = new();

    private bool _isLocked = false;

    [MenuItem("Tools/Lumie Component Inspector")]
    public static void Open()
    {
        GetWindow<LumieCI>("LumieComponentInspector");
    }

    private void OnEnable()
    {
        EditorApplication.hierarchyChanged += RepaintWindow;
    }

    private void OnDisable()
    {
        EditorApplication.hierarchyChanged -= RepaintWindow;
    }

    public void CreateGUI()
    {
        _inspectorConfigs = Resources.Load<LCIConfigsSO>(LCIConfigsSO.DefaultAssetPath);

        rootVisualElement.style.opacity = 0;
        rootVisualElement.style.flexDirection = FlexDirection.Column;

        _rootScrollView = new(this);
        rootVisualElement.Add(_rootScrollView);

        _unityHeader = new(this);
        _rootScrollView.Add(_unityHeader);

        _toolbarsHolder = new(this);
        _rootScrollView.Add(_toolbarsHolder);

        _actionToolbar = new(this);
        _toolbarsHolder.Add(_actionToolbar);

        _componentToolBar = new(this);
        _toolbarsHolder.Add(_componentToolBar);

        _componentInspectors = new(this);
        _rootScrollView.Add(_componentInspectors);

        _lockInspectorButton = new(this);

        _stickyHeader = new(this, _rootScrollView, _toolbarsHolder, _unityHeader);
        rootVisualElement.Add(_stickyHeader);  // added LAST = paints on top
    }

    private void OnSelectionChange()
    {
        if (_isLocked) return;

        var targetObject = Selection.activeObject;
        rootVisualElement.style.opacity = targetObject ? 1 : 0;

        RefreshSelection();
    }

    private void RefreshSelection()
    {
        // Save states before cleaning up
        if (_targetGO != null)
        {
            var temp = new Dictionary<int, ComponentInspectorState>();
            foreach (var kVPair in _componentInspectorStates)
                temp.Add(kVPair.Key.GetEntityId(), kVPair.Value);

            _cachedInspectorStateByGameObject[_targetGO.GetEntityId()] = temp;
        }

        _targetObject = Selection.activeObject;
        _targetGO = Selection.activeGameObject;
        _componentInspectorStates.Clear();

        if (_targetGO == null)
        {
            RepaintWindow();
            return;
        }

        // Retrieve saved inspector states
        bool canRetrieveSavedEditorDatas = _cachedInspectorStateByGameObject
            .TryGetValue(_targetGO.GetEntityId(), out var savedInspectorStates);

        var components = _targetGO.GetComponents<Component>();

        foreach (var c in components)
        {
            if (c == null) continue;

            if (canRetrieveSavedEditorDatas)
            {
                savedInspectorStates.TryGetValue(c.GetEntityId(), out var v);
                if (v != null)
                {
                    _componentInspectorStates[c] = v;
                }
            }
            else
            {
                _componentInspectorStates[c] = new();
            }
        }

        RepaintWindow();
    }

    private void SetAllVisibility(bool value)
    {
        var components = _targetGO.GetComponents<Component>();

        foreach (var c in components)
        {
            if (c == null) continue;

            _componentInspectorStates[c].IsVisible = value;
        }

        RepaintWindow();
    }

    private void RepaintWindow()
    {
        _actionToolbar.Refresh();
        _componentToolBar.Refresh();
        _componentInspectors.Refresh();
        _lockInspectorButton.AttachButton();
    }
}