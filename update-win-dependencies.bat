pwsh scripts\SteamDownloader.ps1 -steam_appid 258550 -platform windows -deps_dir "../raw-deps"
pwsh scripts\unprivate-dependencies.ps1 -outputPath "deps/windows/" -inputPath "raw-deps/windows/RustDedicated_Data/Managed"
PAUSE