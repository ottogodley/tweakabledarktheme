# Tweakable Dark Theme

A dark Aurora theme based on the splendid [SolarisTheme](https://github.com/simast/SolarisTheme) (basically all credit goes to simast for their wonderful work), with some added customisation available via the "Change Settings" button on the AuroraPatch launcher before starting the game. Each setting is written to its own json file within this mod's folder, default settings can be restored by deleting those settings files. You can change any of the icons inside the Icons directory, but the names must remain unchanged.

Yes, I know the UI on the settings window sucks - it _is_ functional though.

## Details

Changes include:

1) Font selection options (many thanks to twice2double for the code for this in [T2DTheme](https://github.com/Aurora-Modders/T2DTheme))
2) Background colour customisation
3) Foreground (text) colour customisation

![ChangeSettings](/Settings.png?raw=true)
![TweakableDarkTheme](/TweakableDarkTheme.png?raw=true)

## Install

* Install [AuroraPatch](https://github.com/Aurora-Modders/AuroraPatch)
* Unzip release archive into Patches directory in your Aurora install directory. Resulting directory structure should be as follows:
`Aurora/Patches/TweakableDarkTheme/*mod dll file and icons directory here*`

## Dependencies

Requires AuroraPatch >= 0.1.0, Lib.dll from AuroraPatch, and the ThemeCreator >= 1.3.0.0 patch.