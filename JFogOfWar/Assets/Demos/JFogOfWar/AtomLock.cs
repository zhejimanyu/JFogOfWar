using System;
using System.Runtime.CompilerServices;
using System.Threading;
using UnityEngine;

/// <summary>
/// 原子锁 (自定义)。
/// 最轻量级的互斥锁。
/// 适用场景: 在锁期间 计算量非常少的情况，等待时间非常短。如果在锁的过程中，计算量非常大，等待时间特别长，则应该使用系统锁。
/// </summary>
public class AtomLock
{
	private int m_Value = 0;
	
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Lock ()
	{
		while(0 != Interlocked.Exchange(ref m_Value, 1))
		{
			Thread.Sleep(0);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void UnLock ()
	{
		// m_Value = 0;
		Interlocked.Exchange(ref m_Value, 0);
	}
	
	/// <summary>
	/// 此接口可防止在锁的过程中出现异常导致死锁的问题
	/// </summary>
	/// <param name="action"></param>
	/// <returns></returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void DoAction(Action action)
	{
		this.Lock();
		try
		{
			action?.Invoke();
		}
		catch (Exception e)
		{
            Debug.LogError(e);
		}
		finally
		{
			this.UnLock();
		}
	}
}