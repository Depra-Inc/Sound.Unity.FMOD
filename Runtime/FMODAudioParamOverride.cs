namespace Depra.Sound.FMOD
{
	[System.Serializable]
	public sealed class FMODAudioParamOverride
	{
		public string Name;
		public float Minimum;
		public float Maximum;
		public float DefaultValue;
		public float Value;
		public ParamType Type;
		public bool IsSupported = true;
		public bool Enabled;
		public string[] Labels = System.Array.Empty<string>();

		public enum ParamType
		{
			CONTINUOUS,
			DISCRETE,
			LABELED,
		}
	}
}