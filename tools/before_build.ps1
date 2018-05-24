if(Test-Path .\artifacts) { 
	Remove-Item .\artifacts -Force -Recurse 
}

dotnet restore NLog.Targets.GraylogHttp.sln