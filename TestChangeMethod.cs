using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Reflection;

[HotFix("class")]
/// <summary>
/// <para>Author: zhaojun zhjzhjxzhl@163.com</para>
/// <para>Date: $time$</para>
/// <para>$Id: TestChange.cs 6294 2014-09-19 07:33:18Z zhaojun $</para>
/// </summary>
public class TestChangeMethod : MonoBehaviour
{
	public AAA aa(int a, AAA b,AAA c)
	{
		return c;
    }

    void OnGUI()
    {
        if (GUI.Button(new Rect(10, 100, 300, 150), "try"))
        {
			AAA aaa = new AAA("aaa");
			AAA bbb = new AAA("bbb");
            object obj111 = aa(20, aaa,bbb);
            Debug.Log((obj111 as AAA).name);

        }
    }
}

public class AAA
{
	public string name = "";
	public AAA(string name)
	{
		this.name = name;
	}
}

[AttributeUsage(AttributeTargets.Class,AllowMultiple=false,Inherited=false)]
class HotFixAttribute : System.Attribute
{
    protected string desc;
    public HotFixAttribute(string desc="")
    {
        this.desc = desc;
    }
    public string getDesc()
    {
        return desc;
    }
}
 
