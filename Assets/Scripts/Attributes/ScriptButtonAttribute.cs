//
// An attribute that draws a button for a dummy property, which can call a public method on the associated script class
// The method must have this signature:
//  public void MethodName(string propertyPath) { }
//

using UnityEngine;

public class ScriptButtonAttribute : PropertyAttribute
{
   //method name to call on the associated script
   public readonly string MethodNameToCall;
   public readonly string ButtonLabel;
   public ScriptButtonAttribute(string buttonLabel, string methodName)
   {
      this.ButtonLabel = buttonLabel;
      this.MethodNameToCall = methodName;
   }
}