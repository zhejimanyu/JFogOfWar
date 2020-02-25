using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JFOWRender : MonoBehaviour
{

    //未探索的颜色
    public Color unExploredColor = new Color(0, 0, 0, 255f);
    //已探索的颜色
    public Color exploredColor = new Color(0, 0, 0, 200f / 255f);
    Material m_Mat;

    private void Start()
    {
        MeshRenderer mr = transform.GetComponentInChildren<MeshRenderer>();
        if (mr != null)
            m_Mat = mr.sharedMaterial;
    }

    private void OnWillRenderObject()
    {
        if (m_Mat != null && JFOWSystem.instance.texture != null)
        {
            m_Mat.SetTexture("_MainTex", JFOWSystem.instance.texture);
            m_Mat.SetFloat("_BlendFactor", JFOWSystem.instance.blendFactor);

            if (JFOWSystem.instance.enableFog)
                m_Mat.SetColor("_UnExplored", unExploredColor);
            else
                m_Mat.SetColor("_UnExplored", exploredColor);
            m_Mat.SetColor("_Explored", exploredColor);
        }
    }

    private void OnDestroy()
    {
        m_Mat.SetTexture("_MainTex", null);
    }

}
