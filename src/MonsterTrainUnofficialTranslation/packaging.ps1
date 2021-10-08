param ([string]$PackageTarget, [string]$SolutionDir, [string]$ProjectDir, [string]$TargetDir, [string]$SteamAppsDir)

$ErrorActionPreference = "Stop"

function CheckLastExitCode {
    $ec = $LastExitCode
    if ($ec -ne 0) {
        (gci rebuild.trigger).LastWriteTime = Get-Date

        # https://stackoverflow.com/a/50202663/3567518
        $host.SetShouldExit($ec)
        exit $ec
    }
}

xcopy "deployment" "$($PackageTarget)\content\" /EY
CheckLastExitCode
xcopy "$($TargetDir)*.dll" "$($PackageTarget)\content\plugins\" /EY
CheckLastExitCode
xcopy "$($SolutionDir)..\locale" "$($PackageTarget)\content\plugins\locale\" /EY
CheckLastExitCode
Copy-Item "$($ProjectDir)workshop.vdf" "$($PackageTarget)\"
CheckLastExitCode

# Install as local plugin
#xcopy "$($PackageTarget)\content" "$($SteamAppsDir)\workshop\content\1102190\content\" /EY
#CheckLastExitCode
