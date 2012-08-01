.\Nuget.exe update -self
.\Nuget.exe pack ..\src\DataModel.Core\DataModel.Core.csproj -Prop Configuration=Release -Symbols -Build
