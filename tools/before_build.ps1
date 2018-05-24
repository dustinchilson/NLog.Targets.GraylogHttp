if(Test-Path .\artifacts) { 
	Remove-Item .\artifacts -Force -Recurse 
}

dotnet restore src\NLog.Targets.GraylogHttp\NLog.Targets.GraylogHttp.csproj