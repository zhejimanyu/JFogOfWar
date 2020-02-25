/*
*	Author(作者)：gzj
*	Description(描述)：
*	Version(版本):
*	LogicFlow(逻辑流程):
*	TODO:
*/
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Singleton<T> where T : class, new()
{

    private static T m_instance;
    public static T instance
    {
        get
        {
            if (m_instance == null)
            {
                m_instance = Activator.CreateInstance<T>();
                if (m_instance != null)
                {
                    (m_instance as Singleton<T>).Init();
                }
            }

            return m_instance;
        }
    }

    public virtual void Init()
    {

    }

    public virtual void Release()
    {
        OnRelease();

        if (m_instance != null)
            m_instance = null;
    }

    public static void OnRelease()
    {

    }

}
