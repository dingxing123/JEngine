
/*
 * JEngine自动生成的编辑器脚本，作者已经代替你掉了头发，帮你写出了这个编辑器脚本，让你能够直接看对象序列化后的字段
 */
#if UNITY_EDITOR
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using ILRuntime.Runtime.Enviorment;
using JEngine.Core;
using LitJson;
using ProjectAdapter;
using UnityEditor;
using UnityEditor.AnimatedValues;
using UnityEngine;
using Object = UnityEngine.Object;
using Tools = JEngine.Core.Tools;

[CustomEditor(typeof(ExampleAPI), true)]
public class ExampleAPIAdapterEditor : Editor
{
    private bool displaying;

    private AnimBool[] fadeGroup = new AnimBool[2];

    private async void OnEnable()
    {
        for (int i = 0; i < fadeGroup.Length; i++)
        {
            fadeGroup[i] = new AnimBool(false);
            fadeGroup[i].valueChanged.AddListener(Repaint);
        }

        displaying = true;

        while (true && Application.isEditor && Application.isPlaying && displaying)
        {
            try
            {
                Repaint();
            }
            catch
            {
            }

            await Task.Delay(500);
        }
    }

    private void OnDestroy()
    {
        displaying = false;
    }

    private void OnDisable()
    {
        // 移除动画监听
        for (int i = 0; i < fadeGroup.Length; i++)
        {
            fadeGroup[i].valueChanged.RemoveListener(Repaint);
        }

    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        ExampleAPIAdapter.Adapter clr = target as ExampleAPIAdapter.Adapter;
        var instance = clr.ILInstance;
        if (instance != null)
        {
            EditorGUILayout.LabelField("Script", clr.ILInstance.Type.Name);

            int index = 0;
            foreach (var i in instance.Type.FieldMapping)
            {
                //这里是取的所有字段，没有处理不是public的
                var name = i.Key;
                var type = instance.Type.FieldTypes[index];
                index++;

                var cType = type.TypeForCLR;
                object obj = instance[i.Value];
                if (cType.IsPrimitive) //如果是基础类型
                {
                    GUI.enabled = true;
                    try
                    {
                        if (cType == typeof(float))
                        {
                            instance[i.Value] = EditorGUILayout.FloatField(name, (float) instance[i.Value]);
                        }
                        else if (cType == typeof(double))
                        {
                            instance[i.Value] = EditorGUILayout.DoubleField(name, (float) instance[i.Value]);
                        }
                        else if (cType == typeof(int))
                        {
                            instance[i.Value] = EditorGUILayout.IntField(name, (int) instance[i.Value]);
                        }
                        else if (cType == typeof(long))
                        {
                            instance[i.Value] = EditorGUILayout.LongField(name, (long) instance[i.Value]);
                        }
                        else if (cType == typeof(bool))
                        {
                            var result = bool.TryParse(instance[i.Value].ToString(), out var value);
                            if (!result)
                            {
                                value = instance[i.Value].ToString() == "1";
                            }

                            instance[i.Value] = EditorGUILayout.Toggle(name, value);
                        }
                        else
                        {
                            EditorGUILayout.LabelField(name, instance[i.Value].ToString());
                        }
                    }
                    catch (Exception e)
                    {
                        Log.PrintError($"无法序列化{name}，{e.Message}");
                        EditorGUILayout.LabelField(name, instance[i.Value].ToString());
                    }
                }
                else
                {
                    if (cType == typeof(string))
                    {
                        if (obj != null)
                        {
                            instance[i.Value] = EditorGUILayout.TextField(name, (string) instance[i.Value]);
                        }
                        else
                        {
                            instance[i.Value] = EditorGUILayout.TextField(name, "");
                        }
                    }
                    else if (cType == typeof(JsonData)) //可以折叠显示Json数据
                    {
                        if (instance[i.Value] != null)
                        {
                            fadeGroup[1].target = EditorGUILayout.Foldout(fadeGroup[1].target, name, true);
                            if (EditorGUILayout.BeginFadeGroup(fadeGroup[1].faded))
                            {
                                instance[i.Value] = EditorGUILayout.TextArea(
                                    ((JsonData) instance[i.Value]).ToString()
                                );
                            }

                            EditorGUILayout.EndFadeGroup();
                            EditorGUILayout.Space();
                        }
                        else
                        {
                            EditorGUILayout.LabelField(name, "暂无值的JsonData");
                        }
                    }
                    else if (typeof(Object).IsAssignableFrom(cType))
                    {
                        if (obj == null && cType.GetInterfaces().Contains(typeof(CrossBindingAdaptorType)))
                        {
                            EditorGUILayout.LabelField(name, "未赋值的热更类");
                            break;
                        }

                        if (cType.GetInterfaces().Contains(typeof(CrossBindingAdaptorType)))
                        {
                            try
                            {
                                var clrInstance = (MonoBehaviour) Tools.FindObjectsOfTypeAll<CrossBindingAdaptorType>()
                                    .Find(adaptor =>
                                        adaptor.ILInstance == instance[i.Value]);
                                GUI.enabled = true;
                                EditorGUILayout.ObjectField(name, clrInstance, cType, true);
                                GUI.enabled = false;
                            }
                            catch
                            {
                                EditorGUILayout.LabelField(name, "未赋值的热更类");
                            }

                            break;
                        }

                        //处理Unity类型
                        var res = EditorGUILayout.ObjectField(name, obj as Object, cType, true);
                        instance[i.Value] = res;
                    }
                    //可绑定值，可以尝试更改
                    else if (type.ReflectionType.ToString().Contains("BindableProperty") && obj != null)
                    {
                        PropertyInfo fi = type.ReflectionType.GetProperty("Value");
                        object val = fi.GetValue(obj);

                        string genericTypeStr = type.ReflectionType.ToString().Split('`')[1].Replace("1<", "")
                            .Replace(">", "");
                        Type genericType = Type.GetType(genericTypeStr);
                        if (genericType == null ||
                            (!genericType.IsPrimitive && genericType != typeof(string))) //不是基础类型或字符串
                        {
                            EditorGUILayout.LabelField(name, val.ToString()); //只显示字符串
                        }
                        else
                        {
                            //可更改
                            var data = Tools.ConvertSimpleType(EditorGUILayout.TextField(name, val.ToString()),
                                genericType);
                            if (data != null) //尝试更改
                            {
                                fi.SetValue(obj, data);
                            }
                        }
                    }
                    else
                    {
                        //其他类型现在没法处理
                        if (obj != null)
                        {
                            if (cType.GetInterfaces().Contains(typeof(CrossBindingAdaptorType))) //需要跨域继承的普遍都有适配器
                            {
                                var clrInstance = (MonoBehaviour) Tools.FindObjectsOfTypeAll<CrossBindingAdaptorType>()
                                    .Find(adaptor =>
                                        adaptor.ILInstance.Equals(instance[i.Value]));
                                if (clrInstance != null)
                                {
                                    GUI.enabled = true;
                                    EditorGUILayout.ObjectField(name, clrInstance.gameObject, typeof(GameObject), true);
                                    GUI.enabled = false;
                                }
                            }
                            else //不需要跨域继承的，也有可能以ClassBind挂载
                            {
                                var clrInstance = Tools.FindObjectsOfTypeAll<MonoBehaviourAdapter.Adaptor>()
                                    .Find(adaptor =>
                                        adaptor.ILInstance.Equals(instance[i.Value]));
                                if (clrInstance != null)
                                {
                                    GUI.enabled = true;
                                    EditorGUILayout.ObjectField(name, clrInstance, typeof(MonoBehaviourAdapter.Adaptor),
                                        true);
                                    GUI.enabled = false;
                                }
                                else
                                {
                                    EditorGUILayout.LabelField(name, obj.ToString());
                                }
                            }
                        }
                        else
                            EditorGUILayout.LabelField(name, "(null)");
                    }
                }
            }
        }

        // 应用属性修改
        serializedObject.ApplyModifiedProperties();
    }

}
#endif
