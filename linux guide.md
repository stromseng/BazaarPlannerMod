1. Download the Installer(https://playthebazaar.com/download)

2. Add the Installer to Steam

- Open **Steam**
- Go to **Games > Add a Non-Steam Game**
- Click **Browse**, and select the `Tempo Launcher - Beta Setup 1.0.4.exe` installer you downloaded

 3. Set Compatibility

- In your Library, right-click the installer
- Choose **Properties > Compatibility**
- Check **“Force the use of a specific Steam Play compatibility tool”**
- Select **Proton 9.0-4**

4. Install the Launcher

Run the `.exe` installer through Steam.
I suggest installing to: `~/Games/` (or somewhere easy to remember)

5. Sign in and Launch the Game

- Open the installed **Tempo Launcher**
- Log into your account
- Let the launcher finish downloading and unpacking the game
- Launch the game at least once to finalize setup

6. Close the Game

Once the game has launched successfully, **close it**.

7. Enable `winhttp` via Wine

Open a terminal and run:

```
winecfg
```

In the window that appears:

- Go to the **Libraries** tab
- From the drop-down, choose `winhttp` and click **Add**
- Confirm `winhttp (native, builtin)` appears under existing overrides
- Click **OK** to close

8. (Optional) Clean up Installer

You can remove the Installer from Steam if you'd like. It served its purpose.

9. Add the Actual Launcher to Steam

- Go to **Add a Non-Steam Game** again
- Browse to the installed launcher:
  ```
  ~/Games/Tempo Launcher - Beta/Tempo Launcher - Beta.exe
  ```

10. Set Launch Options

In the new Steam entry:

- Right-click → **Properties**
- Under **LAUNCH OPTIONS**, paste:

```
WINEDLLOVERRIDES="winhttp=n,b" %command%
```

- Also make sure Proton 9.0-4 is selected under Compatibility again

11. (Optional) Test the Game

Launch the game again via Steam and ensure it works correctly with these settings.

12. Install the Mod

**Option 1: Wine Installer**

- Run `BazaarPlannerModInstaller.exe` via Wine.
- The intallation path should be on the `Z:/home/[your-username]/Games/Tempo Launcher - Beta/The Bazaar game_64/bazaarwinprodlatest/`

**Option 2: Manual Install**

13. Done!

Launch the game from your Steam library. The Mod should be working.
