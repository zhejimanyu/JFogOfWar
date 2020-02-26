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

public class JFOWSystem : MonoSingleton<JFOWSystem>
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
    public Texture2D texture => m_Texture;

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

    public bool enableRender = true;
    public bool enableFog = true;

    /// <summary>
    /// 由update控制变化，用于shader中fog的插值变化
    /// </summary>
    [SerializeField]
    float m_BlendFactor = 0f;
    public float blendFactor => m_BlendFactor;

    /// <summary>
    /// 纹理过渡时间
    /// </summary>
    public float texBlendTime = 0.5f;

    /// <summary>
    /// 地图左下角原点
    /// </summary>
    Vector3 m_Origin;

    bool m_ThreadWork = true;

    System.Diagnostics.Stopwatch m_SW;

    /// <summary>
    /// 模糊次数
    /// </summary>
    public int blurIterations = 2;

    /// <summary>
    /// 更新缓冲耗时
    /// </summary>
    [SerializeField]
    float consumeTime;

    public void AddFieldViewer(IFOWFieldViewer viewer)
    {
        this.m_Viewer.Add(viewer);
    }

    public void RemoveFieldViewer(IFOWFieldViewer viewer)
    {
        if (m_Viewer.Contains(viewer))
        {
            m_Viewer.Remove(viewer);
            viewer.Release();
        }
    }

    private void Start()
    {
        Init();

        m_Thread = new Thread(ThreadCheckUpdate);
        m_Thread.Start();
    }

    protected override void Init()
    {
        m_Buffer0 = new Color32[m_TextureSize * m_TextureSize];
        m_TexSizeDivideWorldSize = m_TextureSize / m_WorldSize;
        m_Origin = transform.position - new Vector3(m_WorldSize / 2, 0, m_WorldSize / 2);
        m_Viewer.Clear();
        m_Removes.Clear();
        m_Adds.Clear();
    }

    private void Update()
    {
        if (texBlendTime > 0)
            m_BlendFactor = Mathf.Clamp01(m_BlendFactor + Time.deltaTime / texBlendTime);
        else
            m_BlendFactor = 1;

        if (m_State == State.Wait)
        {
            float time = Time.time;
            if (m_UpdateTimer < time)
            {
                m_UpdateTimer = time + m_UpdateInterval;
                m_State = State.NeedUpdate;
            }
        }
        else if (m_State == State.UpdateTexture)
        {
            UpdateTexture();
        }
    }

    void ThreadCheckUpdate()
    {
        //计算耗时
        if (m_SW == null)
            m_SW = new System.Diagnostics.Stopwatch();

        while (m_ThreadWork)
        {
            if (!m_ThreadWork)
                return;

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

        //float factor = (texBlendTime > 0f) ? Mathf.Clamp01(m_BlendFactor + consumeTime / texBlendTime) : 1f;
    
        //for (int i = 0; i < m_Buffer0.Length; ++i)
        //{
        //    m_Buffer0[i].g = (byte)Mathf.Lerp(m_Buffer0[i].b, m_Buffer0[i].g, factor);
        //}

        //记录上一帧可见区域
        ResetLastVisable();

        //计算当前可见范围
        ResetBuffer(m_Buffer0, "g");
        for (int i = 0; i < m_Viewer.Count; ++i)
            CalCurVisible(m_Viewer[i]);

        //模糊
        for (int i = 0; i < blurIterations; ++i)
            BlurVisible();

        //叠加
        OverlayBuffer();
        
        m_State = State.UpdateTexture;
    }

    void ResetLastVisable()
    {
        for (int i = 0; i < m_Buffer0.Length; ++i)
        {
            m_Buffer0[i].b = m_Buffer0[i].a;
        }
    }

    void CalCurVisible(IFOWFieldViewer viewer)
    {
        if (!viewer.IsValid()) return;

        ////纹理点 = 世界点 * 纹理宽/世界宽
        //Vector3 texPos = (viewer.GetPos() - m_Origin) * m_TexSizeDivideWorldSize;
        //texPos = new Vector3(Mathf.Clamp(texPos.x, 0, m_TextureSize), 0, Mathf.Clamp(texPos.z, 0, m_TextureSize));

        //float texRadius = viewer.GetRadius() * m_TexSizeDivideWorldSize + radiusOffset;
        //texRadius = Mathf.Clamp(texRadius, 0, m_TextureSize);

        ////只处理视野体可视半径范围内的坐标
        ////RoundToInt返回四舍五入到最接近的整数的f
        //int minX = Mathf.RoundToInt(texPos.x - texRadius);
        //int maxX = Mathf.RoundToInt(texPos.x + texRadius);
        //int minY = Mathf.RoundToInt(texPos.z - texRadius);
        //int maxY = Mathf.RoundToInt(texPos.z + texRadius);
        //minX = Mathf.Clamp(minX, 0, m_TextureSize);
        //minY = Mathf.Clamp(minY, 0, m_TextureSize);
        //maxX = Mathf.Clamp(maxX, 0, m_TextureSize);
        //maxY = Mathf.Clamp(maxY, 0, m_TextureSize);

        //float distSqrt = 0f;
        //float radiusSqrt = texRadius * texRadius;

        //for (int i = minX; i < maxX; ++i)
        //{
        //    for (int j = minY; j < maxY; ++j)
        //    {
        //        distSqrt = (i - texPos.x) * (i - texPos.x) + (j - texPos.y) * (j - texPos.y);
        //        if (distSqrt <= radiusSqrt)
        //        {
        //            //if (curCount < maxCount)
        //            //{
        //            //    curCount++;
        //            //    UnityThread.PostAction(delegate
        //            //    {
        //            //        Debug.LogError($"可视：{i} {j} {i + j * m_TextureSize} {(i - 1) * m_TextureSize + (j - 1)} {i * m_TextureSize + j}");
        //            //    });
        //            //}
        //            //m_Buffer0[(i - 1) * m_TextureSize + (j - 1)].g = 255;
        //            //m_Buffer0[i * m_TextureSize + j].g = 255;
        //            m_Buffer0[i + j * m_TextureSize].g = 255;
        //        }
        //    }
        //}

        //相对于战争迷雾的位置
        Vector3 pos = (viewer.GetPos() - m_Origin) * m_TexSizeDivideWorldSize;//纹理上的坐标
        float radius = viewer.GetRadius() * m_TexSizeDivideWorldSize - radiusOffset;//纹理上的半径

        // Coordinates we'll be dealing with
        //我们将要处理的坐标
        //RoundToInt返回四舍五入到最接近的整数的f
        int xmin = Mathf.RoundToInt(pos.x - radius);
        int ymin = Mathf.RoundToInt(pos.z - radius);
        int xmax = Mathf.RoundToInt(pos.x + radius);
        int ymax = Mathf.RoundToInt(pos.z + radius);

        int cx = Mathf.RoundToInt(pos.x);
        int cy = Mathf.RoundToInt(pos.z);

        cx = Mathf.Clamp(cx, 0, m_TextureSize - 1);
        cy = Mathf.Clamp(cy, 0, m_TextureSize - 1);

        int radiusSqr = Mathf.RoundToInt(radius * radius);

        for (int y = ymin; y < ymax; ++y)
        {
            if (y > -1 && y < m_TextureSize)
            {
                int yw = y * m_TextureSize;

                for (int x = xmin; x < xmax; ++x)
                {
                    if (x > -1 && x < m_TextureSize)
                    {
                        int xd = x - cx;
                        int yd = y - cy;
                        int dist = xd * xd + yd * yd;

                        // Reveal this pixel
                        if (dist < radiusSqr)
                        {
                            m_Buffer0[x + yw].g = 255;
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// 在一维数组中取索引：index = y+x*w //w为列数，x和y为水平竖直索引
    /// </summary>
    void BlurVisible()
    {
        //float maxValue = 0;
        //for (int i = 0; i < m_TextureSize; ++i)
        //{
        //    for (int j = 0; j < m_TextureSize; ++j)
        //    {
        //        int index = j + i * m_TextureSize;

        //        //int val = m_Buffer0[index].g;
        //        //val += m_Buffer0[j - 1 + i * m_TextureSize].g;
        //        //val += m_Buffer0[j + 1 + i * m_TextureSize].g;
        //        //val += m_Buffer0[j + (i - 1) * m_TextureSize].g;
        //        //val += m_Buffer0[j + (i + 1) * m_TextureSize].g;
        //        //val += m_Buffer0[j - 1 + (i - 1) * m_TextureSize].g;
        //        //val += m_Buffer0[j - 1 + (i + 1) * m_TextureSize].g;
        //        //val += m_Buffer0[j + 1 + (i - 1) * m_TextureSize].g;
        //        //val += m_Buffer0[j + 1 + (i + 1) * m_TextureSize].g;


        //        //maxValue = Mathf.Max(maxValue, j + i * m_TextureSize);
        //        //maxValue = Mathf.Max(maxValue, j - 1 + i * m_TextureSize);
        //        //maxValue = Mathf.Max(maxValue, j + 1 + i * m_TextureSize);
        //        //maxValue = Mathf.Max(maxValue, j + (i - 1) * m_TextureSize);
        //        //maxValue = Mathf.Max(maxValue, j + (i + 1) * m_TextureSize);
        //        //maxValue = Mathf.Max(maxValue, j - 1 + (i - 1) * m_TextureSize);
        //        //maxValue = Mathf.Max(maxValue, j - 1 + (i + 1) * m_TextureSize);
        //        //maxValue = Mathf.Max(maxValue, j + 1 + (i - 1) * m_TextureSize);
        //        //maxValue = Mathf.Max(maxValue, j + 1 + (i + 1) * m_TextureSize);

        //        maxValue = Mathf.Max(maxValue, i + j * m_TextureSize);
        //        maxValue = Mathf.Max(maxValue, i - 1 + j * m_TextureSize);
        //        maxValue = Mathf.Max(maxValue, i + 1 + j * m_TextureSize);
        //        maxValue = Mathf.Max(maxValue, i + (j - 1) * m_TextureSize);
        //        maxValue = Mathf.Max(maxValue, i + (j + 1) * m_TextureSize);
        //        maxValue = Mathf.Max(maxValue, i - 1 + (j - 1) * m_TextureSize);
        //        maxValue = Mathf.Max(maxValue, i - 1 + (j + 1) * m_TextureSize);
        //        maxValue = Mathf.Max(maxValue, i + 1 + (j - 1) * m_TextureSize);
        //        maxValue = Mathf.Max(maxValue, i + 1 + (j + 1) * m_TextureSize);

        //        //m_Buffer0[index].a = (byte)(val / 9);
        //    }
        //}
        //Debug.LogError("blur 2@@@@@@@@ maxValue:" + maxValue);


        for (int y = 0; y < m_TextureSize; ++y)
        {
            int yw = y * m_TextureSize;
            int yw0 = (y - 1);
            if (yw0 < 0) yw0 = 0;
            int yw1 = (y + 1);
            if (yw1 == m_TextureSize) yw1 = y;

            yw0 *= m_TextureSize;
            yw1 *= m_TextureSize;

            for (int x = 0; x < m_TextureSize; ++x)
            {
                int x0 = (x - 1);
                if (x0 < 0) x0 = 0;
                int x1 = (x + 1);
                if (x1 == m_TextureSize) x1 = x;

                int index = x + yw;
                int val = m_Buffer0[index].g;

                val += m_Buffer0[x0 + yw].g;
                val += m_Buffer0[x1 + yw].g;

                val += m_Buffer0[x + yw0].g;
                val += m_Buffer0[x0 + yw0].g;
                val += m_Buffer0[x1 + yw0].g;

                val += m_Buffer0[x + yw1].g;
                val += m_Buffer0[x0 + yw1].g;
                val += m_Buffer0[x1 + yw1].g;

                m_Buffer0[index].a = (byte)(val / 9);
            }
        }
    }

    void OverlayBuffer()
    {
        for (int i = 0; i < m_Buffer0.Length; ++i)
        {
            m_Buffer0[i].r += m_Buffer0[i].a;
            m_Buffer0[i].r = (byte)(Mathf.Clamp(m_Buffer0[i].r, 0, 255));
        }
    }

    void ResetBuffer(Color32[] buffer, string pass)
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

    void UpdateTexture()
    {
        if (!enableRender)
            return;

        if (m_Texture == null)
        {
            m_Texture = new Texture2D(m_TextureSize, m_TextureSize, TextureFormat.ARGB32, false);
            m_Texture.wrapMode = TextureWrapMode.Clamp;
        }

        m_Texture.SetPixels32(m_Buffer0);
        m_Texture.Apply();
        m_BlendFactor = 0f;//重置混合因子

        m_State = State.Wait;
    }

    public override void Release()
    {
        this.m_ThreadWork = false;
        if (m_Texture != null)
            GameObject.Destroy(m_Texture);
        this.m_Texture = null;
    }

    private void OnDestroy()
    {
        Release();
    }

    /// <summary>
    /// 是否已探索
    /// </summary>
    /// <param name="pos">世界坐标</param>
    public bool IsExplored(Vector3 pos)
    {
        if (m_Buffer0 == null)
            return false;

        pos -= m_Origin;

        int x = Mathf.RoundToInt(pos.x * m_TexSizeDivideWorldSize);
        int y = Mathf.RoundToInt(pos.z * m_TexSizeDivideWorldSize);

        x = Mathf.Clamp(x, 0, m_TextureSize);
        y = Mathf.Clamp(y, 0, m_TextureSize);

        return m_Buffer0[y + x * m_TextureSize].g > 0;
    }
}
