using System;
using System.Linq;
using Depra.Sound.Unity.Editor;
using FMODUnity;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using GUID = FMOD.GUID;

namespace Depra.Sound.FMOD.Editor
{
	[CustomEditor(typeof(FMODAudioBank))]
	internal sealed class FMODAudioBankEditor : UnityEditor.Editor, IAudioBankEmbeddedEditor
	{
		private const float ACTION_BUTTON_WIDTH = 168f;
		private const float EVENT_ACTION_BUTTON_WIDTH = 82f;

		private ReorderableList _events;
		private ReorderableList _containers;
		private AudioProjectSettings _settings;
		private GUIContent _importButtonContent;
		private GUIContent _sourceBankPathContent;
		private string _eventSearch = string.Empty;

		private void OnEnable()
		{
			_sourceBankPathContent = new GUIContent("Source Bank Path", EditorIcons.STUDIO);
			_importButtonContent = new GUIContent(" Import FMOD Events", EditorIcons.IMPORT,
				"Import events from FMOD Studio");
		}

		public override void OnInspectorGUI() => DrawEmbedded(AudioProjectSettingsProvider.LoadTable(), string.Empty);

		public void DrawEmbedded(AudioProjectSettings settings, string eventSearch = "")
		{
			_settings = settings;
			_eventSearch = eventSearch ?? string.Empty;
			serializedObject.Update();

			DrawImportToolbar();
			DrawBankMetadata();
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
				EditorGUILayout.LabelField(
					"This bank is generated from FMOD Bank. Reimport the bank to update events and containers.",
					EditorStyles.helpBox, GUILayout.MaxWidth(800f), GUILayout.Height(16f));

				GUILayout.FlexibleSpace();
				if (GUILayout.Button(_importButtonContent, EditorStyles.toolbarButton, GUILayout.Width(160f)))
				{
					FMODAudioBankImportWindow.Open(target as FMODAudioBank);
				}
			}
		}

		private void DrawBankMetadata()
		{
			var sourcePath = serializedObject.FindProperty("_metadata");
			using (new EditorGUI.DisabledScope(true))
			{
				EditorGUILayout.BeginHorizontal();
				EditorGUILayout.PropertyField(sourcePath.FindPropertyRelative("BankPath"), _sourceBankPathContent);
				var sizeProperty = sourcePath.FindPropertyRelative("TotalSize");
				var sizeValue = sizeProperty.longValue;
				var formattedSize = FormatSize(sizeValue);
				EditorGUILayout.LabelField($"File Size: {formattedSize}", GUILayout.Width(150f));
				EditorGUILayout.EndHorizontal();
			}

			EditorGUILayout.Space(4f);
		}

		private void DrawEventList()
		{
			if (_events != null && _events.serializedProperty.serializedObject.targetObject == serializedObject.targetObject)
			{
				return;
			}

			var entries = serializedObject.FindProperty("_events");
			_events = new ReorderableList(serializedObject, entries, false, true, false, true)
			{
				elementHeightCallback = index =>
					index >= entries.arraySize
						? EditorGUIUtility.singleLineHeight
						: MatchesEventSearch(entries.GetArrayElementAtIndex(index))
							? GetEventHeight(entries.GetArrayElementAtIndex(index))
							: 0f,
				drawHeaderCallback = rect =>
				{
					var matched = 0;
					for (var index = 0; index < entries.arraySize; index++)
					{
						if (MatchesEventSearch(entries.GetArrayElementAtIndex(index)))
						{
							matched++;
						}
					}

					EditorGUI.LabelField(rect, $"Events ({matched}/{entries.arraySize})");
				},
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
			if (_containers != null && _containers.serializedProperty.serializedObject.targetObject == serializedObject.targetObject)
			{
				return;
			}

			var entries = serializedObject.FindProperty("_containers");
			_containers = new ReorderableList(serializedObject, entries, true, true, true, true)
			{
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
			if (clip?.boxedValue == null)
			{
				return line + 6f;
			}

			var eventHeight = EditorGUI.GetPropertyHeight(clip, new GUIContent("Event"));
			var parameters = description.FindPropertyRelative("_parameters");
			if (parameters == null || parameters.arraySize == 0)
			{
				return line + 2f + line + 2f + eventHeight + 8f;
			}

			var parametersHeight = GetStaticParametersHeight(parameters);
			return line + 2f + line + 2f + eventHeight + 2f + parametersHeight + 8f;
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

		private void DrawEventRow(Rect rect, SerializedProperty entries, int index)
		{
			if (index >= entries.arraySize)
			{
				return;
			}

			var entry = entries.GetArrayElementAtIndex(index);
			if (!MatchesEventSearch(entry))
			{
				return;
			}

			var eventName = entry.FindPropertyRelative(nameof(FMODAudioBank.EventEntry.Name));
			var id = entry.FindPropertyRelative(nameof(FMODAudioBank.EventEntry.Id));
			var header = new Rect(rect.x, rect.y + 2f, rect.width - ACTION_BUTTON_WIDTH - 4f,
				EditorGUIUtility.singleLineHeight);
			var idValue = id.FindPropertyRelative("Value");

			entry.isExpanded = EditorGUI.Foldout(header, entry.isExpanded,
				$"{(string.IsNullOrWhiteSpace(eventName.stringValue) ? $"Event {index + 1}" : eventName.stringValue)}   (ID {idValue.ulongValue})",
				true);

			if (!entry.isExpanded)
			{
				return;
			}

			var reimportRect = new Rect(rect.xMax - ACTION_BUTTON_WIDTH, rect.y + 2f, EVENT_ACTION_BUTTON_WIDTH,
				EditorGUIUtility.singleLineHeight);
			if (GUI.Button(reimportRect, "Reimport"))
			{
				ReimportEvent(entries, index);
				return;
			}

			var duplicateRect = new Rect(reimportRect.xMax + 4f, rect.y + 2f, EVENT_ACTION_BUTTON_WIDTH,
				EditorGUIUtility.singleLineHeight);
			if (GUI.Button(duplicateRect, "Duplicate"))
			{
				DuplicateEvent(entries, index);
				return;
			}

			var y = header.yMax + 2f;
			var indent = rect.x + 14f;
			var width = rect.width - 14f;
			var nameRect = new Rect(indent, y, width, EditorGUIUtility.singleLineHeight);
			EditorGUI.PropertyField(nameRect, eventName);
			var description = entry.FindPropertyRelative(nameof(FMODAudioBank.EventEntry.Description));

			y = nameRect.yMax + 2f;
			var clip = description.FindPropertyRelative("_clip")?.FindPropertyRelative("_event");
			if (clip?.boxedValue == null)
			{
				return;
			}

			var eventContent = new GUIContent("Event");
			var eventHeight = EditorGUI.GetPropertyHeight(clip, eventContent, true);
			EditorGUI.PropertyField(new Rect(indent, y, width, eventHeight), clip, eventContent);

			var parameters = description.FindPropertyRelative("_parameters");
			if (parameters is { arraySize: > 0 })
			{
				y = y + eventHeight + 2f;
				var parametersRect = new Rect(indent, y, width, GetStaticParametersHeight(parameters));
				DrawStaticParameters(parametersRect, parameters, new GUIContent("Required Parameters"));
			}
		}

		private void DuplicateEvent(SerializedProperty entries, int index)
		{
			if (!TryGetSettings("Cannot duplicate event without project Audio Table."))
			{
				return;
			}

			entries.InsertArrayElementAtIndex(index);
			entries.MoveArrayElement(index, index + 1);
			var source = entries.GetArrayElementAtIndex(index);
			var insertIndex = index + 1;
			var duplicated = entries.GetArrayElementAtIndex(insertIndex);

			var sourceName = source.FindPropertyRelative(nameof(FMODAudioBank.EventEntry.Name)).stringValue;
			duplicated.FindPropertyRelative(nameof(FMODAudioBank.EventEntry.Name)).stringValue =
				string.IsNullOrWhiteSpace(sourceName) ? $"Event {insertIndex + 1}" : $"{sourceName}_copy";

			Undo.RecordObject(_settings, "Allocate FMOD alias event ID");
			var newId = _settings.AllocateEventId();
			EditorUtility.SetDirty(_settings);
			SetId(duplicated.FindPropertyRelative(nameof(FMODAudioBank.EventEntry.Id)), newId);

			serializedObject.ApplyModifiedProperties();
			serializedObject.Update();
			_events.index = insertIndex;
		}

		private void ReimportEvent(SerializedProperty entries, int index)
		{
			if (!TryGetSettings("Cannot reimport event without project Audio Table."))
			{
				return;
			}

			if (target is not FMODAudioBank bank)
			{
				return;
			}

			var entry = entries.GetArrayElementAtIndex(index);
			var eventName = entry.FindPropertyRelative(nameof(FMODAudioBank.EventEntry.Name)).stringValue;
			var description = entry.FindPropertyRelative(nameof(FMODAudioBank.EventEntry.Description));
			var clip = description.FindPropertyRelative("_clip")?.FindPropertyRelative("_event");
			if (clip == null)
			{
				Debug.LogError($"Cannot reimport '{eventName}': missing FMOD event reference.");
				return;
			}

			if (!TryGetGuid(clip, out var guid))
			{
				Debug.LogError($"Cannot reimport '{eventName}': failed to read FMOD event GUID.");
				return;
			}

			var path = clip.FindPropertyRelative("Path")?.stringValue;
			EventManager.RefreshBanks();
			var importedEvent = EventManager.Events?.FirstOrDefault(x => x.Guid.Equals(guid)) ??
			                    EventManager.Events?.FirstOrDefault(x =>
				                    !string.IsNullOrEmpty(path) &&
				                    string.Equals(x.Path, path, StringComparison.Ordinal));

			if (importedEvent == null)
			{
				Debug.LogWarning($"Cannot reimport '{eventName}': event was not found in FMOD cache.");
				return;
			}

			var metadata = ReadBankMetadata();
			Undo.RecordObject(_settings, "Reimport FMOD event");
			Undo.RecordObject(bank, "Reimport FMOD event");
			bank.Import(metadata, new[] { FMODAudioBankImportWindow.CreateImportedEvent(importedEvent, _settings) },
				_settings);
			EditorUtility.SetDirty(bank);

			if (!_settings.Banks.Contains(bank))
			{
				_settings.Banks.Add(bank);
				EditorUtility.SetDirty(_settings);
			}

			serializedObject.ApplyModifiedProperties();
			serializedObject.Update();
			AssetDatabase.SaveAssets();
		}

		private FMODBankMetadata ReadBankMetadata()
		{
			var metadata = serializedObject.FindProperty("_metadata");
			return new FMODBankMetadata
			{
				BankPath = metadata.FindPropertyRelative("BankPath").stringValue,
				TotalSize = metadata.FindPropertyRelative("TotalSize").longValue
			};
		}

		private static bool TryGetGuid(SerializedProperty eventProperty, out GUID guid)
		{
			guid = default;
			var guidProperty = eventProperty.FindPropertyRelative("Guid");
			if (guidProperty == null)
			{
				return false;
			}

			guid = new GUID
			{
				Data1 = guidProperty.FindPropertyRelative("Data1").intValue,
				Data2 = guidProperty.FindPropertyRelative("Data2").intValue,
				Data3 = guidProperty.FindPropertyRelative("Data3").intValue,
				Data4 = guidProperty.FindPropertyRelative("Data4").intValue
			};

			return true;
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

		private bool MatchesEventSearch(SerializedProperty eventEntry)
		{
			if (string.IsNullOrWhiteSpace(_eventSearch))
			{
				return true;
			}

			var search = _eventSearch.Trim();
			var eventName = eventEntry.FindPropertyRelative(nameof(FMODAudioBank.EventEntry.Name))?.stringValue;
			if (!string.IsNullOrEmpty(eventName) && eventName.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0)
			{
				return true;
			}

			var path = eventEntry.FindPropertyRelative(nameof(FMODAudioBank.EventEntry.Description))
				?.FindPropertyRelative("_clip")
				?.FindPropertyRelative("_event")
				?.FindPropertyRelative("Path")
				?.stringValue;

			return !string.IsNullOrEmpty(path) &&
			       path.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0;
		}

		private static readonly string[] SIZE_SUFFIX = { "B", "KB", "MB", "GB" };

		private static string FormatSize(long bytes)
		{
			var order = 0;
			double size = bytes;
			while (size >= 1024 && order + 1 < SIZE_SUFFIX.Length)
			{
				order++;
				size /= 1024;
			}

			return $"{size:0.##} {SIZE_SUFFIX[order]}";
		}

		private static float GetStaticParametersHeight(SerializedProperty array)
		{
			var line = EditorGUIUtility.singleLineHeight;
			var height = line;
			if (!array.isExpanded)
			{
				return height;
			}

			for (var index = 0; index < array.arraySize; index++)
			{
				var element = array.GetArrayElementAtIndex(index);
				height += EditorGUI.GetPropertyHeight(element, true) + 2f;
			}

			return height;
		}

		private static void DrawStaticParameters(Rect rect, SerializedProperty array, GUIContent label)
		{
			var line = EditorGUIUtility.singleLineHeight;
			var y = rect.y;

			var headerRect = new Rect(rect.x, y, rect.width, line);
			array.isExpanded = EditorGUI.Foldout(headerRect, array.isExpanded, label, true);
			if (!array.isExpanded)
			{
				return;
			}

			y += line + 2f;
			var oldIndent = EditorGUI.indentLevel;
			EditorGUI.indentLevel++;

			for (var index = 0; index < array.arraySize; index++)
			{
				var element = array.GetArrayElementAtIndex(index);
				var elementHeight = EditorGUI.GetPropertyHeight(element, true);
				var elementRect = new Rect(rect.x, y, rect.width, elementHeight);
				EditorGUI.PropertyField(elementRect, element, new GUIContent($"Element {index + 1}"), true);
				y += elementHeight + 2f;
			}

			EditorGUI.indentLevel = oldIndent;
		}
	}
}