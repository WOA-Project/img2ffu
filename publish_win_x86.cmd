@echo off

msbuild /m /t:restore,img2ffu:publish /p:Platform=x86 /p:RuntimeIdentifier=win-x86 /p:PublishDir="%CD%\publish\artifacts\win-x86\CLI" /p:PublishSingleFile=true /p:PublishTrimmed=false /p:Configuration=Release Img2Ffu.sln