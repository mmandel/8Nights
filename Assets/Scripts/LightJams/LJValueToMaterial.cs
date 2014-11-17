//
//  Just copies a LightJamValue on this game object to the material color of the renderer on the same game object
//

using UnityEngine;
using System.Collections;

public class LJValueToMaterial : MonoBehaviour {

   private LightJamsValue _lj;
   private Renderer _rend;

	// Use this for initialization
	void Start () {
      _rend = this.renderer;
      _lj = gameObject.GetComponent<LightJamsValue>();
	}
	
	// Update is called once per frame
	void Update () {
      if ((_rend == null) || (_lj == null))
         return;

      _rend.material.color = new Color(_lj.Value, 0.0f, 0.0f);
	}
}
