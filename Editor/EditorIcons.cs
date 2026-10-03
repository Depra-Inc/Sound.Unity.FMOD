using FMODUnity;
using UnityEditor;
using UnityEngine;

namespace Depra.Sound.FMOD.Editor
{
	internal static class EditorIcons
	{
		public static readonly Texture2D IMPORT = (Texture2D)EditorGUIUtility.Load("d_Import@2x");
		public static readonly Texture2D STUDIO = EditorUtils.LoadImage("StudioIcon.png");
		public static readonly Texture2D BADGE_2D = (Texture2D)EditorGUIUtility.Load("SceneView2D On@2x");
	}
}