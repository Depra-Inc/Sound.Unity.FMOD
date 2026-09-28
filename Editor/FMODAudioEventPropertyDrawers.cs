using UnityEditor;
using UnityEngine;

namespace Depra.Sound.FMOD.Editor
{
	[CustomPropertyDrawer(typeof(FMODAudioEventDescription))]
	public sealed class FMODAudioEventDescriptionDrawer : PropertyDrawer
	{
		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			if (!property.isExpanded) return EditorGUIUtility.singleLineHeight;
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
			var title = path == null || string.IsNullOrEmpty(path.stringValue)
				? "FMOD Event"
				: path.stringValue;

			var header = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
			property.isExpanded = EditorGUI.Foldout(header, property.isExpanded, $"{title} ({guidValue})", true);

			if (property.isExpanded)
			{
				var line = EditorGUIUtility.singleLineHeight;
				var x = position.x + 14f;
				var width = position.width - 14f;
				var pathRect = new Rect(x, header.yMax + 2f, width, line);
				EditorGUI.LabelField(pathRect, "FMOD Event", title);
				var idRect = new Rect(x, pathRect.yMax + 2f, width, line);
				EditorGUI.LabelField(idRect, "FMOD ID", guidValue.ToString());
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

	[CustomPropertyDrawer(typeof(FMODAudioParameterOverride))]
	public sealed class FMODAudioParameterOverrideDrawer : PropertyDrawer
	{
		public override float GetPropertyHeight(SerializedProperty property, GUIContent label) =>
			EditorGUIUtility.singleLineHeight;

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			var enabled = property.FindPropertyRelative("Enabled");
			var name = property.FindPropertyRelative("Name");
			var value = property.FindPropertyRelative("Value");
			var minimum = property.FindPropertyRelative("Minimum");
			var maximum = property.FindPropertyRelative("Maximum");
			var discrete = property.FindPropertyRelative("IsDiscrete").boolValue;
			var supported = property.FindPropertyRelative("IsSupported").boolValue;
			var toggle = new Rect(position.x, position.y, 18f, position.height);
			var nameRect = new Rect(toggle.xMax + 2f, position.y, Mathf.Min(112f, position.width * 0.32f),
				position.height);
			var valueRect = new Rect(nameRect.xMax + 4f, position.y,
				position.xMax - nameRect.xMax - 4f, position.height);

			using (new EditorGUI.DisabledScope(!supported))
			{
				enabled.boolValue = EditorGUI.Toggle(toggle, enabled.boolValue);
			}

			EditorGUI.LabelField(nameRect, name.stringValue, EditorStyles.miniLabel);
			using (new EditorGUI.DisabledScope(!enabled.boolValue || !supported))
			{
				if (!supported)
				{
					EditorGUI.LabelField(valueRect, "Unsupported", EditorStyles.miniLabel);
				}
				else if (discrete)
				{
					value.floatValue = EditorGUI.IntSlider(valueRect, Mathf.RoundToInt(value.floatValue),
						Mathf.CeilToInt(minimum.floatValue), Mathf.FloorToInt(maximum.floatValue));
				}
				else
				{
					value.floatValue = EditorGUI.Slider(valueRect, value.floatValue,
						minimum.floatValue, maximum.floatValue);
				}
			}
		}
	}
}