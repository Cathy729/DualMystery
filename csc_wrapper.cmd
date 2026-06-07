@echo off
set CSC_DLL=C:\Program Files\dotnet\sdk\10.0.300\Roslyn\bincore\csc.dll
if not exist "%CSC_DLL%" exit /b 1
dotnet exec "%CSC_DLL%" %*
