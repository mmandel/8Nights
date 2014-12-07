//
// GUI code for AnimatorStateAttribute, 
// which gives a custom drop-down to select an animator state by name on a string property
//

using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

[CustomPropertyDrawer(typeof(AnimatorStateAttribute))]
public class AnimatorStateProperyDrawer : PropertyDrawer
{

   AnimatorStateAttribute stateAttribute { get { return ((AnimatorStateAttribute)attribute); } }

   public override void OnGUI(Rect pos, SerializedProperty prop, GUIContent label)
   {
      //get the list of states
      List<string> animatorStateNames = new List<string>();
      string kNoneOption = "<none>";
      animatorStateNames.Add(kNoneOption);

      //get the layer num from the property name passed through the attribute
      int layerNum = prop.serializedObject.FindProperty(stateAttribute.LayerPropName).intValue;
      GameObject go = (prop != null && prop.serializedObject != null) ? (prop.serializedObject.targetObject as Component).gameObject : null;
      if (go != null)
      {
         Animator a = go.GetComponent<Animator>();
         if (a != null)
         {
            UnityEditorInternal.AnimatorController ac = a.runtimeAnimatorController as UnityEditorInternal.AnimatorController;
            if ((ac != null) && (ac.GetLayer(layerNum) != null))
            {
               UnityEditorInternal.StateMachine sm = ac.GetLayer(layerNum).stateMachine;
               for (int i = 0; i < sm.stateCount; i++) 
               {
                  UnityEditorInternal.State state = sm.GetState(i);
                  animatorStateNames.Add(state.name);
               }
            }
         }
      }
         
      //check if the animator state was deleted, and default to error string
      string curValue = kNoneOption;
      string kErrorName = "<ERROR!>";
      if (prop.type.Equals("string"))
      {
         curValue = prop.stringValue;
         if (curValue.Equals(""))
         {
            curValue = kNoneOption;
         }
         else if (!animatorStateNames.Exists(x => x.Equals(curValue)))
         {
            animatorStateNames.Add("<ERROR!>");
            curValue = animatorStateNames[animatorStateNames.Count - 1];
         }
      }


      int curIdx = animatorStateNames.IndexOf(curValue);

      EditorGUI.BeginChangeCheck();
      PropertyDrawerUtl.BoldForPrefabOverride(prop); //make sure we bold the property name if overridden on prefab
      int selectedIdx = EditorGUI.Popup(pos, label.text, curIdx, animatorStateNames.ToArray());
      if (EditorGUI.EndChangeCheck())
      {
         prop.stringValue = (!animatorStateNames[selectedIdx].Equals(kErrorName) && !animatorStateNames[selectedIdx].Equals(kNoneOption)) ? animatorStateNames[selectedIdx] : "";
         EditorUtility.SetDirty(prop.serializedObject.targetObject);
      }
   }
}