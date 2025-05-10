# Installing the Mod
You don't need to clone this repo, simply download the latest version zip file from
https://github.com/oceanseth/BazaarPlannerMod/releases
then run the Installer.exe file or follow Manual instructions in the text file it contains.
- [Install on Linux](README-Linux.md)

If you want to build it yourself, clone the repo and run `dotnet publish -c Release` in the root folder.
Then, simply run the BazaarPlannerMod/bin/Release/net8.0-windows/BazaarPlannerModInstaller.exe

# Developing the Mod
1. Enable `Logging.Console` in the BepInEx config
2. Change the GamePath in the csproj to point at your game installation

# Using the Mod
1. Press 'b' while in game to open board states on bazaarplanner
2. OR intead of 1, simply login to the planner website and click 'follow' in the board buttons to follow your game changes.

# Contributing
If you have any suggestions or feedback, please join the discord server and open an issue.

# License
This project is licensed under the MIT License. See the LICENSE file for details.


#Managing releases
Make sure the latest version number has been set in the csproj file and the BazaarPlannerModInstaller/Program.cs file
Run dotnet publish -c Release
copy the Readme-what is this.txt file to the publish folder
copy the BepInEx_win_x64_5.4.23.2.zip file to the publish folder
zip the files of that folder into the zip file
tag the branch with the release version and add the zip file to the release assets