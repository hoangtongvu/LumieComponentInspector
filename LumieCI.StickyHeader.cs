using UnityEditor;
using UnityEngine.UIElements;

namespace LumieComponentInspector;

partial class LumieCI : EditorWindow
{
    private StickyHeader _stickyHeader;

    private class StickyHeader : VisualElement
    {
        private readonly LumieCI _lci;

        private readonly ScrollView _scrollView;
        private readonly VisualElement _stickyTarget;
        private readonly VisualElement _elementAboveTarget;

        private VisualElement _spacer;
        private int _stickyOriginalIndex;
        private float _stickyTriggerOffset;  // Y position where sticky kicks in

        public StickyHeader(
            LumieCI lci,
            ScrollView scrollView,
            VisualElement stickyTarget,
            VisualElement elementAboveTarget) : base()
        {
            _lci = lci;
            _scrollView = scrollView;
            _stickyTarget = stickyTarget;
            _elementAboveTarget = elementAboveTarget;

            this.style.position = Position.Absolute;
            this.style.top = 0;
            this.style.left = 0;
            this.style.right = 0;
            this.style.bottom = 0;
            this.pickingMode = PickingMode.Ignore;

            _scrollView.RegisterCallback<GeometryChangedEvent>(OnLayoutReady);
            _scrollView.contentViewport.RegisterCallback<GeometryChangedEvent>(OnScrollViewResized);
            _elementAboveTarget.RegisterCallback<GeometryChangedEvent>(OnElementAboveTargetResized);
        }

        private void OnScrollViewResized(GeometryChangedEvent evt)
        {
            this.style.width = evt.newRect.width;
        }

        private void OnLayoutReady(GeometryChangedEvent evt)
        {
            _scrollView.UnregisterCallback<GeometryChangedEvent>(OnLayoutReady);
            _scrollView.verticalScroller.valueChanged += OnScroll;

            _stickyOriginalIndex = _scrollView.IndexOf(_stickyTarget);

            // Create a spacer matching the sticky element's height
            _spacer = new VisualElement();
            _spacer.style.height = _stickyTarget.resolvedStyle.height;
            _spacer.style.display = DisplayStyle.None;

            _scrollView.contentContainer.Insert(_stickyOriginalIndex + 1, _spacer);
        }

        private void OnElementAboveTargetResized(GeometryChangedEvent evt)
        {
            if (evt.newRect.height <= 0) return;

            _stickyTriggerOffset = evt.newRect.height;
        }

        private void OnScroll(float scrollY)
        {
            if (scrollY >= _stickyTriggerOffset)
                PinStickyElement(scrollY);
            else
                UnpinStickyElement();
        }

        private void PinStickyElement(float scrollY)
        {
            this.Add(_stickyTarget);
            _spacer.style.display = DisplayStyle.Flex;
        }

        private void UnpinStickyElement()
        {
            _scrollView.Insert(_stickyOriginalIndex, _stickyTarget);
            _spacer.style.display = DisplayStyle.None;
        }
    }
}