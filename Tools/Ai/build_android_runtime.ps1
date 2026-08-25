param(
    [string]$AndroidSdk = $env:ANDROID_HOME,
    [string]$Abi = "arm64-v8a",
    [int]$Api = 24
)

$ErrorActionPreference = "Stop"
$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
$llamaRoot = Join-Path $projectRoot "artifacts\tools\MiniCPM-V-Apps\llama.cpp-omni"
if (-not (Test-Path -LiteralPath (Join-Path $llamaRoot "CMakeLists.txt"))) {
    throw "llama.cpp-omni source is missing. Initialize artifacts/tools/MiniCPM-V-Apps first."
}
if ([string]::IsNullOrWhiteSpace($AndroidSdk)) {
    $AndroidSdk = Join-Path $env:LOCALAPPDATA "Android\Sdk"
}
$ndk = Get-ChildItem -LiteralPath (Join-Path $AndroidSdk "ndk") -Directory |
    Sort-Object Name -Descending | Select-Object -First 1
if ($null -eq $ndk) { throw "Android NDK was not found below $AndroidSdk. " }
$cmakeExe = Get-ChildItem -LiteralPath (Join-Path $AndroidSdk "cmake") -Filter cmake.exe -Recurse |
    Sort-Object FullName -Descending | Select-Object -First 1
if ($null -eq $cmakeExe) { throw "Android SDK CMake was not found below $AndroidSdk. " }
$ninjaExe = Join-Path $cmakeExe.Directory.FullName "ninja.exe"
if (-not (Test-Path -LiteralPath $ninjaExe)) { throw "Ninja was not found beside $($cmakeExe.FullName). " }

$nativeRoot = Join-Path $projectRoot "Native\ScheduleAi"
$buildRoot = Join-Path $projectRoot "artifacts\native-build\schedule-ai-$Abi"
$outputRoot = Join-Path $projectRoot "Platforms\Android\jniLibs\$Abi"
New-Item -ItemType Directory -Force -Path $buildRoot, $outputRoot | Out-Null

& $cmakeExe.FullName -S $nativeRoot -B $buildRoot -G Ninja `
    "-DCMAKE_TOOLCHAIN_FILE=$($ndk.FullName)\build\cmake\android.toolchain.cmake" `
    "-DCMAKE_MAKE_PROGRAM=$ninjaExe" `
    "-DANDROID_ABI=$Abi" "-DANDROID_PLATFORM=android-$Api" "-DCMAKE_BUILD_TYPE=Release" `
    "-DLLAMA_SRC=$llamaRoot"
if ($LASTEXITCODE -ne 0) { throw "CMake configure failed with exit code $LASTEXITCODE. " }
& $cmakeExe.FullName --build $buildRoot --target schedule_ai --parallel
if ($LASTEXITCODE -ne 0) { throw "Native build failed with exit code $LASTEXITCODE. " }

$library = Get-ChildItem -LiteralPath $buildRoot -Filter libschedule_ai.so -Recurse | Select-Object -First 1
if ($null -eq $library) { throw "libschedule_ai.so was not produced. " }
Copy-Item -LiteralPath $library.FullName -Destination (Join-Path $outputRoot "libschedule_ai.so") -Force
Write-Host "Native runtime copied to $outputRoot"
