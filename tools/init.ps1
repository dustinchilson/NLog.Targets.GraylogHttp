if ($env:APPVEYOR_REPO_BRANCH -ne "master")
{
	Update-AppveyorBuild -Version "$($env:APPVEYOR_REPO_COMMIT.substring(0,7))"
}