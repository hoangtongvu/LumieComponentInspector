using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace LumieComponentInspector;

internal abstract class EditorWindowTitleButton
{
    protected readonly LumieCI _lci;

    public EditorWindowTitleButton(LumieCI lci) : base()
    {
        _lci = lci;
    }

    public void AttachButton()
    {
        var parentField = typeof(EditorWindow).GetField(
            "m_Parent",
            BindingFlags.Instance | BindingFlags.NonPublic);

        var hostView = parentField.GetValue(_lci);

        var showButtonField = hostView.GetType().BaseType.GetField(
            "m_ShowButton",
            BindingFlags.Instance | BindingFlags.NonPublic);

        var delegateType = showButtonField.FieldType;

        var method = GetType().GetMethod(
            nameof(DrawButton),
            BindingFlags.Instance | BindingFlags.NonPublic);

        var del = Delegate.CreateDelegate(
            delegateType,
            this,
            method);

        showButtonField.SetValue(hostView, del);
    }

    protected abstract void DrawButton(Rect rect);
}