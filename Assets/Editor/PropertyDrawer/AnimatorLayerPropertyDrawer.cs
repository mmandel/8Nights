//
// GUI code for AnimatorLayerAttribute, 
// which gives a custom drop-down to select an animator layer by name on an int property
//

using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

[CustomPropertyDrawer(typeof(AnimatorLayerAttribute))]
public class AnimatorLayerProperyDrawer : PropertyDrawer
{

   List<GUIContent> _animatorLayerNames = new List<GUIContent>();

   public override void OnGUI(Rect pos, SerializedProperty prop, GUIContent label)
   {
      //get the list of states
      GameObject go = (prop.serializedObject.targetObject as Component).gameObject;
      if(go != null)
      {
         Animator a = go.GetComponent<Animator>();
         if(a != null)
         {
            UnityEditorInternal.AnimatorController ac = a.runtimeAnimatorController as UnityEditorInternal.AnimatorController;
            if(ac != null)
            {
               if (_animatorLayerNames.Count != ac.layerCount)
               {
                  _animatorLayerNames.Clear();
                  for (int i = 0; i < ac.layerCount; i++)
                  {
                     _animatorLayerNames.Add(new GUIContent(ac.GetLayer(i).name));
                  }
               }
            }
         }
      }

      //check if the animator layer was deleted, and default to error string
      int curValue = prop.intValue;
      bool addedError = false;
      if (prop.intValue >= _animatorLayerNames.Count)
      {
         addedError = true;
         _animatorLayerNames.Add(new GUIContent("<ERROR!>"));
         curValue = _animatorLayerNames.Count - 1;
      }

      EditorGUI.BeginChangeCheck();
      PropertyDrawerUtl.BoldForPrefabOverride(prop); //make sure we bold the property name if overridden on prefab
      int selectedLayer = EditorGUI.Popup(pos, label, prop.intValue, _animatorLayerNames.ToArray());
      if(EditorGUI.EndChangeCheck())
      {
         prop.intValue = selectedLayer;
         EditorUtility.SetDirty(prop.serializedObject.targetObject);
      }

      //clean up
      if (addedError)
         _animatorLayerNames.RemoveAt(_animatorLayerNames.Count - 1);
   }
}