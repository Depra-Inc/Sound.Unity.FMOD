using Depra.Sound.Configuration;
using Depra.Sound.Editor;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Depra.Sound.FMOD.Editor
{
	[CustomEditor(typeof(FMODAudioBank))]
	internal sealed class FMODAudioBankEditor : UnityEditor.Editor, IAudioBankEmbeddedEditor
	{
		private ReorderableList _events;
		private ReorderableList _containers;
		private AudioProjectSettings _settings;

		public override void OnInspectorGUI() => DrawEmbedded(AudioProjectSettingsProvider.LoadTable());

		public void DrawEmbedded(AudioProjectSettings settings)
		{
			_settings = settings;
			serializedObject.Update();

			DrawImportToolbar();
			DrawSourceBankPath();
			DrawEventList();
			DrawContainerList();

			_events.DoList(GUILayoutUtility.GetRect(0f, _events.GetHeight(), GUILayout.ExpandWidth(true)));
			EditorGUILayout.Space(6f);
			_containers.DoList(GUILayoutUtility.GetRect(0f, _containers.GetHeight(), GUILayout.ExpandWidth(true)));
			serializedObject.ApplyModifiedProperties();
		}

		private void DrawImportToolbar()
		{
			using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
			{
				GUILayout.FlexibleSpace();
				if (GUILayout.Button("Import FMOD Events", EditorStyles.toolbarButton, GUILayout.Width(140f)))
				{
					EditorApplication.ExecuteMenuItem(FMODAudioBankImportWindow.MENU_PATH);
				}
			}
		}

		private void DrawSourceBankPath()
		{
			var sourcePath = serializedObject.FindProperty("_sourceBankPath");
			using (new EditorGUI.DisabledScope(true))
			{
				EditorGUILayout.PropertyField(sourcePath, new GUIContent("Source Bank Path"));
			}

			EditorGUILayout.Space(4f);
		}

		private void DrawEventList()
		{
			var entries = serializedObject.FindProperty("_events");
			_events = new ReorderableList(serializedObject, entries, false, true, false, true)
			{
				index = entries.arraySize > 0 ? 0 : -1,
				elementHeightCallback = index =>
					index >= entries.arraySize
						? EditorGUIUtility.singleLineHeight
						: GetEventHeight(entries.GetArrayElementAtIndex(index)),
				drawHeaderCallback = rect => EditorGUI.LabelField(rect, $"Events ({entries.arraySize})"),
				drawElementCallback = (rect, index, _, _) => DrawEventRow(rect, entries, index),
				onRemoveCallback = list =>
				{
					if (EditorUtility.DisplayDialog("Remove Event", "Remove the selected event?", "Remove", "Cancel"))
					{
						DeleteArrayElement(entries, list.index);
						list.index = Mathf.Min(list.index, entries.arraySize - 1);
					}
				}
			};
		}

		private void DrawContainerList()
		{
			var entries = serializedObject.FindProperty("_containers");
			_containers = new ReorderableList(serializedObject, entries, true, true, true, true)
			{
				index = entries.arraySize > 0 ? 0 : -1,
				elementHeightCallback = index => index >= entries.arraySize
					? EditorGUIUtility.singleLineHeight
					: GetContainerHeight(entries.GetArrayElementAtIndex(index)),
				drawHeaderCallback = rect => EditorGUI.LabelField(rect, $"Containers ({entries.arraySize})"),
				drawElementCallback = (rect, index, _, _) => DrawContainerRow(rect, entries, index),
				onAddCallback = list => AddContainer(entries, list),
				onRemoveCallback = list =>
				{
					if (EditorUtility.DisplayDialog("Remove Container", "Remove the selected container?", "Remove",
						    "Cancel"))
					{
						DeleteArrayElement(entries, list.index);
						list.index = Mathf.Min(list.index, entries.arraySize - 1);
					}
				}
			};
		}

		private void AddContainer(SerializedProperty entries, ReorderableList list)
		{
			if (!TryGetSettings("Cannot add container without project Audio Table."))
			{
				return;
			}

			var index = entries.arraySize;
			entries.InsertArrayElementAtIndex(index);
			var entry = entries.GetArrayElementAtIndex(index);
			entry.FindPropertyRelative(nameof(AudioContainerEntry.Name)).stringValue = $"Container {index + 1}";
			Undo.RecordObject(_settings, "Allocate audio container ID");
			var containerId = _settings.AllocateEventId();
			EditorUtility.SetDirty(_settings);
			SetId(entry.FindPropertyRelative(nameof(AudioContainerEntry.Id)), containerId);
			entry.FindPropertyRelative(nameof(AudioContainerEntry.Container)).objectReferenceValue = null;
			list.index = index;
		}

		private bool TryGetSettings(string error)
		{
			if (_settings)
			{
				return true;
			}

			Debug.LogError(error);
			return false;
		}

		private static float GetEventHeight(SerializedProperty entry)
		{
			var line = EditorGUIUtility.singleLineHeight;
			if (!entry.isExpanded)
			{
				return line + 6f;
			}

			var description = entry.FindPropertyRelative(nameof(FMODAudioBank.EventEntry.Description));
			var clip = description.FindPropertyRelative("_clip")?.FindPropertyRelative("_event");
			var parameters = description.FindPropertyRelative("_parameters");
			var eventHeight = EditorGUI.GetPropertyHeight(clip, new GUIContent("Event"));
			var parametersHeight = EditorGUI.GetPropertyHeight(parameters, new GUIContent("Static Parameters"), true);
			return line + 2f + line + 2f + eventHeight + 2 + parametersHeight + 8f;
		}

		private static float GetContainerHeight(SerializedProperty entry)
		{
			var line = EditorGUIUtility.singleLineHeight;
			if (!entry.isExpanded)
			{
				return line + 6f;
			}

			return line + 2f + line + 2f + line + 8f;
		}

		private static void DrawEventRow(Rect rect, SerializedProperty entries, int index)
		{
			if (index >= entries.arraySize)
			{
				return;
			}

			var entry = entries.GetArrayElementAtIndex(index);
			var name = entry.FindPropertyRelative(nameof(FMODAudioBank.EventEntry.Name));
			var id = entry.FindPropertyRelative(nameof(FMODAudioBank.EventEntry.Id));
			var header = new Rect(rect.x, rect.y + 2f, rect.width, EditorGUIUtility.singleLineHeight);
			var idValue = id.FindPropertyRelative("Value");
			entry.isExpanded = EditorGUI.Foldout(header, entry.isExpanded,
				$"{(string.IsNullOrWhiteSpace(name.stringValue) ? $"Event {index + 1}" : name.stringValue)}   (ID {idValue.ulongValue})",
				true);
			if (!entry.isExpanded)
			{
				return;
			}

			var y = header.yMax + 2f;
			var indent = rect.x + 14f;
			var width = rect.width - 14f;
			var nameRect = new Rect(indent, y, width, EditorGUIUtility.singleLineHeight);
			EditorGUI.PropertyField(nameRect, name);
			var description = entry.FindPropertyRelative(nameof(FMODAudioBank.EventEntry.Description));

			y = nameRect.yMax + 2f;
			var clip = description.FindPropertyRelative("_clip")?.FindPropertyRelative("_event");
			var eventContent = new GUIContent("Event");
			var eventHeight = EditorGUI.GetPropertyHeight(clip, eventContent, true);
			EditorGUI.PropertyField(new Rect(indent, y, width, eventHeight), clip, eventContent);

			var parameters = description.FindPropertyRelative("_parameters");
			y = y + eventHeight + 2f;
			EditorGUI.PropertyField(new Rect(indent, y, width, EditorGUI.GetPropertyHeight(parameters, true)),
				parameters, new GUIContent("Static Parameters"), true);
		}

		private static void DrawContainerRow(Rect rect, SerializedProperty entries, int index)
		{
			if (index >= entries.arraySize)
			{
				return;
			}

			var entry = entries.GetArrayElementAtIndex(index);
			var name = entry.FindPropertyRelative(nameof(AudioContainerEntry.Name));
			var id = entry.FindPropertyRelative(nameof(AudioContainerEntry.Id));
			var header = new Rect(rect.x, rect.y + 2f, rect.width, EditorGUIUtility.singleLineHeight);
			var idValue = id.FindPropertyRelative(nameof(AudioContainerEntry.Id.Value));
			entry.isExpanded = EditorGUI.Foldout(header, entry.isExpanded,
				$"{(string.IsNullOrWhiteSpace(name.stringValue) ? $"Container {index + 1}" : name.stringValue)}   (ID {idValue.ulongValue})",
				true);
			if (!entry.isExpanded)
			{
				return;
			}

			var y = header.yMax + 2f;
			var indent = rect.x + 14f;
			var width = rect.width - 14f;
			var nameRect = new Rect(indent, y, width, EditorGUIUtility.singleLineHeight);
			EditorGUI.PropertyField(nameRect, name);
			y = nameRect.yMax + 2f;
			var description = entry.FindPropertyRelative(nameof(AudioContainerEntry.Container));
			EditorGUI.ObjectField(new Rect(indent, y, width, EditorGUIUtility.singleLineHeight + 2f),
				description,
				typeof(AudioEventContainer), GUIContent.none);
		}

		private static void DeleteArrayElement(SerializedProperty array, int index)
		{
			var size = array.arraySize;
			array.DeleteArrayElementAtIndex(index);
			if (array.arraySize == size)
			{
				array.DeleteArrayElementAtIndex(index);
			}
		}

		private static void SetId(SerializedProperty id, ulong value) =>
			id.FindPropertyRelative("Value").ulongValue = value;
	}
}