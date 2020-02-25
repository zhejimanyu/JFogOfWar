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

public class MonoSingleton<T> : MonoBehaviour where T : Component
{

    private static T m_instance = null;

    public static T instance
    {
        get
        {
            if (m_instance == null)
            {
                m_instance = GameObject.FindObjectOfType(typeof(T)) as T;
                if (m_instance == null)
                {
                    GameObject go = new GameObject(typeof(T).Name);
                    m_instance = go.AddComponent<T>();
                    GameObject parent = GameObject.Find("SingleParent");
                    if (parent != null)
                    {
                        go.transform.SetParent(parent.transform);
                    }
                }
            }

            return m_instance;
        }
    }

    private void Awake()
    {
        if (m_instance == null)
        {
            m_instance = this as T;
        }

        DontDestroyOnLoad(gameObject);
        Init();
    }

    protected virtual void Init()
    {

    }

    public virtual void Release()
    {
        OnRelease();
        MonoSingleton<T>.m_instance = null;
        UnityEngine.Object.Destroy(gameObject);
    }

    public virtual void OnRelease()
    {

    }

}
