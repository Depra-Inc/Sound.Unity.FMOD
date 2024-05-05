// SPDX-License-Identifier: Apache-2.0
// © 2024 Nikolay Melnikov <n.melnikov@depra.org>

using System;
using Depra.Sound.Parameter;

namespace Depra.Sound.FMOD
{
	[Serializable]
	public struct FmodInteger : IAudioClipParameter
	{
		public string Name;
		public int Value;
		public bool IgnoreSeekSpeed;

		public FmodInteger(string name, int value, bool ignoreSeekSpeed = false)
		{
			Name = name;
			Value = value;
			IgnoreSeekSpeed = ignoreSeekSpeed;
		}
	}
}