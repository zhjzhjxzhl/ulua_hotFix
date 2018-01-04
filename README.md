# ulua_hotFix
add hotfix to ulua
之前工程用了tolua,为了开发速度能快点，没有全部选在lua。而是只有活动部分选择了lua热更新。基本功能部分，还是c#开发的，那么修复的时候就略麻烦。后来xlua提出了热修复功能，但是已经用了ulua了，也懒得换成xlua。于是本着自己动手（造轮子），丰衣足食的程序员精神，参考了下xlua自己给tolua里也简单加了个hotfix的功能，功能比较受限，主要是c#和lua的数据结构并不能很好的兼容的原因。如果在游戏测试阶段，勉强应付下热修复还是可以的。
原理和xlua是一样的，直接在编译之后，在标记为hotfix的类的方法调用出插入lua修复调用。对于数值型返回，没有做类型支持，需要用数值型的，可以进一步自己处理。因为lua只有double类型，而c#的类型转换有box和ubox过程。传个类型过去做个转换也容易处理的。
闲话不说了，直接上代码。
标记类：

//////////定义一个属性
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

方法模板类：如果要扩充类型，就这里扩充就好了。
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

重点来了，工具类。就是这个进行替换的。在项目编译好之后，运行这个，给标签打上。每个方法都会调用上面那个类里的injectmethod了。
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using System.Collections.Generic;
using System;
using System.Reflection;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;


/// <summary>
/// <para>Author: zhaojun zhjzhjxzhl@163.com</para>
/// <para>Date: $time$</para>
/// <para>$Id: ChangeUse.cs 6294 2014-09-19 07:33:18Z zhaojun $</para>
/// </summary>
public class ChangeUse {


    [MenuItem("Utils/HotFixGenerate")]
    public static void Test()
    {
string path = "./Library/ScriptAssemblies/Assembly-CSharp.dll";
        AssemblyDefinition assembly = AssemblyDefinition.ReadAssembly(path);
bool haveInjected = false;
        if (assembly.MainModule.Types.Any(t => t.Name == "__HOTFIXE_FLAG"))
        {
            Debug.Log("had injected!");
haveInjected = true;
        }
        TypeReference objType = assembly.MainModule.Import(typeof(object));
assembly.MainModule.Types.Add(new TypeDefinition("__HOTFIX_GEN", "__HOTFIXE_FLAG", Mono.Cecil.TypeAttributes.Class,
            objType));


        var AAAttribute = assembly.MainModule.Types.Single(t => t.FullName == "HotFixAttribute");
        List<TypeDefinition> hotfix_delegates = (from module in assembly.Modules
                            from type in module.Types
                            where type.CustomAttributes.Any(ca => ca.AttributeType == AAAttribute)
                            select type).ToList();


        


List<MethodDefinition> ms = (from module in assembly.Modules
               from type in module.Types
               from method in type.Methods
                            where method.Name == "InjectMethod"
               select method).ToList();




        var voidType = assembly.MainModule.Import(typeof(void));


foreach (TypeDefinition td in hotfix_delegates) {
Mono.Collections.Generic.Collection<MethodDefinition> methods = td.Methods;
foreach(MethodDefinition method in methods)
{
//构造函数，和基本返回类型不处理
if(!haveInjected &&  !method.IsConstructor && (!method.ReturnType.IsValueType))
{
//方法的参数的个数
int param_count = method.Parameters.Count + (method.IsStatic ? 0 : 1);


string name = (td.FullName+"_"+method.Name);
name = name.Replace(".","_");
var insertPoint = method.Body.Instructions[0];
var processor = method.Body.GetILProcessor();

if (method.IsConstructor)
{
insertPoint = findNextRet(method.Body.Instructions, insertPoint);
}


Instruction para = processor.Create(OpCodes.Ldstr,name);
processor.InsertBefore(insertPoint,para);


processor.InsertBefore(insertPoint,processor.Create(OpCodes.Ldc_I4,param_count));
processor.InsertBefore(insertPoint,processor.Create(OpCodes.Newarr,objType));


for (int i = 0; i < param_count; i++)
{
processor.InsertBefore(insertPoint,processor.Create(OpCodes.Dup));
processor.InsertBefore(insertPoint,processor.Create(OpCodes.Ldc_I4,i));
if (i < ldargs.Length)
{
//将参数装入列表
processor.InsertBefore(insertPoint, processor.Create(ldargs[i]));
}
else
{
processor.InsertBefore(insertPoint, processor.Create(OpCodes.Ldarg, (short)i));
}
int index = i;
if(!method.IsStatic)
{
index -= 1;
}
if(index>=0 && method.Parameters[index].ParameterType.IsValueType)
{
processor.InsertBefore(insertPoint,processor.Create(OpCodes.Box,method.Parameters[index].ParameterType));
}
processor.InsertBefore(insertPoint,processor.Create(OpCodes.Stelem_Ref));
}
Instruction ii = processor.Create(OpCodes.Call,ms[0].GetElementMethod());
processor.InsertBefore(insertPoint,ii);

//处理结果
var keyVPType = assembly.MainModule.Import(typeof(KeyValuePair<bool,object>));
var mr = new VariableDefinition(keyVPType);
method.Body.Variables.Add(mr);
method.Body.InitLocals = true;
processor.InsertBefore(insertPoint,processor.Create(OpCodes.Stloc,mr));
processor.InsertBefore(insertPoint,processor.Create(OpCodes.Ldloca_S,mr));
var met = assembly.MainModule.Import(typeof(KeyValuePair<bool,object>).GetMethod("get_Key"));
processor.InsertBefore(insertPoint,processor.Create(OpCodes.Call,met));
processor.InsertBefore(insertPoint,processor.Create(OpCodes.Brfalse,insertPoint));


if(method.ReturnType.FullName == voidType.FullName)
{
//如果返回时空，则弹出所有的栈。已经没有栈了，上面的判断用过了。
//processor.InsertBefore(insertPoint,processor.Create(OpCodes.Pop));
}else{
                   processor.InsertBefore(insertPoint, processor.Create(OpCodes.Ldloca_S,mr));
met = assembly.MainModule.Import(typeof(KeyValuePair<bool,object>).GetMethod("get_Value"));
processor.InsertBefore(insertPoint,processor.Create(OpCodes.Call,met));


//返回类型转换。
//if(method.ReturnType.IsValueType)
//{
//基础类型，不处理。
//processor.InsertBefore(insertPoint,processor.Create(OpCodes.Unbox_Any,method.ReturnType));
//}else{
//processor.InsertBefore(insertPoint,processor.Create(OpCodes.Castclass,method.ReturnType));
//}
}
processor.InsertBefore(insertPoint,processor.Create(OpCodes.Ret));
//直接复制testCall从stloc.0直到第一个return开始的部分
/*bool start = false;
foreach(Instruction ins in testCall.Body.Instructions)
{
if(ins.OpCode == OpCodes.Stloc_0)
{
//复制开始
start = true;
}
if(start)
{
processor.InsertBefore(insertPoint,ins);
}
if(ins.OpCode == OpCodes.Ret)
{
//结束复制
break;
}
}*/


Debug.Log("canFix : "+name);
}
}
}
if (!haveInjected) {
assembly.Write (path);
}
} 
static OpCode[] ldargs = new OpCode[] { OpCodes.Ldarg_0, OpCodes.Ldarg_1, OpCodes.Ldarg_2, OpCodes.Ldarg_3 };


static Instruction findNextRet(Mono.Collections.Generic.Collection<Instruction> instructions, Instruction pos)
{
bool posFound = false;
for(int i = 0; i < instructions.Count; i++)
{
if (posFound && instructions[i].OpCode == OpCodes.Ret)
{
return instructions[i];
}
else if (instructions[i] == pos)
{
posFound = true;
}
}
return null;
}
}
