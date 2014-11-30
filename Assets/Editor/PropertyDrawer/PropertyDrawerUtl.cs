//
// Some utl funcs for PropertyDrawer classes
//

using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Reflection;

public class PropertyDrawerUtl  
{

   static MethodInfo boldFontMethodInfo = null;
   //bolds property name if it is overriding the prefab value
   //mimics the behavior of the standard unity property editor...
   public static void BoldForPrefabOverride(SerializedProperty prop)
   {
      if (boldFontMethodInfo == null)
         boldFontMethodInfo = typeof(EditorGUIUtility).GetMethod("SetBoldDefaultFont", BindingFlags.Static | BindingFlags.NonPublic);
      boldFontMethodInfo.Invoke(null, new[] { prop.prefabOverride as object });
   }
}
