// SPDX-License-Identifier: Apache-2.0
// © 2024-2026 Depra <n.melnikov@depra.org>

using System;
using Depra.SerializeReference.Extensions;
using FMODUnity;
using UnityEngine;

namespace Depra.Sound.FMOD
{
	[Serializable]
	[SerializeReferenceMenuPath("FMOD Audio Clip")]
	public sealed class FMODAudioClip : IAudioClip, IEquatable<FMODAudioClip>
	{
		[SerializeField] private EventReference _event;

		public static implicit operator EventReference(FMODAudioClip clip) => clip._event;

		public FMODAudioClip() { }
		public FMODAudioClip(EventReference @event) => _event = @event;

		public EventReference Event => _event;
		public string Name => _event.ToString();

		public float Duration
		{
			get
			{
				RuntimeManager.GetEventDescription(_event).getLength(out var length);
				return length;
			}
		}

		public override string ToString() => Name;
		public override int GetHashCode() => Event.Guid.GetHashCode();

		public bool Equals(FMODAudioClip other) => Event.Guid == other?.Event.Guid;
		public override bool Equals(object obj) => obj is FMODAudioClip other && Equals(other);
	}
}