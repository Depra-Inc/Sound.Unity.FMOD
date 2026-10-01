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
		[SerializeField] private FMODBankMetadata _metadata;

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
			select (entry.Id.Value, $"{eventName} ({entry.Id.Value})");

#if UNITY_EDITOR
		internal void Import(FMODBankMetadata metadata, IEnumerable<EventEntry> importedEvents,
			AudioProjectSettings settings)
		{
			if (!settings)
			{
				throw new ArgumentNullException(nameof(settings));
			}

			var events = _events ??= new List<EventEntry>();
			var reservedIds = new HashSet<ulong>();
			var previousEvents = new List<EventEntry>(events);
			foreach (var entry in previousEvents)
			{
				reservedIds.Add(entry.Id.Value);
			}

			if (_metadata.BankPath != metadata.BankPath)
			{
				events.Clear();
			}

			_metadata = metadata;
			foreach (var imported in importedEvents)
			{
				if (imported.Description == null)
				{
					continue;
				}

				var entry = imported;
				var description = entry.Description;
				var existingIndexes = FindEvents(events, description.Event.Guid);
				var existing = existingIndexes.Count > 0
					? events[existingIndexes[0]]
					: FindEvent(previousEvents, description.Event.Guid);

				if (existing.Description != null && settings.ContainsEventId(existing.Id.Value, this))
				{
					existing = default;
				}

				if (existing.Description != null)
				{
					if (existingIndexes.Count == 0)
					{
						description.PreserveOverrides(existing.Description);
						entry.Id = existing.Id;
						events.Add(entry);
						continue;
					}

					foreach (var existingIndex in existingIndexes)
					{
						var existingEntry = events[existingIndex];
						var clonedDescription = description.Clone();
						clonedDescription.PreserveOverrides(existingEntry.Description);
						existingEntry.Description = clonedDescription;
						if (string.IsNullOrWhiteSpace(existingEntry.Name))
						{
							existingEntry.Name = entry.Name;
						}

						events[existingIndex] = existingEntry;
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
			events.FirstOrDefault(entry => entry.Description != null && entry.Description.Event.Guid.Equals(guid));

		private static List<int> FindEvents(IReadOnlyList<EventEntry> events, GUID guid)
		{
			var indexes = new List<int>();
			for (var index = 0; index < events.Count; index++)
			{
				var description = events[index].Description;
				if (description != null && description.Event.Guid.Equals(guid))
				{
					indexes.Add(index);
				}
			}

			return indexes;
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
}