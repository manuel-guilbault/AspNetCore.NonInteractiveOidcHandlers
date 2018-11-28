param(
	[string] $BuilderNumber
)

If ($BuilderNumber -match "^\d+\.\d+\.\d+\.\d+$") {
	$version = $BuilderNumber
} Else {
	$version = $BuilderNumber -replace "^.+\.(\d+)$", '1.0.0.$1'
}
$buildRevision = $BuilderNumber.Split(".")[3]
Write-Host "##vso[task.setvariable variable=Version]$version"
Write-Host "##vso[task.setvariable variable=BuildRevision]$buildRevision"
Write-Host "Computed Version: $version"
Write-Host "Computed BuildRevision: $buildRevision"