
for /f "delims=" %%A in ('cd') do ( set WHERE=%%~nxA )
set WHERE=%WHERE: =%
echo %WHERE%
"D:\Program Files\7-Zip\7z.exe" a -y -tzip %WHERE%.zip "%cd%\bin\Debug\netstandard2.0\%WHERE%.dll" "%cd%\README.md" "%cd%\icon.png" "%cd%\manifest.json"  
