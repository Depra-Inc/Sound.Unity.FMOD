using System;

namespace Depra.Sound.FMOD
{
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