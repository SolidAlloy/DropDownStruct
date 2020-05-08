﻿namespace DropDownStruct
{
    using System.Linq;
    using System.Reflection;
    using com.spacepuppy.Dynamic;
    using UnityEditor;
    using UnityEngine;

    [CustomPropertyDrawer(typeof(DropDownAttribute))]
    public class DropDownDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var dropDownAttribute = attribute as DropDownAttribute;
            var fields = dropDownAttribute.Fields;

            if (fields.Length != 0)
            {
                int index = FindByValue(fields, GetTargetObjectOfProperty(property));
                index = EditorGUI.Popup(position, fields[index].Name, index, GetFieldNames(fields));

                SetTargetObjectOfProperty(property, fields[index].GetValue(null));
            }
        }

        private static int FindByValue(FieldInfo[] fields, object value)
        {
            for (int i = 0; i < fields.Length; i++)
            {
                if (fields[i].GetValue(null).Equals(value))
                {
                    return i;
                }
            }

            return 0;
        }

        private static string[] GetFieldNames(FieldInfo[] fields)
        {
            return (from FieldInfo field in fields select field.Name).ToArray();
        }

        /// <summary>
        /// Gets the object the property represents.
        /// </summary>
        /// <param name="prop"></param>
        /// <returns></returns>
        private static object GetTargetObjectOfProperty(SerializedProperty prop)
        {
            if (prop == null)
            {
                return null;
            }

            var path = prop.propertyPath.Replace(".Array.data[", "[");
            object obj = prop.serializedObject.targetObject;
            var elements = path.Split('.');
            foreach (var element in elements)
            {
                if (element.Contains("["))
                {
                    var elementName = element.Substring(0, element.IndexOf("["));
                    var index = System.Convert.ToInt32(element.Substring(element.IndexOf("[")).Replace("[", string.Empty).Replace("]", string.Empty));
                    obj = GetValue_Imp(obj, elementName, index);
                }
                else
                {
                    obj = GetValue_Imp(obj, element);
                }
            }

            return obj;
        }

        private static void SetTargetObjectOfProperty(SerializedProperty prop, object value)
        {
            var path = prop.propertyPath.Replace(".Array.data[", "[");
            object obj = prop.serializedObject.targetObject;
            var elements = path.Split('.');
            foreach (var element in elements.Take(elements.Length - 1))
            {
                if (element.Contains("["))
                {
                    var elementName = element.Substring(0, element.IndexOf("["));
                    var index = System.Convert.ToInt32(element.Substring(element.IndexOf("[")).Replace("[", string.Empty).Replace("]", string.Empty));
                    obj = GetValue_Imp(obj, elementName, index);
                }
                else
                {
                    obj = GetValue_Imp(obj, element);
                }
            }

            if (obj is null)
            {
                return;
            }

            try
            {
                var element = elements.Last();

                if (element.Contains("["))
                {
                    var elementName = element.Substring(0, element.IndexOf("["));
                    var index = System.Convert.ToInt32(element.Substring(element.IndexOf("[")).Replace("[", string.Empty).Replace("]", string.Empty));
                    if (DynamicUtil.GetValue(element, elementName) is System.Collections.IList arr)
                    {
                        arr[index] = value;
                    }
                }
                else
                {
                    DynamicUtil.SetValue(obj, element, value);
                }
            }
            catch
            {
                return;
            }
        }

        private static object GetValue_Imp(object source, string name)
        {
            if (source == null)
            {
                return null;
            }

            var type = source.GetType();

            while (type != null)
            {
                var f = type.GetField(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                if (f != null)
                {
                    return f.GetValue(source);
                }

                var p = type.GetProperty(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                if (p != null)
                {
                    return p.GetValue(source, null);
                }

                type = type.BaseType;
            }

            return null;
        }

        private static object GetValue_Imp(object source, string name, int index)
        {
            if (!(GetValue_Imp(source, name) is System.Collections.IEnumerable enumerable))
            {
                return null;
            }

            var enm = enumerable.GetEnumerator();

            for (int i = 0; i <= index; i++)
            {
                if (!enm.MoveNext())
                {
                    return null;
                }
            }

            return enm.Current;
        }
    }
}
