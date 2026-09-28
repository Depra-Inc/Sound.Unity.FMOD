using System;
using System.Collections.Generic;
using Depra.Sound.Configuration;
using Depra.Sound.Runtime;
using FMODUnity;
using UnityEngine;

namespace Depra.Sound.FMOD
{
	[CreateAssetMenu(menuName = "Depra/Sound/FMOD Audio Bank", fileName = "FMOD Audio Bank")]
	public sealed class FMODAudioBank : AudioBankAsset
	{
		[SerializeField] private List<AudioBankEntry> _events = new();
		[SerializeField] private string _sourceBankPath;

		public override IReadOnlyList<AudioBankEntry> Events => _events;
		public string SourceBankPath => _sourceBankPath;

#if UNITY_EDITOR
		public void Import(string bankPath, IReadOnlyList<AudioBankEntry> importedEvents, AudioProjectSettings settings)
		{
			if (!settings)
			{
				throw new ArgumentNullException(nameof(settings));
			}

			var events = _events;
			var previousEvents = new List<AudioBankEntry>(events);
			var reservedIds = new HashSet<ulong>();
			foreach (var entry in previousEvents)
			{
				reservedIds.Add(entry.Id.Value);
			}

			if (_sourceBankPath != bankPath)
			{
				_sourceBankPath = bankPath;
				events.Clear();
			}

			foreach (var imported in importedEvents)
			{
				if (imported.Description is not FMODAudioEventDescription importedDescription)
				{
					continue;
				}

				var existingIndex = FindEvent(importedDescription.Event.Guid);
				var entry = imported;
				var existing = existingIndex >= 0
					? events[existingIndex]
					: FindEvent(previousEvents, importedDescription.Event.Guid);
				if (existing.Description != null && settings.ContainsEventId(existing.Id.Value, this))
				{
					existing = default;
				}

				if (existing.Description != null)
				{
					if (existing.Description is FMODAudioEventDescription existingDescription)
					{
						importedDescription.PreserveOverrides(existingDescription);
					}

					entry.Id = existing.Id;
					if (existingIndex >= 0) events[existingIndex] = entry;
					else events.Add(entry);
				}
				else
				{
					entry.Id = settings.AllocateEventId(reservedIds, this);
					reservedIds.Add(entry.Id.Value);
					events.Add(entry);
				}
			}
		}

		private static AudioBankEntry FindEvent(IReadOnlyList<AudioBankEntry> events, global::FMOD.GUID guid)
		{
			for (var index = 0; index < events.Count; index++)
			{
				if (events[index].Description is FMODAudioEventDescription description &&
				    description.Event.Guid.Equals(guid))
				{
					return events[index];
				}
			}

			return default;
		}

		private int FindEvent(global::FMOD.GUID guid)
		{
			var events = _events;
			for (var index = 0; index < events.Count; index++)
			{
				if (events[index].Description is FMODAudioEventDescription description &&
				    description.Event.Guid.Equals(guid))
				{
					return index;
				}
			}

			return -1;
		}

#endif
	}

	[Serializable]
	public sealed class FMODAudioEventDescription : IAudioEventVariant
	{
		[SerializeField] private FMODAudioClip _clip;
		[SerializeField] private List<FMODAudioParamOverride> _parameters = new();
		[SerializeField] private bool _is3D;

		public IAudioClip Clip => _clip;
		public EventReference Event => _clip.Event;
		public IReadOnlyList<FMODAudioParamOverride> Parameters => _parameters;
		public bool Is3D => _is3D;

		public IAudioEventDescription Compile()
		{
			var parameters = new List<AudioParam>();
			foreach (var parameter in _parameters)
			{
				if (parameter.Enabled && parameter.IsSupported)
				{
					parameters.Add(parameter.IsDiscrete
						? AudioParam.CustomRef(UnityAudioParamId.LabeledInt, UnityAudioParamId.LabeledInt, parameter.Name,
							integerValue: Mathf.RoundToInt(parameter.Value))
						: AudioParam.CustomRef(UnityAudioParamId.LabeledFloat, UnityAudioParamId.LabeledFloat, parameter.Name,
							float0: parameter.Value));
				}
			}

			var requirements = _is3D
				? new AudioEventRequirements(new List<IAudioEventRequirement> { new PositionRequirement() })
				: null;
			return new RuntimeAudioEvent(_clip, requirements, parameters.ToArray());
		}

#if UNITY_EDITOR
		public static FMODAudioEventDescription Create(EventReference reference,
			IReadOnlyList<FMODAudioParamOverride> parameters, bool is3D) => new()
		{
			_clip = new FMODAudioClip(reference),
			_parameters = new List<FMODAudioParamOverride>(parameters),
			_is3D = is3D
		};
#endif

		internal void PreserveOverrides(FMODAudioEventDescription previous)
		{
			foreach (var parameter in _parameters)
			{
				foreach (var oldParameter in previous._parameters)
				{
					if (parameter.Name != oldParameter.Name) continue;
					parameter.Enabled = oldParameter.Enabled;
					parameter.Value = Mathf.Clamp(oldParameter.Value, parameter.Minimum, parameter.Maximum);
					break;
				}
			}
		}
	}

	[Serializable]
	public sealed class FMODAudioParamOverride
	{
		public string Name;
		public float Minimum;
		public float Maximum;
		public float DefaultValue;
		public float Value;
		public bool IsDiscrete;
		public bool IsSupported = true;
		public bool Enabled;
	}
}