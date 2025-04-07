# Installing the Mod
Example game path:
C:\Program Files\Tempo Launcher - Beta\The Bazaar game_64\bazaarwinprodlatest

1. Download BepInEx BepInEx_win_x64_5.4.23.2.zip from this link:
- https://github.com/BepInEx/BepInEx/releases
2. Extract the downloaded files into the game's root folder.

After extracting, the game folder should include these files and folders:

    A folder named BepInEx
    A file named winhttp.dll
    Other BepInEx files.

3. Start the game so BepInEx can generate files
4. Place the Provided .dll File into the BepInEx/plugins Folder

# Developing the Mod
1. Enable `Logging.Console` in the BepInEx config
2. Change the GamePath in the csproj to point at your game installation

# Using the Mod
1. Enable `Logging.Console` in the BepInEx config
2. Press `P` in game to write the state to the console