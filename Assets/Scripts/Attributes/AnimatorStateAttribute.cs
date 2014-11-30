//
//  Attribute used to create a custom editor drop down for animator state names on an int property
//  See AnimatorStatePropertyDrawer for the UI code...
//

using UnityEngine;

public class AnimatorStateAttribute : PropertyAttribute
{
   //the property name to nab the layer # we check for animator states
   public readonly string LayerPropName;
   public AnimatorStateAttribute(string layerPropName)
   {
      this.LayerPropName = layerPropName;
   }
}