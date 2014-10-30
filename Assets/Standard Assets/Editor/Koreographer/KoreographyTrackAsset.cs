//----------------------------------------------
//            	   Koreographer                 
//      Copyright © 2014 Sonic Bloom, LLC      
//----------------------------------------------

using UnityEditor;

public class KoreographyTrackAsset
{
	[MenuItem("Assets/Create/Koreography Track")]
	public static void CreateAsset()
	{
		CustomAssetUtility.CreateAsset<KoreographyTrack>();
	}
}
