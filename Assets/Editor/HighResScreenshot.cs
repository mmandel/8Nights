//
//  Quick way to take a high res screenshot
//

using UnityEngine;
using UnityEditor;
using System.Collections;

public class HighResScreenshot : ScriptableWizard
{
   [Range(1, 12)]
   public int ResolutionMult = 4;
   public string SaveLocation = "C:/Temp/UnityScreenshot.png";

   static int LastResolutionMult = -1;

   [MenuItem("Tools/High-res Screenshot")]
   static void CreateWizard()
   {
      HighResScreenshot s = ScriptableWizard.DisplayWizard<HighResScreenshot>("High-res Screenshot", "Take Screenshot!");

      //intialize to same value as used in last screenshot
      if(LastResolutionMult != -1)
         s.ResolutionMult = LastResolutionMult;
   }

   void OnWizardCreate()
   {
      //screenshots dont work with rift cam on...
      if (CameraMgr.Instance != null)
         CameraMgr.Instance.ActivateRiftCam(false);

      if (EightNightsAudioMgr.Instance != null)
         EightNightsAudioMgr.Instance.ShowTestUI = false;      

      Application.CaptureScreenshot(SaveLocation, ResolutionMult);

      LastResolutionMult = ResolutionMult;
   }
}