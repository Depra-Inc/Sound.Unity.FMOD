using System;
using System.Collections.Generic;
using System.Linq;
using FMOD;
using UnityEngine;

namespace Depra.Sound.FMOD
{
	[CreateAssetMenu(menuName = "Depra/Sound/FMOD Audio Bank", fileName = "FMOD Audio Bank")]
	public sealed class FMODAudioBank : AudioBankAsset
	{
		[SerializeField] private List<EventEntry> _events;
		[SerializeField] private List<AudioContainerEntry> _containers;
		[SerializeField] private string _sourceBankPath;

		public override string IconPath => $"Assets/Plugins/FMOD/images/StudioIcon.png";

		public override bool Contains(AudioEventId id) =>
			(_events != null && _events.Any(entry => entry.Id == id)) ||
			(_containers != null && _containers.Any(entry => entry.Id == id));

		public override void Compile(IDictionary<AudioEventId, IAudioEventDescription> map)
		{
			foreach (var entry in _events)
			{
				map.TryAdd(entry.Id, entry.Description.Compile());
			}

			foreach (var entry in _containers)
			{
				map.TryAdd(entry.Id, entry.Container.Compile());
			}
		}

		public override IEnumerable<(ulong id, string label)> GetAllEventNames() => from entry in _events
			let eventName = string.IsNullOrWhiteSpace(entry.Name) ? "Unnamed Event" : entry.Name
			select (entry.Id.Value, $"{eventName} ({entry.Id.Value}) - {eventName}");

#if UNITY_EDITOR
		internal void Import(string bankPath, IEnumerable<EventEntry> importedEvents,
			AudioProjectSettings settings)
		{
			if (!settings)
			{
				throw new ArgumentNullException(nameof(settings));
			}

			var events = _events ??= new List<EventEntry>();
			var previousEvents = new List<EventEntry>(events);
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
				if (imported.Description == null)
				{
					continue;
				}

				var description = imported.Description;
				var existingIndex = FindEvent(description.Event.Guid);
				var entry = imported;
				var existing = existingIndex >= 0
					? events[existingIndex]
					: FindEvent(previousEvents, description.Event.Guid);

				if (existing.Description != null && settings.ContainsEventId(existing.Id.Value, this))
				{
					existing = default;
				}

				if (existing.Description != null)
				{
					description.PreserveOverrides(existing.Description);
					entry.Id = existing.Id;
					if (existingIndex >= 0)
					{
						events[existingIndex] = entry;
					}
					else
					{
						events.Add(entry);
					}
				}
				else
				{
					entry.Id = settings.AllocateEventId(reservedIds, this);
					reservedIds.Add(entry.Id.Value);
					events.Add(entry);
				}
			}
		}

		private static EventEntry FindEvent(IReadOnlyList<EventEntry> events, GUID guid) =>
			events.FirstOrDefault(entry => entry.Description.Event.Guid.Equals(guid));

		private int FindEvent(GUID guid)
		{
			for (var index = 0; index < _events.Count; index++)
			{
				var description = _events[index].Description;
				if (description != null && description.Event.Guid.Equals(guid))
				{
					return index;
				}
			}

			return -1;
		}
#endif
		[Serializable]
		internal struct EventEntry
		{
			public string Name;
			public AudioEventId Id;
			public FMODAudioEventDescription Description;
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