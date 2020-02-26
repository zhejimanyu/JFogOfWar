/*
*	Author(作者)：gzj
*	Description(描述)：
*	Version(版本):
*	LogicFlow(逻辑流程):
*	TODO:
*/
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(JFOWRender))]
public class JFOWRenderInspector : Editor
{

    JFOWRender m_Target;

    void OnEnable()
    {
        m_Target = target as JFOWRender;
    }

    public override bool HasPreviewGUI()
    {
        if (m_Target == null)
            return false;

        return JFOWSystem.instance.texture != null;
    }

    public override void DrawPreview(Rect previewArea)
    {
        //base.DrawPreview(previewArea);
        if (m_Target != null && JFOWSystem.instance.texture != null)
            GUI.DrawTexture(previewArea, JFOWSystem.instance.texture);
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        if (GUILayout.Button("重置紋理"))
        {
            if (JFOWSystem.instance.texture != null)
            {
                JFOWSystem.instance.ReseTexture();
            }
        }
    }
}
