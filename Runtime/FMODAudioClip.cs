// SPDX-License-Identifier: Apache-2.0
// © 2024-2025 Depra <n.melnikov@depra.org>

using System;
using FMODUnity;
using UnityEngine;

namespace Depra.Sound.FMOD
{
	[Serializable]
	public struct FMODAudioClip : IAudioClip, IEquatable<FMODAudioClip>
	{
		public static implicit operator EventReference(FMODAudioClip clip) => clip._event;

		[SerializeField] private EventReference _event;

		public FMODAudioClip(EventReference @event) => _event = @event;

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
		public override int GetHashCode() => _event.Guid.GetHashCode();

		public bool Equals(FMODAudioClip other) => _event.Guid == other._event.Guid;
		public override bool Equals(object obj) => obj is FMODAudioClip other && Equals(other);
	}
}