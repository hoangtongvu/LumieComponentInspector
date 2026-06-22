using System;
using UnityEditor;
using UnityEngine.UIElements;

namespace LumieComponentInspector.Inspector;

partial class LumieCI
{
    private StickyHeader _stickyHeader;

    private class StickyHeader : VisualElement, IDisposable
    {
        private readonly LumieCI _lci;

        private ScrollView _scrollView;
        private VisualElement _stickyTarget;
        private VisualElement _stickyTargetOriginalParent;
        private VisualElement _elementAboveTarget;

        private VisualElement _spacer;
        private int _stickyOriginalIndex;
        private float _stickyTriggerOffset;  // Y position where sticky kicks in

        private bool _isPinned;

        public StickyHeader(LumieCI lci) : base()
        {
            _lci = lci;

            this.style.position = Position.Absolute;
            this.style.top = 0;
            this.style.left = 0;
            this.style.right = 0;
            this.pickingMode = PickingMode.Ignore;

            _isPinned = false;
        }

        public void Initialize(ScrollView scrollView)
        {
            _scrollView = scrollView;
            _scrollView.contentViewport.RegisterCallback<GeometryChangedEvent>(OnScrollViewResized);
        }

        public void Dispose()
        {
            this.UnBind();
            this.RemoveFromHierarchy();
        }

        public void Bind(
            VisualElement stickyTarget,
            VisualElement elementAboveTarget)
        {
            _isPinned = false;
            _stickyTarget = stickyTarget;
            _stickyTargetOriginalParent = _stickyTarget.parent;
            _stickyOriginalIndex = _stickyTargetOriginalParent.IndexOf(_stickyTarget);
            _elementAboveTarget = elementAboveTarget;

            EditorApplication.delayCall += OnLayoutReady;
            _elementAboveTarget.RegisterCallback<GeometryChangedEvent>(OnElementAboveTargetResized);
        }

        public void UnBind()
        {
            _scrollView.verticalScroller.valueChanged -= OnScroll;
        }

        private void OnScrollViewResized(GeometryChangedEvent evt)
        {
            this.style.width = evt.newRect.width;
        }

        private void OnLayoutReady()
        {
            EditorApplication.delayCall -= OnLayoutReady;

            _scrollView.verticalScroller.valueChanged += OnScroll;

            _stickyTriggerOffset = _elementAboveTarget.resolvedStyle.height;

            // Create a spacer matching the sticky element's height
            _spacer ??= new() { name = "LCI.Spacer" };
            _spacer.style.height = _stickyTarget.resolvedStyle.height;
            _spacer.style.display = DisplayStyle.None;

            _stickyTargetOriginalParent.Insert(_stickyOriginalIndex, _spacer);
        }

        private void OnElementAboveTargetResized(GeometryChangedEvent evt)
        {
            if (evt.newRect.height <= 0) return;

            _stickyTriggerOffset = evt.newRect.height;
        }

        private void OnScroll(float scrollY)
        {
            if (!_isPinned && scrollY >= _stickyTriggerOffset)
                PinStickyElement(scrollY);

            if (_isPinned && scrollY < _stickyTriggerOffset)
                UnpinStickyElement();
        }

        private void PinStickyElement(float scrollY)
        {
            _isPinned = true;
            this.Add(_stickyTarget);
            _spacer.style.display = DisplayStyle.Flex;
        }

        private void UnpinStickyElement()
        {
            _isPinned = false;
            _stickyTargetOriginalParent.Insert(_stickyOriginalIndex, _stickyTarget);
            _spacer.style.display = DisplayStyle.None;
        }
    }
}