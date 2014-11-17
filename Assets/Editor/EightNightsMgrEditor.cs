//
// Editor for EightNightsMgr script
//

using UnityEngine;
using UnityEditor;
using System.Collections;

[CustomEditor(typeof(EightNightsMgr))]
public class EightNightsMgrEditor : Editor 
{
   public override void OnInspectorGUI()
   {
      //make sure the string labels are filled in for each world, so the print nice
      EightNightsMgr mgr = ((EightNightsMgr)target);
      for (int i = 0; i < mgr.LightGroups.Length; i++)
      {
         EightNightsMgr.LightGroupConfig c = mgr.LightGroups[i];
         c.GroupName = c.Group.ToString();
      }

      base.OnInspectorGUI();
   }

}
