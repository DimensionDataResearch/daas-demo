Param(
    [Parameter(Mandatory=$true)]
    [string] $Repo,

    [Parameter()]
    [string[]] $OnlyComponents = $null,

    [Parameter()]
    [string] $Version = '1.0.0-dev',

    [Parameter()]
    [switch] $Deploy,

    [Parameter()]
    [switch] $NoPush
)

$ErrorActionPreference = 'Stop'

$VersionParts = $Version.Split('-')
$VersionPrefix = $VersionParts[0]
If ($VersionParts.Count -eq 2) {
    $VersionSuffix = $VersionParts[1]
} Else {
    $VersionSuffix = ''
}

$Components = $OnlyComponents
If (!$Components) {
    $Components = @(
        'identity-server',
        'api',
        'database-proxy',
        'provisioning',
        'ui'
    )
}

$docker = Get-Command docker

Write-Host 'Building images...'
ForEach ($Component in $Components) {
    Write-Host "Building image for $Component..."
    & $docker build -t "${Repo}/${Component}:${Version}" --build-arg "VERSION_PREFIX=$VersionPrefix" --build-arg "VERSION_SUFFIX=$VersionSuffix" -f Dockerfile.${Component} .
    If ($LASTEXITCODE) {
        Return $LASTEXITCODE
    }
}

If (!$NoPush) {
    Write-Host 'Pushing images...'
    ForEach ($Component in $Components) {
        Write-Host "Pushing image for $Component..."
        & $docker push "${Repo}/${Component}:${Version}"
    
        If ($LASTEXITCODE) {
            Return $LASTEXITCODE
        }
    }
}

If ($Deploy -and !$NoPush) {
    $kubectl = Get-Command kubectl
    $manifestDirectory = Join-Path $PSScriptRoot 'deploy\k8s'

    Write-Host 'Deploying application...'
    ForEach ($Component in $Components) {
        Write-Host "Deploying $Component..."
        $manifestFile = Join-Path $manifestDirectory "daas-$Component.yml"

        & $kubectl delete -f $manifestFile
        # It's fine if the target doesn't exist before we try to delete it

        & $kubectl apply -f $manifestFile
        If ($LASTEXITCODE) {
            Return $LASTEXITCODE
        }
    }
}
