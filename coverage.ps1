$buildConfig = if ($ENV:CONFIGURATION -eq $null) { "Release" } else { $ENV:CONFIGURATION }

$buildFolder = if ($ENV:APPVEYOR_BUILD_FOLDER -eq $null) { $PSScriptRoot } else { $ENV:APPVEYOR_BUILD_FOLDER }

$openCover = 'C:\ProgramData\chocolatey\lib\opencover.portable\tools\OpenCover.Console.exe'

$target = '-target:C:\Program Files\dotnet\dotnet.exe'
$targetArgs = '-targetargs:"test -c:' + $buildConfig + ' --logger:trx;LogFileName=results.trx --filter FullyQualifiedName!~Benchmarks /p:DebugType=full"' 
$filter = '-filter:+[Kentico.Kontent.Delivery*]*-[*Tests]*-[*Benchmarks]*'
$output = '-output:' + $buildFolder + '\coverage.xml'
$register = if ($ENV:APPVEYOR -eq $true ) { '-register' } else { '-register:user' } # Magical parameter that breaks things

& $openCover $target $targetArgs $filter $register '-oldStyle' '-mergeoutput' $output
