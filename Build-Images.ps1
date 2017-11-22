Param(
    [Parameter(Mandatory=$true)]
    [string] $Repo,

    [Parameter()]
    [string] $Version = '1.0.0-dev'
)

$Components = @(
    'api',
    'sql-executor',
    'provisioning',
    'ui'
)

$docker = Get-Command docker

Write-Host 'Building images...'
ForEach ($Component in $Components) {
    & $docker build -t "${Repo}/${Component}:${Version}" -f Dockerfile.${Component} .
}

Write-Host 'Pushing images...'
ForEach ($Component in $Components) {
    & $docker push "${Repo}/${Component}:${Version}"
}
