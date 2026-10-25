using UnityEditor;
using UnityEngine;
using FMODUnity;

namespace Depra.Sound.FMOD.Editor
{
	[CustomPropertyDrawer(typeof(FMODAudioEventDescription))]
	public sealed class FMODAudioEventDescriptionDrawer : PropertyDrawer
	{
		private const float DETAILS_BUTTON_WIDTH = 72f;
		private const float EVENT_LABEL_WIDTH = 52f;
		private const float EVENT_ACTION_BUTTON_WIDTH = 24f;

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			EditorGUI.BeginProperty(position, label, property);

			var clip = property.FindPropertyRelative("_clip")?.FindPropertyRelative("_event");
			var firstLineHeight = EditorGUIUtility.singleLineHeight;
			var eventLabelRect = new Rect(position.x, position.y, EVENT_LABEL_WIDTH, firstLineHeight);
			var badgeWidth = 20f;
			var buttonWidth = DETAILS_BUTTON_WIDTH;
			var buttonRect = new Rect(position.xMax - buttonWidth, position.y, buttonWidth, firstLineHeight);
			var badgeRect = new Rect(buttonRect.x - badgeWidth - 4f, position.y, badgeWidth, firstLineHeight);
			var fieldRect = new Rect(eventLabelRect.xMax, position.y,
				badgeRect.x - eventLabelRect.xMax - 4f, firstLineHeight);

			EditorGUI.LabelField(eventLabelRect, "Event");

			if (clip != null)
			{
				DrawEventReferenceWithoutFoldout(fieldRect, clip);
			}
			else
			{
				EditorGUI.LabelField(fieldRect, "FMOD Event is missing");
			}

			var is3D = property.FindPropertyRelative("_is3D")?.boolValue ?? false;
			DrawSpatialBadge(badgeRect, is3D);

			if (GUI.Button(buttonRect, "Details"))
			{
				PopupWindow.Show(buttonRect, new DetailsPopup(property.serializedObject, property.propertyPath));
			}

			var y = fieldRect.yMax + 2f;
			var x = position.x;
			var width = position.width;

			var parameters = property.FindPropertyRelative("_parameters");
			var exportedHeight = GetExportedParametersHeight(parameters);
			if (exportedHeight > 0f)
			{
				DrawExportedParameters(new Rect(x, y, width, exportedHeight), parameters);
				y += exportedHeight + 2f;
			}

			var contractParameters = property.FindPropertyRelative("_contractParameters");
			if (contractParameters != null)
			{
				var contractRect = new Rect(x, y, width, EditorGUI.GetPropertyHeight(contractParameters, true));
				EditorGUI.PropertyField(contractRect, contractParameters,
					new GUIContent("Additional Contract Parameters"), true);
			}

			EditorGUI.EndProperty();
		}

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			var line = EditorGUIUtility.singleLineHeight;
			var height = line;

			var parameters = property.FindPropertyRelative("_parameters");
			var exportedHeight = GetExportedParametersHeight(parameters);
			if (exportedHeight > 0f)
			{
				height += 2f + exportedHeight;
			}

			var contractParameters = property.FindPropertyRelative("_contractParameters");
			if (contractParameters != null)
			{
				height += 2f + EditorGUI.GetPropertyHeight(contractParameters, true);
			}

			return height;
		}

		private static void ReadTitleAndGuid(SerializedProperty property, out string title, out global::FMOD.GUID guidValue)
		{
			title = "FMOD Event";
			guidValue = default;

			var clip = property.FindPropertyRelative("_clip")?.FindPropertyRelative("_event");
			if (clip == null)
			{
				return;
			}

			var path = clip.FindPropertyRelative("Path");
			var guid = clip.FindPropertyRelative("Guid");
			if (guid == null)
			{
				return;
			}

			guidValue = new global::FMOD.GUID
			{
				Data1 = guid.FindPropertyRelative("Data1").intValue,
				Data2 = guid.FindPropertyRelative("Data2").intValue,
				Data3 = guid.FindPropertyRelative("Data3").intValue,
				Data4 = guid.FindPropertyRelative("Data4").intValue
			};

			if (path != null && !string.IsNullOrEmpty(path.stringValue))
			{
				title = path.stringValue;
			}
		}

		private static void DrawEventReferenceWithoutFoldout(Rect rect, SerializedProperty eventReference)
		{
			var path = eventReference.FindPropertyRelative("Path");
			if (path == null)
			{
				EditorGUI.LabelField(rect, "Path field is missing");
				return;
			}

			var openRect = new Rect(rect.xMax - EVENT_ACTION_BUTTON_WIDTH, rect.y,
				EVENT_ACTION_BUTTON_WIDTH, rect.height);
			var browseRect = new Rect(openRect.x - EVENT_ACTION_BUTTON_WIDTH - 2f, rect.y,
				EVENT_ACTION_BUTTON_WIDTH, rect.height);
			var pathRect = new Rect(rect.x, rect.y, browseRect.x - rect.x - 2f, rect.height);

			using (var scope = new EditorGUI.ChangeCheckScope())
			{
				var newPath = EditorGUI.TextField(pathRect, path.stringValue);
				if (scope.changed)
				{
					SetEventReferenceFromPath(eventReference, newPath);
				}
			}

			if (GUI.Button(browseRect, "..."))
			{
				var eventBrowser = ScriptableObject.CreateInstance<EventBrowser>();
				eventBrowser.ChooseEvent(eventReference);
				var windowRect = rect;
				windowRect.xMin = pathRect.xMin;
				windowRect.position = GUIUtility.GUIToScreenPoint(windowRect.position);
				windowRect.height = rect.height + 1f;
				windowRect.width = Mathf.Max(windowRect.width, 300f);
				eventBrowser.ShowAsDropDown(windowRect, new Vector2(windowRect.width, 400f));
			}

			if (GUI.Button(openRect, "O"))
			{
				EventBrowser.ShowWindow();
				var browser = EditorWindow.GetWindow<EventBrowser>();
				browser.FrameEvent(path.stringValue);
			}
		}

		private static void SetEventReferenceFromPath(SerializedProperty eventReference, string path)
		{
			var eventRef = EventManager.EventFromPath(path);
			if (eventRef != null)
			{
				eventReference.SetEventReference(eventRef.Guid, eventRef.Path);
			}
			else
			{
				eventReference.SetEventReference(new global::FMOD.GUID(), path);
			}
		}

		private static float GetDetailsHeight() => EditorGUIUtility.singleLineHeight * 3f + 8f;
		
		private static void DrawDetailsContent(Rect rect, SerializedProperty property)
		{
			ReadTitleAndGuid(property, out var title, out var guidValue);
			var line = EditorGUIUtility.singleLineHeight;
			var x = rect.x;
			var width = rect.width;
			var y = rect.y;

			var pathRect = new Rect(x, y, width, line);
			EditorGUI.LabelField(pathRect, "Path", title);
			y = pathRect.yMax + 2f;

			var idRect = new Rect(x, y, width, line);
			EditorGUI.LabelField(idRect, "Id", guidValue.ToString());
			y = idRect.yMax + 2f;

			var spatialRect = new Rect(x, y, width, line);
			EditorGUI.LabelField(spatialRect, "Spatialization",
				property.FindPropertyRelative("_is3D")?.boolValue == true ? "3D" : "2D");
		}

		private sealed class DetailsPopup : PopupWindowContent
		{
			private readonly SerializedObject _serializedObject;
			private readonly string _propertyPath;
			private Vector2 _scroll;

			public DetailsPopup(SerializedObject serializedObject, string propertyPath)
			{
				_serializedObject = serializedObject;
				_propertyPath = propertyPath;
			}

			public override Vector2 GetWindowSize()
			{
				var height = GetDetailsHeight() + 24f;
				return new Vector2(560f, height);
			}

			public override void OnGUI(Rect rect)
			{
				_serializedObject.Update();
				var property = _serializedObject.FindProperty(_propertyPath);
				if (property == null)
				{
					EditorGUI.LabelField(rect, "Event data is unavailable.");
					_serializedObject.ApplyModifiedProperties();
					return;
				}

				var contentHeight = GetDetailsHeight();
				var viewRect = new Rect(8f, 8f, rect.width - 24f, contentHeight);
				_scroll = GUI.BeginScrollView(rect, _scroll,
					new Rect(0f, 0f, viewRect.xMax + 8f, viewRect.yMax + 8f));
				DrawDetailsContent(viewRect, property);
				GUI.EndScrollView();

				_serializedObject.ApplyModifiedProperties();
			}
		}

		private static float GetExportedParametersHeight(SerializedProperty array)
		{
			if (array == null || array.arraySize == 0)
			{
				return 0f;
			}

			var line = EditorGUIUtility.singleLineHeight;
			return Mathf.Max(line, array.arraySize * (line + 2f));
		}

		private static void DrawExportedParameters(Rect rect, SerializedProperty array)
		{
			if (array == null || array.arraySize == 0)
			{
				return;
			}

			var line = EditorGUIUtility.singleLineHeight;
			var y = rect.y;
			for (var index = 0; index < array.arraySize; index++)
			{
				var element = array.GetArrayElementAtIndex(index);
				var rowRect = new Rect(rect.x, y, rect.width, line);
				DrawExportedRow(rowRect, element);
				y += line + 2f;
			}
		}

		private static void DrawExportedRow(Rect rect, SerializedProperty element)
		{
			var enabled = element.FindPropertyRelative("Enabled");
			var name = element.FindPropertyRelative("Name");
			var value = element.FindPropertyRelative("Value");
			var minimum = element.FindPropertyRelative("Minimum");
			var maximum = element.FindPropertyRelative("Maximum");
			var type = element.FindPropertyRelative("Type");
			var labels = element.FindPropertyRelative("Labels");
			var supported = element.FindPropertyRelative("IsSupported").boolValue;

			var x = rect.x;
			var toggleRect = new Rect(x, rect.y, 18f, rect.height);
			enabled.boolValue = EditorGUI.Toggle(toggleRect, enabled.boolValue);
			x = toggleRect.xMax + 4f;

			var nameRect = new Rect(x, rect.y, Mathf.Min(150f, rect.width * 0.34f), rect.height);
			EditorGUI.LabelField(nameRect, name.stringValue, EditorStyles.label);
			x = nameRect.xMax + 4f;

			var valueRect = new Rect(x, rect.y, rect.xMax - x, rect.height);
			using (new EditorGUI.DisabledScope(!enabled.boolValue || !supported))
			{
				if (!supported)
				{
					EditorGUI.LabelField(valueRect, "Unsupported", EditorStyles.miniLabel);
					return;
				}

				switch ((FMODAudioParamOverride.ParamType)type.enumValueIndex)
				{
					case FMODAudioParamOverride.ParamType.LABELED:
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
						break;
					}
					case FMODAudioParamOverride.ParamType.DISCRETE:
						value.floatValue = EditorGUI.IntSlider(valueRect, (int)value.floatValue,
							(int)minimum.floatValue, (int)maximum.floatValue);
						break;
					default:
						value.floatValue = EditorGUI.Slider(valueRect, value.floatValue,
							minimum.floatValue, maximum.floatValue);
						break;
				}
			}
		}

		private static void DrawSpatialBadge(Rect rect, bool is3D)
		{
			if (!is3D)
			{
				GUI.Label(rect, new GUIContent(EditorIcons.BADGE_2D, "2D non-spatial event"));
			}
		}
	}
}
