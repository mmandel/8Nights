//
// Deals with triggering button sounds for each group
//

using UnityEngine;
using System.Collections;

public class ButtonSoundMgr : MonoBehaviour 
{
   public ButtonConfig[] ButtonConfigs;

   [System.Serializable]
   public class ButtonConfig
   {
      public EightNightsMgr.GroupID Group;
      public SimpleMusicPlayer MusicPlayer;
   }

   public void TriggerSoundForGroup(EightNightsMgr.GroupID group)
   {
      ButtonConfig c = FindButtonConfig(group);
      if ((c != null) && (c.MusicPlayer != null))
      {

         //TODO: should fade things nicely and try to align the "attack moment" with a downbeat...
         c.MusicPlayer.Stop();
         c.MusicPlayer.Play();
      }
   }

   ButtonConfig FindButtonConfig(EightNightsMgr.GroupID group)
   {
      foreach (ButtonConfig c in ButtonConfigs)
      {
         if (c.Group == group)
            return c;
      }
      return null;
   }
}
