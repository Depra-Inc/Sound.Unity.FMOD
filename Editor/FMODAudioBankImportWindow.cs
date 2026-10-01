using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Depra.Sound.Unity.Editor;
using FMODUnity;
using UnityEditor;
using UnityEngine;

namespace Depra.Sound.FMOD.Editor
{
	internal sealed class FMODAudioBankImportWindow : EditorWindow
	{
		public const string MENU_PATH = "Depra/Sound/Import FMOD Bank";
		private const string ALL_EVENTS_OPTION = "<All Events>";
		private const string ALL_EVENTS_METADATA_LABEL = "All FMOD Events";

		[MenuItem(MENU_PATH)]
		private static void Open() => Open(null);

		public static void Open(FMODAudioBank bank)
		{
			var window = GetWindow<FMODAudioBankImportWindow>("FMOD Bank Importer");
			window.titleContent.image = EditorIcons.STUDIO;
			window._target = bank;
		}

		private FMODAudioBank _target;
		private List<EditorBankRef> _banks;
		private List<EditorEventRef> _events;
		private readonly HashSet<string> _selection = new();
		private int _bankIndex = -1;
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
					CreateTarget(out _target);
				}
			}

			var hasEvents = _events != null && _events.Count > 0;
			if (!hasEvents)
			{
				EditorGUILayout.HelpBox("No FMOD events found. Refresh the FMOD bank cache and try again.",
					MessageType.Warning);
				if (GUILayout.Button("Refresh"))
				{
					Refresh();
				}

				return;
			}

			var displayedOptions = new List<string> { ALL_EVENTS_OPTION };
			if (_banks != null)
			{
				displayedOptions.AddRange(_banks
					.Select(bank => string.IsNullOrEmpty(bank.StudioPath) ? bank.Name : bank.StudioPath));
			}

			var popupIndex = Mathf.Clamp(_bankIndex + 1, 0, displayedOptions.Count - 1);
			var selectedPopupIndex = EditorGUILayout.Popup("FMOD Bank", popupIndex, displayedOptions.ToArray());
			if (selectedPopupIndex != popupIndex)
			{
				_selection.Clear();
			}

			_bankIndex = selectedPopupIndex - 1;
			EditorGUILayout.LabelField($"{FilteredEvents().Count} events  |  {_selection.Count} selected",
				EditorStyles.miniLabel);
			_search = EditorGUILayout.TextField(_search, EditorStyles.toolbarSearchField);

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

				if (GUILayout.Button("Fast Import All", EditorStyles.miniButton))
				{
					ImportAllBanks();
				}
			}

			DrawCacheDiagnostics();

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
			if (!projectTable)
			{
				EditorGUILayout.HelpBox("Assign the project Audio Table in Project Settings > Sound before importing.",
					MessageType.Warning);
			}

			using (new EditorGUI.DisabledScope(!_target || _selection.Count == 0 || !projectTable))
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

		private void HardRefresh()
		{
			var cacheAssetPath = EventManager.CacheAssetFullName;
			if (!string.IsNullOrEmpty(cacheAssetPath))
			{
				AssetDatabase.DeleteAsset(cacheAssetPath);
			}

			AssetDatabase.Refresh();
			EventManager.RefreshBanks();
			LoadCache();
		}

		private void LoadCache()
		{
			_banks = EventManager.Banks ?? new List<EditorBankRef>();
			_events = EventManager.Events ?? new List<EditorEventRef>();
			_selection.Clear();
		}

		private void DrawCacheDiagnostics()
		{
			var missingBanks = GetMissingBanksInCache();
			if (missingBanks.Count == 0)
			{
				return;
			}

			var preview = string.Join(", ", missingBanks.Take(6));
			if (missingBanks.Count > 6)
			{
				preview += ", ...";
			}

			EditorGUILayout.HelpBox(
				$"FMOD cache may be stale. Not in EventManager cache: {preview}",
				MessageType.Warning);

			if (GUILayout.Button("Rebuild FMOD Cache", EditorStyles.miniButton))
			{
				HardRefresh();
			}
		}

		private List<string> GetMissingBanksInCache()
		{
			try
			{
				var sourceBankPath = Settings.Instance?.SourceBankPath;
				if (string.IsNullOrEmpty(sourceBankPath) || !Directory.Exists(sourceBankPath))
				{
					return new List<string>();
				}

				var diskBanks = Directory
					.GetFiles(sourceBankPath, "*.bank", SearchOption.AllDirectories)
					.Where(path => !path.EndsWith(".strings.bank", StringComparison.OrdinalIgnoreCase))
					.Select(Path.GetFileNameWithoutExtension)
					.Where(bankName => !string.IsNullOrEmpty(bankName))
					.Distinct(StringComparer.OrdinalIgnoreCase)
					.ToList();

				if (diskBanks.Count == 0)
				{
					return new List<string>();
				}

				var cacheBanks = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
				if (_banks != null)
				{
					foreach (var bank in _banks)
					{
						if (bank == null)
						{
							continue;
						}

						if (!string.IsNullOrEmpty(bank.Name))
						{
							cacheBanks.Add(Path.GetFileNameWithoutExtension(bank.Name));
						}

						if (!string.IsNullOrEmpty(bank.Path))
						{
							cacheBanks.Add(Path.GetFileNameWithoutExtension(bank.Path));
						}
					}
				}

				return diskBanks
					.Where(bankName => !cacheBanks.Contains(bankName))
					.OrderBy(bankName => bankName)
					.ToList();
			}
			catch
			{
				return new List<string>();
			}
		}

		private List<EditorEventRef> FilteredEvents() => EventsInScope().Where(eventRef =>
			(string.IsNullOrEmpty(_search) || !string.IsNullOrEmpty(eventRef.Path) && eventRef.Path.IndexOf(_search,
				StringComparison.OrdinalIgnoreCase) >= 0)).ToList();

		private IEnumerable<EditorEventRef> EventsInScope()
		{
			if (_events == null || _events.Count == 0)
			{
				return Enumerable.Empty<EditorEventRef>();
			}

			if (_bankIndex < 0)
			{
				return _events;
			}

			if (_banks == null || _banks.Count == 0)
			{
				return Enumerable.Empty<EditorEventRef>();
			}

			var bank = _banks[Mathf.Clamp(_bankIndex, 0, _banks.Count - 1)];
			return _events.Where(eventRef => eventRef.Banks != null && eventRef.Banks.Contains(bank));
		}

		private IEnumerable<EditorEventRef> EventsInBank(EditorBankRef bank) => _events == null
			? Enumerable.Empty<EditorEventRef>()
			: _events.Where(eventRef => eventRef.Banks != null && eventRef.Banks.Contains(bank));

		private void SelectAll()
		{
			foreach (var eventRef in FilteredEvents())
			{
				_selection.Add(eventRef.Guid.ToString());
			}
		}

		private void ImportSelected()
		{
			var table = AudioProjectSettingsProvider.LoadTable();
			if (!table)
			{
				Debug.LogError("Cannot import FMOD events without the project Audio Table.");
				return;
			}

			var imports = from eventRef in EventsInScope()
				where _selection.Contains(eventRef.Guid.ToString())
				select CreateImportedEvent(eventRef, table);

			Undo.RecordObject(table, "Allocate FMOD event IDs");
			Undo.RecordObject(_target, "Import FMOD events");

			FMODBankMetadata metadata;
			if (_bankIndex < 0)
			{
				metadata = new FMODBankMetadata { BankPath = ALL_EVENTS_METADATA_LABEL, TotalSize = 0L };
			}
			else
			{
				var bank = _banks[Mathf.Clamp(_bankIndex, 0, _banks.Count - 1)];
				var bankKey = string.IsNullOrEmpty(bank.StudioPath) ? bank.Path : bank.StudioPath;
				var totalSize = bank.FileSizes.Sum(sizeInfo => sizeInfo.Value);
				metadata = new FMODBankMetadata { BankPath = bankKey, TotalSize = totalSize };
			}

			_target.Import(metadata, imports, table);
			EditorUtility.SetDirty(_target);
			if (!table.Banks.Contains(_target))
			{
				table.Banks.Add(_target);
				EditorUtility.SetDirty(table);
			}

			AssetDatabase.SaveAssets();
		}

		private void ImportAllBanks()
		{
			var table = AudioProjectSettingsProvider.LoadTable();
			if (!table)
			{
				Debug.LogError("Cannot import FMOD events without the project Audio Table.");
				return;
			}

			if (!CreateTarget(out _target))
			{
				return;
			}

			Undo.RecordObject(table, "Import all FMOD banks");
			foreach (var bank in _banks)
			{
				var bankKey = string.IsNullOrEmpty(bank.StudioPath) ? bank.Path : bank.StudioPath;
				if (!CreateTarget(out _target, bankKey))
				{
					return;
				}

				var totalSize = bank.FileSizes.Sum(sizeInfo => sizeInfo.Value);
				var metadata = new FMODBankMetadata { BankPath = bankKey, TotalSize = totalSize };
				var imports = EventsInBank(bank).Select(editorEvent => CreateImportedEvent(editorEvent, table));
				_target.Import(metadata, imports, table);
				EditorUtility.SetDirty(_target);
				if (!table.Banks.Contains(_target))
				{
					table.Banks.Add(_target);
				}
			}

			EditorUtility.SetDirty(table);
			AssetDatabase.SaveAssets();
		}

		private static bool CreateTarget(out FMODAudioBank createdBank, string fmodBankPath = null)
		{
			var defaultName = string.IsNullOrEmpty(fmodBankPath)
				? "FMOD_Audio_Bank"
				: Path.GetFileNameWithoutExtension(fmodBankPath) + "_Imported";

			var path = EditorUtility.SaveFilePanelInProject("Create FMOD Audio Bank", defaultName,
				"asset", "Choose where to create the wrapper bank.");
			if (string.IsNullOrEmpty(path))
			{
				createdBank = null;
				return false;
			}

			createdBank = CreateInstance<FMODAudioBank>();
			AssetDatabase.CreateAsset(createdBank, path);
			AssetDatabase.SaveAssets();
			return true;
		}

		internal static FMODAudioBank.EventEntry CreateImportedEvent(EditorEventRef eventRef,
			AudioProjectSettings table)
		{
			var eventId = table.AllocateEventId();
			var eventReference = new EventReference { Guid = eventRef.Guid, Path = eventRef.Path };
			var parameters = eventRef.LocalParameters.Select(CreateParameter).ToList();
			var eventDescription = FMODAudioEventDescription.Create(eventReference, parameters, eventRef.Is3D);
			var displayName = Path.GetFileName(eventRef.Path) ?? eventRef.Path;
			return new FMODAudioBank.EventEntry
			{
				Id = eventId,
				Name = displayName,
				Description = eventDescription
			};
		}

		private static FMODAudioParamOverride CreateParameter(EditorParamRef param) => new()
		{
			Name = param.Name,
			Minimum = param.Min,
			Maximum = param.Max,
			DefaultValue = param.Default,
			Value = param.Default,
			Labels = param.Labels,
			Type = (FMODAudioParamOverride.ParamType)param.Type,
			IsSupported = param.Type != ParameterType.Discrete ||
			              Mathf.CeilToInt(param.Min) <= Mathf.FloorToInt(param.Max)
		};
	}
}