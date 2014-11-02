using UnityEngine;
using System.Collections;

public class ReactToButton_TriggerMusic : MonoBehaviour
{

   public ButtonPress StartButton;
   public ButtonPress StopButton;
   public SimpleMusicPlayer MusicPlayer;

   bool _prevStartState = false;
   bool _prevStopState = false;

   // Update is called once per frame
   void Update()
   {
      //restart music when button is pressed
      if ((_prevStartState != StartButton.ButtonPressed) && StartButton.ButtonPressed)
      {
         MusicPlayer.Stop();
         MusicPlayer.Play();
      }

      //stop music
      if ((_prevStopState != StopButton.ButtonPressed) && StopButton.ButtonPressed)
      {
         MusicPlayer.Stop();
      }

      _prevStartState = StartButton.ButtonPressed;
      _prevStopState = StopButton.ButtonPressed;
   }
}
