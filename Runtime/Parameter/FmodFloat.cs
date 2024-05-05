// SPDX-License-Identifier: Apache-2.0
// © 2024 Nikolay Melnikov <n.melnikov@depra.org>

using System;
using Depra.Sound.Parameter;

namespace Depra.Sound.FMOD
{
	[Serializable]
	public struct FmodFloat : IAudioClipParameter
	{
		public string Name;
		public float Value;
		public bool IgnoreSeekSpeed;

		public FmodFloat(string name, float value, bool ignoreSeekSpeed = false)
		{
			Name = name;
			Value = value;
			IgnoreSeekSpeed = ignoreSeekSpeed;
		}
	}
}