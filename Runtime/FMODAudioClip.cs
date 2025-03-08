// SPDX-License-Identifier: Apache-2.0
// © 2024-2025 Depra <n.melnikov@depra.org>

using FMODUnity;

namespace Depra.Sound.FMOD
{
	public readonly struct FMODAudioClip : IAudioClip
	{
		public static implicit operator EventReference(FMODAudioClip clip) => clip._event;

		private readonly EventReference _event;
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
	}
}