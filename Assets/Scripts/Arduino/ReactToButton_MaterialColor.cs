using UnityEngine;
using System.Collections;

public class ReactToButton_MaterialColor : MonoBehaviour {

   public ButtonPress Button;
   public Color PressedColor = Color.green;
   public Color NotPressedColor = Color.white;

	// Update is called once per frame
   void Update()
   {
      if ((Button != null) && (renderer != null))
      {
         if (Button.ButtonPressed)
            renderer.material.color = PressedColor;
         else
            renderer.material.color = NotPressedColor;
      }
   }
}
