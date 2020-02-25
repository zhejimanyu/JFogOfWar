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

public class JFOWLogic : MonoBehaviour
{

    public List<JFOWFieldViewer> viewers;

    private void Awake()
    {
        UnityThread.Init();
    }

    void Start()
    {
        foreach (var item in viewers)
        {
            item.SetValid();
            JFOWSystem.instance.AddFieldViewer(item);
        }
    }

    void OnGUI()
    {
        if (GUILayout.Button("移除视野"))
        {
            foreach (var item in viewers)
            {
                JFOWSystem.instance.RemoveFieldViewer(item);
            }
        }
    }
}
