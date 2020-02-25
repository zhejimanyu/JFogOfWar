using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Threading;

public class UnityThread : MonoBehaviour 
{
	List<Action> actionList = new List<Action>();
	List<Action> runList = new List<Action>();

	AtomLock m_Lock = new AtomLock();

    void AddAction (Action action)
	{
		if (action == null)
		{
			return;
		}
		
		m_Lock.DoAction(() => actionList.Add(action));
	}

	void Update ()
	{
		m_Lock.DoAction(() =>
		{
			runList.AddRange(actionList);
			actionList.Clear();
		});

		foreach(Action action in runList)
		{
			action();
		}

		runList.Clear();
	}

	/// ---------------------------------------------------------------------

	static UnityThread _instance = null;

	public static void Init ()
	{
		if (_instance == null)
		{
			GameObject o = new GameObject("UnityThread");
			_instance = o.AddComponent<UnityThread>();
			DontDestroyOnLoad(o);
            o.hideFlags = HideFlags.HideInHierarchy;
        }
	}

	public static void PostAction (Action action)
	{
		if (_instance == null)
		{
			return;
		}

		_instance.AddAction(action);
	}
}
