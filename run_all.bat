Gplex.exe kompilator.lex
Gppg.exe /gplex kompilator.y > parser.cs
dotnet build
.\bin\Debug\compiler2021.exe .\stuff\plik.txt
.\llvm_tools\lli.exe .\stuff\plik.txt.ll
