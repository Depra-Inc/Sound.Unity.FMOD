// SPDX-License-Identifier: Apache-2.0
// © 2024 Nikolay Melnikov <n.melnikov@depra.org>

using System;
using Depra.SerializeReference.Extensions;
using Depra.Sound.Parameter;

namespace Depra.Sound.FMOD
{
	[Serializable]
	[SerializeReferenceIcon("d_FilterByLabel")]
	public struct FmodLabel : IAudioClipParameter
	{
		public string Name;
		public string Value;
		public bool IgnoreSeekSpeed;

		public FmodLabel(string name, string value, bool ignoreSeekSpeed = false)
		{
			Name = name;
			Value = value;
			IgnoreSeekSpeed = ignoreSeekSpeed;
		}
	}
}