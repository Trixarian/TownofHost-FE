@echo off
set output_path=%p2f%bin\Release\net6.0

echo Building TOHE...
@echo on
dotnet build -c Release
@echo off
pause
cd "%output_path%"
explorer .