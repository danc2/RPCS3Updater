# RPCS3Updater
C# Program to update to the latest version of RPCS3

Usage: 

1. Extract all files from the latest release inside your RPCS3 folder (RPCS3Updater.exe, SharpCompress.dll and JSON files)
2. Double click RPCS3Updater.exe (if you don't care about watching the output) or open a command window in the same directory and execute.
3. Answer the questions and watch the output.
4. It's really that simple!

RPCS3 updater can also take arguments:
-y to autoupdate, -h to print help, and -nolaunch to disable autolaunching behavior when an update isn't needed

How it works: 
RPCS3 uses github's api to find a new verison of RPCS3 for windows. It then downloads the file, extracts it, replacing your current files. It does not delete save games or any artifacts that were there previously. 


