using System;
using Depra.Sound.Parameter;

namespace Depra.Sound.FMOD
{
	[Serializable]
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