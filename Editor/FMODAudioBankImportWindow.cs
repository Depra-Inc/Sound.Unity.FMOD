using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Depra.Sound.Editor;
using FMODUnity;
using UnityEditor;
using UnityEngine;

namespace Depra.Sound.FMOD.Editor
{
	internal sealed class FMODAudioBankImportWindow : EditorWindow
	{
		public const string MENU_PATH = "Depra/Sound/Import FMOD Bank";

		[MenuItem(MENU_PATH)]
		private static void Open() => GetWindow<FMODAudioBankImportWindow>("FMOD Bank Importer");

		private FMODAudioBank _target;
		private List<EditorBankRef> _banks;
		private List<EditorEventRef> _events;
		private readonly HashSet<string> _selection = new();
		private int _bankIndex;
		private string _search = string.Empty;
		private Vector2 _scroll;

		private void OnEnable() => LoadCache();

		private void OnGUI()
		{
			using (new EditorGUILayout.HorizontalScope())
			{
				_target = (FMODAudioBank)EditorGUILayout.ObjectField("Target Asset", _target,
					typeof(FMODAudioBank), false);
				if (GUILayout.Button("Create", GUILayout.Width(64f)))
				{
					CreateTarget();
				}
			}

			if (_banks == null || _banks.Count == 0)
			{
				EditorGUILayout.HelpBox("No FMOD banks found. Refresh the FMOD bank cache and try again.",
					MessageType.Warning);
				if (GUILayout.Button("Refresh"))
				{
					Refresh();
				}

				return;
			}

			_bankIndex = Mathf.Clamp(_bankIndex, 0, _banks.Count - 1);
			var selectedBank = EditorGUILayout.Popup("FMOD Bank", _bankIndex,
				_banks.Select(bank => string.IsNullOrEmpty(bank.StudioPath) ? bank.Name : bank.StudioPath).ToArray());
			if (selectedBank != _bankIndex)
			{
				_selection.Clear();
			}

			_bankIndex = selectedBank;
			EditorGUILayout.LabelField($"{FilteredEvents().Count} events  |  {_selection.Count} selected",
				EditorStyles.miniLabel);
			//_search = EditorGUILayout.ToolbarSearchField(_search);

			using (new EditorGUILayout.HorizontalScope())
			{
				if (GUILayout.Button("Select All", EditorStyles.miniButtonLeft))
				{
					SelectAll();
				}

				if (GUILayout.Button("Select None", EditorStyles.miniButtonRight))
				{
					_selection.Clear();
				}

				GUILayout.FlexibleSpace();
				if (GUILayout.Button("Refresh Cache", EditorStyles.miniButton))
				{
					Refresh();
				}
			}

			_scroll = EditorGUILayout.BeginScrollView(_scroll);
			foreach (var eventRef in FilteredEvents())
			{
				var guid = eventRef.Guid.ToString();
				var selected = _selection.Contains(guid);
				var label = $"{eventRef.Path} ({eventRef.Guid})";
				var next = EditorGUILayout.ToggleLeft(label, selected);
				if (next)
				{
					_selection.Add(guid);
				}
				else
				{
					_selection.Remove(guid);
				}
			}

			EditorGUILayout.EndScrollView();

			var projectTable = AudioProjectSettingsProvider.LoadTable();
			if (projectTable == null)
			{
				EditorGUILayout.HelpBox("Assign the project Audio Table in Project Settings > Sound before importing.",
					MessageType.Warning);
			}

			using (new EditorGUI.DisabledScope(!_target || _selection.Count == 0 || projectTable == null))
			{
				if (GUILayout.Button($"Import {_selection.Count} Selected Events", GUILayout.Height(30f)))
				{
					ImportSelected();
				}
			}
		}

		private void Refresh()
		{
			EventManager.RefreshBanks();
			LoadCache();
		}

		private void LoadCache()
		{
			_banks = EventManager.Banks ?? new List<EditorBankRef>();
			_events = EventManager.Events ?? new List<EditorEventRef>();
			_selection.Clear();
		}

		private List<EditorEventRef> FilteredEvents() => EventsInSelectedBank().Where(eventRef =>
			(string.IsNullOrEmpty(_search) || !string.IsNullOrEmpty(eventRef.Path) && eventRef.Path.IndexOf(_search,
				StringComparison.OrdinalIgnoreCase) >= 0)).ToList();

		private IEnumerable<EditorEventRef> EventsInSelectedBank()
		{
			if (_banks == null || _events == null || _banks.Count == 0)
			{
				return Enumerable.Empty<EditorEventRef>();
			}

			var bank = _banks[Mathf.Clamp(_bankIndex, 0, _banks.Count - 1)];
			return _events.Where(eventRef => eventRef.Banks != null && eventRef.Banks.Contains(bank));
		}

		private void SelectAll()
		{
			foreach (var eventRef in FilteredEvents())
			{
				_selection.Add(eventRef.Guid.ToString());
			}
		}

		private void CreateTarget()
		{
			var path = EditorUtility.SaveFilePanelInProject("Create FMOD Audio Bank", "FMOD Audio Bank",
				"asset", "Choose where to create the wrapper bank.");
			if (string.IsNullOrEmpty(path))
			{
				return;
			}

			_target = CreateInstance<FMODAudioBank>();
			AssetDatabase.CreateAsset(_target, path);
			AssetDatabase.SaveAssets();
		}

		private void ImportSelected()
		{
			var table = AudioProjectSettingsProvider.LoadTable();
			if (table == null)
			{
				Debug.LogError("Cannot import FMOD events without the project Audio Table.");
				return;
			}

			var bank = _banks[_bankIndex];
			var imports = new List<FMODAudioBank.EventEntry>();
			foreach (var eventRef in EventsInSelectedBank())
			{
				if (!_selection.Contains(eventRef.Guid.ToString()))
				{
					continue;
				}

				var eventId = table.AllocateEventId();
				var eventReference = new EventReference { Guid = eventRef.Guid, Path = eventRef.Path };
				var parameters = eventRef.LocalParameters.Select(CreateParameter).ToList();
				var eventDescription = FMODAudioEventDescription.Create(eventReference, parameters, eventRef.Is3D);
				imports.Add(new FMODAudioBank.EventEntry
				{
					Id = eventId,
					Name = Path.GetFileName(eventRef.Path) ?? eventRef.Path,
					Description = eventDescription
				});
			}

			Undo.RecordObject(table, "Allocate FMOD event IDs");
			Undo.RecordObject(_target, "Import FMOD events");
			var bankKey = string.IsNullOrEmpty(bank.StudioPath) ? bank.Path : bank.StudioPath;
			_target.Import(bankKey, imports, table);
			EditorUtility.SetDirty(table);
			EditorUtility.SetDirty(_target);

			table.Banks.Add(_target);
			EditorUtility.SetDirty(table);

			AssetDatabase.SaveAssets();
		}

		private static FMODAudioParamOverride CreateParameter(EditorParamRef parameter)
		{
			var supported = parameter.Type == ParameterType.Continuous || parameter.Type == ParameterType.Discrete;
			return new FMODAudioParamOverride
			{
				Name = parameter.Name,
				Minimum = parameter.Min,
				Maximum = parameter.Max,
				DefaultValue = parameter.Default,
				Value = parameter.Default,
				IsDiscrete = parameter.Type == ParameterType.Discrete,
				IsSupported = supported && (parameter.Type != ParameterType.Discrete ||
				                            Mathf.CeilToInt(parameter.Min) <= Mathf.FloorToInt(parameter.Max))
			};
		}
	}
}