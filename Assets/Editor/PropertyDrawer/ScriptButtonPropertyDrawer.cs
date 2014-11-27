//
// GUI code for ScriptButtonAttribute, 
// which gives draws a button for a dummy property which calls a method on the corresponding script when pressed
// The method must have this signature:
//    public void MethodName(string propertyPath) { }
//

using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System;

[CustomPropertyDrawer(typeof(ScriptButtonAttribute))]
public class ScriptButtonProperyDrawer : PropertyDrawer
{

   ScriptButtonAttribute buttonAttribute { get { return ((ScriptButtonAttribute)attribute); } }

   public override void OnGUI(Rect pos, SerializedProperty prop, GUIContent label)
   {
      float depthOffset = prop.depth * 15; //compensate for indent level of property so button appears to be correct width
      pos.width -= depthOffset;
      pos.x += depthOffset;
      if(GUI.Button(pos, buttonAttribute.ButtonLabel))
      {
         UnityEngine.Object obj = prop.serializedObject != null ? prop.serializedObject.targetObject : null;
         if(obj != null)
         {
            Type thisType = obj.GetType();
            MethodInfo theMethod = thisType.GetMethod(buttonAttribute.MethodNameToCall);
            if (theMethod != null)
            {
               theMethod.Invoke(obj, new object[]{prop.propertyPath});
               EditorUtility.SetDirty(prop.serializedObject.targetObject);
            }
            else
               Debug.LogError("Can't call method '" + buttonAttribute.MethodNameToCall + "' because its not a PUBLIC method on class '" + thisType.ToString() + "'");
         }
      }
   }
}