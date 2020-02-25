/*
 * 视野体
 * 系统:缓存状态
 * 迷雾计算器：传入视野体
 * 迷雾渲染器
 * buffer0,buffer1,buffer2三个缓冲
 * buffer0.r：当前所有已探索区域，buffer0.g:这一帧的可见区域，buffer.b:上一帧的可见区域，buffer.a：用于模糊处理的临时缓存
*/
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class JFOWSystem : MonoBehaviour
{

    public enum State
    {
        Wait,//等待，控制每次进行视野计算的间隔
        NeedUpdate,//进行缓冲计算
        UpdateTexture,//更新纹理,渲染出来
    }

    List<IFOWFieldViewer> m_Viewer = new List<IFOWFieldViewer>();
    List<IFOWFieldViewer> m_Adds = new List<IFOWFieldViewer>();
    List<IFOWFieldViewer> m_Removes = new List<IFOWFieldViewer>();

    /// <summary>
    /// r：当前所有已探索区域，g:这一帧的可见区域，b:上一帧的可见区域，a：用于模糊处理的临时缓存
    /// </summary>
    Color32[] m_Buffer0;

    Texture2D m_Texture;

    State m_State = State.Wait;

    float m_WorldSize = 512;
    int m_TextureSize = 512;

    /// <summary>
    /// 视野体半径偏移,修改该值可增加或减少可视半径
    /// </summary>
    public float radiusOffset = 0f;

    float m_UpdateInterval = 0.3f;

    float m_UpdateTimer = 0;

    float m_TexSizeDivideWorldSize;

    Thread m_Thread;

    /// <summary>
    /// 地图左下角原点
    /// </summary>
    Vector3 m_Origin;

    void AddFieldViewer(IFOWFieldViewer viewer)
    {
        this.m_Viewer.Add(viewer);
    }

    void RemoveFieldViewer(IFOWFieldViewer viewer)
    {
        if (m_Viewer.Contains(viewer))
            m_Viewer.Remove(viewer);
    }

    private void Start()
    {
        Init();

        m_Thread = new Thread(ThreadCheckUpdate);
        m_Thread.Start();
    }

    void Init()
    {
        m_Buffer0 = new Color32[m_TextureSize * m_TextureSize];
        m_TexSizeDivideWorldSize = m_TextureSize / m_WorldSize;
        m_Origin = transform.position - new Vector3(m_WorldSize / 2, 0, m_WorldSize / 2);
        m_Viewer.Clear();
        m_Removes.Clear();
        m_Adds.Clear();
    }

    bool m_ThreadWork;

    System.Diagnostics.Stopwatch m_SW;
    /// <summary>
    /// 耗时
    /// </summary>
    [SerializeField]
    float consumeTime;

    void ThreadCheckUpdate()
    {
        //计算耗时
        if (m_SW == null)
            m_SW = new System.Diagnostics.Stopwatch();

        while (m_ThreadWork)
        {
            if (m_State == State.NeedUpdate)
            {
                consumeTime = GetWatchSec(m_SW, UpdateBuffer);
            }
            Thread.Sleep(1);
        }
    }

    /// <summary>
    /// 计算耗时
    /// </summary>
    /// <returns>耗时，单位秒</returns>
    public float GetWatchSec(System.Diagnostics.Stopwatch sw, System.Action action)
    {
        sw.Reset();
        sw.Start();
        action?.Invoke();
        sw.Stop();
        return sw.ElapsedMilliseconds * 0.001f;
    }

    void UpdateBuffer()
    {
        if (m_Adds.Count > 0)
        {
            lock (m_Adds)
            {
                m_Viewer.AddRange(m_Adds);
                m_Adds.Clear();
            }
        }

        if (m_Removes.Count > 0)
        {
            lock (m_Removes)
            {
                foreach (var item in m_Removes)
                {
                    if (m_Viewer.Contains(item))
                        m_Viewer.Remove(item);
                }
                m_Removes.Clear();
            }
        }

        // r：当前所有已探索区域，g:这一帧的可见区域，b:上一帧的可见区域，a：用于模糊处理的临时缓存

        //计算当前可见范围
        ResetBuffer(m_Buffer0, "g");
        for (int i = 0; i < m_Viewer.Count; ++i)
            CalCurVisible(m_Viewer[i]);
        //模糊
        //叠加
        //记录上一帧可见区域
    }

    void CalCurVisible(IFOWFieldViewer viewer)
    {
        if (!viewer.IsValid()) return;

        //纹理点 = 世界点 * 纹理宽/世界宽
        Vector3 texPos = (viewer.GetPos() - m_Origin) * m_TexSizeDivideWorldSize;
        texPos = new Vector3(Mathf.Clamp(texPos.x, 0, m_TextureSize), 0, Mathf.Clamp(texPos.z, 0, m_TextureSize));

        float texRadius = viewer.GetRadius() * m_TexSizeDivideWorldSize + radiusOffset;
        texRadius = Mathf.Clamp(texRadius, 0, m_TextureSize);

        //只处理视野体可视半径范围内的坐标
        //RoundToInt返回四舍五入到最接近的整数的f
        int minX = Mathf.RoundToInt(texPos.x - texRadius);
        int maxX = Mathf.RoundToInt(texPos.x + texRadius);
        int minY = Mathf.RoundToInt(texPos.z - texRadius);
        int maxY = Mathf.RoundToInt(texPos.z + texRadius);
        minX = Mathf.Clamp(minX, 0, m_TextureSize);
        minY = Mathf.Clamp(minY, 0, m_TextureSize);
        maxX = Mathf.Clamp(maxX, 0, m_TextureSize);
        maxY = Mathf.Clamp(maxY, 0, m_TextureSize);

        float distSqrt = 0f;
        float radiusSqrt = texRadius * texRadius;

        for (int i = minX; i < maxX; ++i)
        {
            for (int j = minY; j < maxY; ++j)
            {
                distSqrt = (i - texPos.x) * (i - texPos.x) + (j - texPos.y) * (j - texPos.y);
                if (distSqrt <= radiusSqrt)
                {
                    m_Buffer0[(i - 1) * m_TextureSize + (j - 1)].g = 255;
                }
            }
        }
    }

    void ResetBuffer(Color32[] buffer,string pass)
    {
        for (int i = 0; i < buffer.Length; ++i)
        {
            if (pass.Equals("r"))
                buffer[i].r = (byte)0;
            if (pass.Equals("g"))
                buffer[i].g = (byte)0;
            if (pass.Equals("b"))
                buffer[i].b = (byte)0;
            if (pass.Equals("a"))
                buffer[i].a = (byte)0;
            if (pass.Equals("all"))
                buffer[i] = new Color(0, 0, 0, 0);
        }
    }

    private void Update()
    {
        if (m_State == State.Wait)
        {
            if (m_UpdateTimer < Time.deltaTime)
            {
                m_UpdateTimer += m_UpdateInterval;
                m_State = State.NeedUpdate;
            }
        }
        else if (m_State == State.UpdateTexture)
        {
            UpdateTexture();
        }
    }

    void UpdateTexture()
    {

    }

    public void Release()
    {
        this.m_ThreadWork = false;
        if (m_Texture != null)
            GameObject.Destroy(m_Texture);
        this.m_Texture = null;
    }



}
