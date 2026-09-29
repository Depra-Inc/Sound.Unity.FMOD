using Depra.Sound.Editor;
using JetBrains.Annotations;

namespace Depra.Sound.FMOD.Editor
{
	[UsedImplicitly]
	internal sealed class FMODAudioBankImporter : IAudioBankImporter
	{
		string IAudioBankImporter.Name => "FMOD Importer";
		string IAudioBankImporter.MenuPath => FMODAudioBankImportWindow.MENU_PATH;
	}
}