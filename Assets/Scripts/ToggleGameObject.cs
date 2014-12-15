//
// Toggle game object on/off with a keyboard cheat
//

using UnityEngine;
using System.Collections;

public class ToggleGameObject : MonoBehaviour 
{

   public KeyCode ToggleCheat = KeyCode.K;
   public bool StartEnabled = true;
   public GameObject ObjToToggle;

   bool _isEnabled;

	// Use this for initialization
	void Start () 
   {
      SetEnabled(StartEnabled, true);
	}

   void SetEnabled(bool e, bool force = false)
   {
      if ((_isEnabled != e) || force)
      {
         _isEnabled = e;
         if (ObjToToggle != null)
            ObjToToggle.SetActive(_isEnabled);
      }
   }

   void Update()
   {
      if (Input.GetKeyDown(ToggleCheat))
         SetEnabled(!_isEnabled);
   }
}
