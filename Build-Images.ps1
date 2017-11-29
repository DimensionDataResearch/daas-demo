Param(
    [Parameter(Mandatory=$true)]
    [string] $Repo,

    [Parameter()]
    [string[]] $OnlyComponents = $null,

    [Parameter()]
    [string] $Version = '1.0.0-dev',

    [Parameter()]
    [switch] $Deploy
)

$Components = $OnlyComponents
If (!$Components) {
    $Components = @(
        'api',
        'database-proxy',
        'provisioning',
        'ui'
    )
}

$docker = Get-Command docker

Write-Host 'Building images...'
ForEach ($Component in $Components) {
    & $docker build -t "${Repo}/${Component}:${Version}" -f Dockerfile.${Component} .
    If ($LASTEXITCODE) {
        Return $LASTEXITCODE
    }
}

Write-Host 'Pushing images...'
ForEach ($Component in $Components) {
    & $docker push "${Repo}/${Component}:${Version}"

    If ($LASTEXITCODE) {
        Return $LASTEXITCODE
    }
}

If ($Deploy) {
    $kubectl = Get-Command kubectl
    $manifestDirectory = Join-Path $PSScriptRoot 'deploy\k8s'

    Write-Host 'Deploying application...'
    ForEach ($Component in $Components) {
        $manifestFile = Join-Path $manifestDirectory "daas-$Component.yml"

        & $kubectl delete -f $manifestFile
        # It's fine if the target doesn't exist before we try to delete it

        & $kubectl create -f $manifestFile
        If ($LASTEXITCODE) {
            Return $LASTEXITCODE
        }
    }
}
