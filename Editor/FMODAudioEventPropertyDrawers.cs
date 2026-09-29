using UnityEditor;
using UnityEngine;

namespace Depra.Sound.FMOD.Editor
{
	[CustomPropertyDrawer(typeof(FMODAudioEventDescription))]
	public sealed class FMODAudioEventDescriptionDrawer : PropertyDrawer
	{
		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			if (!property.isExpanded)
			{
				return EditorGUIUtility.singleLineHeight;
			}

			var parameters = property.FindPropertyRelative("_parameters");
			return EditorGUIUtility.singleLineHeight * 4f + EditorGUI.GetPropertyHeight(parameters, true) + 10f;
		}

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			EditorGUI.BeginProperty(position, label, property);
			var clip = property.FindPropertyRelative("_clip").FindPropertyRelative("_event");
			var path = clip.FindPropertyRelative("Path");
			var guid = clip.FindPropertyRelative("Guid");
			var guidValue = new global::FMOD.GUID
			{
				Data1 = guid.FindPropertyRelative("Data1").intValue,
				Data2 = guid.FindPropertyRelative("Data2").intValue,
				Data3 = guid.FindPropertyRelative("Data3").intValue,
				Data4 = guid.FindPropertyRelative("Data4").intValue
			};

			var title = path == null || string.IsNullOrEmpty(path.stringValue) ? "FMOD Event" : path.stringValue;
			var header = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
			property.isExpanded = EditorGUI.Foldout(header, property.isExpanded, $"{title} ({guidValue})", true);
			if (property.isExpanded)
			{
				var line = EditorGUIUtility.singleLineHeight;
				var x = position.x + 14f;
				var width = position.width - 14f;
				var pathRect = new Rect(x, header.yMax + 2f, width, line);
				EditorGUI.LabelField(pathRect, "Path", title);
				var idRect = new Rect(x, pathRect.yMax + 2f, width, line);
				EditorGUI.LabelField(idRect, "Id", guidValue.ToString());
				var spatialRect = new Rect(x, idRect.yMax + 2f, width, line);
				EditorGUI.LabelField(spatialRect, "Spatialization",
					property.FindPropertyRelative("_is3D").boolValue ? "3D" : "2D");
				var parameters = property.FindPropertyRelative("_parameters");
				var body = new Rect(x, spatialRect.yMax + 2f, width,
					EditorGUI.GetPropertyHeight(parameters, true));
				EditorGUI.PropertyField(body, parameters, new GUIContent("Static Parameters"), true);
			}

			EditorGUI.EndProperty();
		}
	}
}