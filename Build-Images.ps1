Param(
    [Parameter(Mandatory=$true)]
    [string] $Repo,

    [Parameter()]
    [string] $Version = '1.0.0-dev',

    [Parameter()]
    [switch] $Deploy
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

If ($Deploy) {
    $kubectl = Get-Command kubectl
    $manifestDirectory = Join-Path $PSScriptRoot 'deploy\k8s'

    Write-Host 'Deploying application...'
    ForEach ($Component in $Components) {
        $manifestFile = Join-Path $manifestDirectory "daas-$Component.yml"

        & $kubectl delete -f $manifestFile
        & $kubectl create -f $manifestFile
    }
}
