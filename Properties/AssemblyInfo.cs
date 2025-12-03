using System.Reflection;
using MelonLoader;

[assembly: AssemblyTitle(BlippoAccess.BuildInfo.Description)]
[assembly: AssemblyDescription(BlippoAccess.BuildInfo.Description)]
[assembly: AssemblyCompany(BlippoAccess.BuildInfo.Company)]
[assembly: AssemblyProduct(BlippoAccess.BuildInfo.Name)]
[assembly: AssemblyCopyright("Created by " + BlippoAccess.BuildInfo.Author)]
[assembly: AssemblyTrademark(BlippoAccess.BuildInfo.Company)]
[assembly: AssemblyVersion(BlippoAccess.BuildInfo.Version)]
[assembly: AssemblyFileVersion(BlippoAccess.BuildInfo.Version)]
[assembly: MelonInfo(typeof(BlippoAccess.BlippoAccessMod), BlippoAccess.BuildInfo.Name, BlippoAccess.BuildInfo.Version, BlippoAccess.BuildInfo.Author, BlippoAccess.BuildInfo.DownloadLink)]
[assembly: MelonColor()]

// Create and Setup a MelonGame Attribute to mark a Melon as Universal or Compatible with specific Games.
// If no MelonGame Attribute is found or any of the Values for any MelonGame Attribute on the Melon is null or empty it will be assumed the Melon is Universal.
// Values for MelonGame Attribute can be found in the Game's app.info file or printed at the top of every log directly beneath the Unity version.
[assembly: MelonGame(null, null)]