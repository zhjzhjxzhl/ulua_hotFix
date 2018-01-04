using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// <para>Author: zhaojun zhjzhjxzhl@163.com</para>
/// <para>Date: $time$</para>
/// <para>$Id: TempleteMethod.cs 6294 2014-09-19 07:33:18Z zhaojun $</para>
/// </summary>
public class TempleteMethod{

    /// <summary>
    /// 没有结果的固定引用。避免反复new。
    /// </summary>
    private static KeyValuePair<bool,object> notHaveFix = new KeyValuePair<bool,object>(false,null);
    /// <summary>
    /// 已经检查过没有的函数，不要每次都去走getLuaFunction了。这个方法效率有点低的。
    /// </summary>
    private static Dictionary<string, bool> notHave = new Dictionary<string, bool>();
    public static KeyValuePair<bool, object> InjectMethod(string name, params object[] args)
    {
        if(notHave.ContainsKey(name))
        {
            return notHaveFix;
        }
        DynamicScript.lua.DoFile("hotFix");
        string selectFName = name;
        if(DynamicScript.lua.IsFuncExists(selectFName))
        { 
            LuaInterface.LuaFunction selectF = DynamicScript.lua.GetLuaFunction(selectFName);
            object[] result = selectF.Call(args);
            object ret = null;
            if (result != null && result.Length > 0)
            {
                ret = (object)result[0];
            }
            return new KeyValuePair<bool, object>(true, ret);
        }else
        {
            notHave.Add(name, true);
        }
        return notHaveFix;
    }
    public static object InjectMM(string name, params object[] args)
    {
        KeyValuePair<bool, object> ret = InjectMethod(name, args);
        if (ret.Key)
        {
            return ret.Value;
        }
        return name;
    }
}
