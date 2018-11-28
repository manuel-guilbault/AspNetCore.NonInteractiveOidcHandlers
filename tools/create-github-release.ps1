param(
	[string] $Owner,
	[string] $Repository,
	[string] $PAT,
	[string] $Version,
	[string] $Commit,
	[string[]] $Assets
)

[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12;

$basicToken = [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes("$($Owner):$PAT"))
$headers = @{
	Accept = "application/vnd.github.v3+json";
	Authorization = "Basic $basicToken";
}

# Create a draft release
$release = Invoke-RestMethod -Method Post -Uri "https://api.github.com/repos/$Owner/$Repository/releases" -Headers $headers -ContentType "application/json" -Body (@{
	tag_name = "v$Version";
	target_commitish = $Commit;
	name = "v$Version";
	draft = $true;
	prerelease = $Version.Contains("-");
} | ConvertTo-Json)

# Upload release's assets
foreach ($asset in $Assets) {
	$assetName = Split-Path $asset -leaf
	Invoke-RestMethod -Method Post -Uri "https://uploads.github.com/repos/$Owner/$Repository/releases/$($release.id)/assets?name=$assetName" -Headers $headers -ContentType "application/octet-stream" -InFile $asset
}

# Publish release
Invoke-RestMethod -Method Patch -Uri "https://api.github.com/repos/$Owner/$Repository/releases/$($release.id)" -Headers $headers -ContentType "application/json" -Body (@{
	draft = $false;
} | ConvertTo-Json)