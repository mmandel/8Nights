//
//  Takes the state of any 8Nights light and mirrors it to a material for visualization / testing
//

using UnityEngine;
using System.Collections;

public class MirrorLightOnMaterial : MonoBehaviour {

   public Renderer RendererWithMat;
   public EightNightsMgr.GroupID Group;
   public EightNightsMgr.LightID Light;


	void Start () 
   {
      EightNightsMgr.Instance.OnLightChanged += OnLightChanged;
	}

   void OnLightChanged(object sender, EightNightsMgr.LightEventArgs e)
   {
      if ((e.Group == Group) && (e.Light == Light))
      {
         if (RendererWithMat != null)
         {
            RendererWithMat.material.color = Color.Lerp(Color.grey, e.Data.LightColor, e.Data.LightIntensity);
         }
      }
   }
}
