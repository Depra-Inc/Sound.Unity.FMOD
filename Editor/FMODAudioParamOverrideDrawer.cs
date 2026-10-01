using UnityEditor;
using UnityEngine;

namespace Depra.Sound.FMOD.Editor
{
	[CustomPropertyDrawer(typeof(FMODAudioParamOverride))]
	public sealed class FMODAudioParamOverrideDrawer : PropertyDrawer
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
			var type = property.FindPropertyRelative("Type");
			var labels = property.FindPropertyRelative("Labels");
			var supported = property.FindPropertyRelative("IsSupported").boolValue;
			var toggle = new Rect(position.x, position.y, 18f, position.height);
			var nameRect = new Rect(toggle.xMax + 2f, position.y, Mathf.Min(112f, position.width * 0.32f),
				position.height);
			var valueRect = new Rect(nameRect.xMax + 4f, position.y,
				position.xMax - nameRect.xMax - 4f, position.height);

			enabled.boolValue = EditorGUI.Toggle(toggle, enabled.boolValue);
			EditorGUI.LabelField(nameRect, name.stringValue, EditorStyles.label);
			using (new EditorGUI.DisabledScope(!enabled.boolValue || !supported))
			{
				if (!supported)
				{
					EditorGUI.LabelField(valueRect, "Unsupported", EditorStyles.miniLabel);
				}

				var paramType = (FMODAudioParamOverride.ParamType)type.enumValueIndex;
				if (paramType == FMODAudioParamOverride.ParamType.LABELED)
				{
					if (labels == null || labels.arraySize == 0)
					{
						EditorGUI.LabelField(valueRect, "No labels", EditorStyles.miniLabel);
						return;
					}

					var labelsArray = new string[labels.arraySize];
					for (var i = 0; i < labels.arraySize; i++)
					{
						labelsArray[i] = labels.GetArrayElementAtIndex(i).stringValue;
					}

					value.floatValue = EditorGUI.Popup(valueRect, (int)value.floatValue, labelsArray);
				}
				else if (paramType == FMODAudioParamOverride.ParamType.DISCRETE)
				{
					value.floatValue = EditorGUI.IntSlider(valueRect, (int)value.floatValue,
						(int)minimum.floatValue, (int)maximum.floatValue);
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