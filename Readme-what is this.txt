========== What is this? =============
BazaarPlannerMod will make it easy to export your game data to BazaarPlanner, instantly changing your board on the site while you change it in game. Along with tracking your runs and making it easy to look back and load previous battles to replay, or to see how your boards stack up against the top ones submitted. 

While Reynad isn't currently supporting our tool, we hope that he might change his mind, and that this feature won't be received negatively by their team. Don't talk about your usage of this tool in the official discord, or you may get banned (some people posting simply "have you heard of bazaarplanner" have been timed out or banned within 10 seconds.)

Small request - lets stay positive in regards to feedback (and not disparage the tempo storm staff in any way), that would be great! They have made a fun game, and we want them to support tools and a community that make playing the game even more fun. 

Thanks again!


========== How to Install ============
Simply extract all the files to a directory and run BazaarPlannerModInstaller.exe
Follow instructions to get the BazaarPlanner.config file from the website; you can install once it's detected in the same directory.
(if for some reason you want to install on linux, see the linux guide in the repo, credits to @yggraszill !)

========== Manual install ============
Without running the .exe - if you don't trust it not to have been tampered with.
Always good to do this when possible, we don't publish with viruses/malware/spyware, but your friend giving this to you might give you a keylogger along with it! If you are going to run an exe make sure its the one linked directly from our site.
==============================================================
1. extract BepInEx_win_x64_5.4.23.2.zip to your game install directory (that contains TheBazaar.exe).
By default this is C:\Program Files\Tempo Launcher - Beta\The Bazaar game_64\bazaarwinprodlatest

2. create a directory "plugins" inside the BepInEx directory that got created.

3. copy BazaarPlannerMod.dll to C:\Program Files\Tempo Launcher - Beta\The Bazaar game_64\bazaarwinprodlatest\BepInEx\plugins

Starting the game will now create files and allow you to press 'b' to load your board into bazaarplanner.
BUT! There's more. If you want to follow and track your runs, you'll need to continue.

4. Go to bazaarplanner.com and click the gear icon in the top right, login if needed, and click Export BazaarPlanner.config

5. Create a directory "config" inside the BepInEx directory and Copy this config file there.

6. Rename it from BazaarPlanner.config to BazaarPlanner.cfg
(Sometimes windows makes this annoying because it hides file extensions from you (god I hate bill gates for doing this so many years ago). If you start the game and look in this folder and see two different files, Simply take the contents of BazaarPlanner.config and copy them into BazaarPlanner.cfg - you can then delete the .config file)

Done! 
Share and Enjoy!

