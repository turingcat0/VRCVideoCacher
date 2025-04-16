dotnet publish -r win-x64 -c Release
xcopy bin\Release\net9.0\win-x64\publish\yt-dlp-stub.exe ..\VRCVideoCacher\ /Y