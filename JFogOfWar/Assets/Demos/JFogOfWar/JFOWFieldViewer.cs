using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

interface IFOWFieldViewer
{
    /// <summary>
    /// 获取视野中心位置
    /// </summary>
    Vector3 GetPos();
    /// <summary>
    /// 获得视野半径
    /// </summary>
    float GetRadius();
    void Release();
    /// <summary>
    /// 指定的世界坐标是否可见
    /// </summary>
    bool IsVisiable(Vector3 pos);

    int GetMinX();
    int GetMinY();
    int GetMaxX(int worldSize);
    int GetMaxY(int worldSize);

    /// <summary>
    /// 该视野是否有效的，可用于队友死亡后，失去已扩展的视野
    /// </summary>
    bool IsValid();
}

public class JFOWFieldViewer : MonoBehaviour, IFOWFieldViewer
{
    [SerializeField]
    float m_Radius;

    bool m_IsValid = false;

    private void Start()
    {
        m_IsValid = false;
    }

    public void SetValid()
    {
        m_IsValid = true;
    }

    public int GetMinX()
    {
        int minX = Mathf.FloorToInt(GetPos().x - m_Radius);
        minX = minX < 0 ? 0 : minX;
        return minX;
    }

    public int GetMinY()
    {
        int minY = Mathf.FloorToInt(GetPos().y - m_Radius);
        minY = minY < 0 ? 0 : minY;
        return minY;
    }

    public int GetMaxX(int worldSize)
    {
        int maxX = Mathf.FloorToInt(GetPos().x + m_Radius);
        maxX = maxX > worldSize ? worldSize : maxX;
        return maxX;
    }

    public int GetMaxY(int worldSize)
    {
        int maxY = Mathf.FloorToInt(GetPos().y + m_Radius);
        maxY = maxY > worldSize ? worldSize : maxY;
        return maxY;
    }

    public Vector3 GetPos()
    {
        return transform.position;
    }

    public float GetRadius()
    {
        return m_Radius;
    }

    public bool IsVisiable(Vector3 pos)
    {
        return m_Radius >= Vector3.Distance(pos, transform.position);
    }

    public bool IsValid()
    {
        return m_IsValid;
    }

    public void Release()
    {
        m_IsValid = false;
    }
}
