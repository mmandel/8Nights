//----------------------------------------------
//            	   Koreographer                 
//      Copyright © 2014 Sonic Bloom, LLC      
//----------------------------------------------

using UnityEditor;

public class KoreographyAsset
{
	[MenuItem("Assets/Create/Koreography")]
	public static void CreateAsset()
	{
		CustomAssetUtility.CreateAsset<Koreography>();
	}
}
