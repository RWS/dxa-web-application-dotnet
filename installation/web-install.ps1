<#
.SYNOPSIS
   Deploys the DXA .NET Web Application on SDL Web 8 (CDaaS)
.EXAMPLE
   .\web-install.ps1 -distDestination "C:\inetpub\wwwroot\DXA_Staging" -webName "DXA Staging" -sitePort 8888 -discoveryServiceUrl http://localhost:8082/discovery.svc
#>

[CmdletBinding( SupportsShouldProcess=$true, PositionalBinding=$false)]
Param(
    #File system path of the root folder of DXA Website
    [Parameter(Mandatory=$true, HelpMessage="File system path of the root folder of DXA Website")]
    [string]$distDestination,

    #Specifies whether the web application should be configured for 'Staging' (XPM-enabled) or 'Live' (performance optimized).
    [Parameter(Mandatory=$false)]
    [ValidateSet("Staging", "Live")]
    [string]$deployType = "Staging",

    #Name of the DXA Website
    [Parameter(Mandatory=$false)]
    [string]$webName = "DXA",

    #Host header of DXA Website used in configs. Specify empty string to use current computer name.
    [Parameter(Mandatory=$false)]
    [string]$siteDomain = "",

    #Port for DXA Website
    [Parameter(Mandatory=$true, HelpMessage="Port for DXA Website")]
    [int]$sitePort,

    #Path to the log directory
    [Parameter(Mandatory=$false)]
    [string]$logFolder = "C:\temp\logs",

    #The logging level (ERROR,WARN,INFO,DEBUG,TRACE in order of increasing verbosity) for the DXA log file. Defaults to INFO.
    [Parameter(Mandatory=$false)]
    [ValidateSet( "ERROR", "WARN", "INFO", "DEBUG", "TRACE")]
    [string]$logLevel = "INFO",

    #Log file name
    [Parameter(Mandatory=$false)]
    [string]$siteLogFile = "site.log",

    #Action to perform when DXA Website already exists: 'Recreate', 'Preserve', 'Cancel' or 'Ask' (default)
    [Parameter(Mandatory=$false)]
    [ValidateSet("Recreate", "Preserve", "Cancel", "Ask")]
    [string]$webSiteAction = "Ask",

    #URL of the Discovery Service (CIS)
    #Note that the URL should include '/discovery.svc' if the Discovery Service is deployed as standalone service.
    [Parameter(Mandatory=$true, HelpMessage="URL of the Discovery Service")]
    [string]$discoveryServiceUrl,

    #OAuth Client ID (set to empty if OAuth is not used)
    [Parameter(Mandatory=$false)]
    [string]$oAuthClientId = "cduser",

    #OAuth Client Secret
    [Parameter(Mandatory=$false)]
    [string]$oAuthClientSecret = "CDUserP@ssw0rd",

    #Exclude Core Module from installation
    [Parameter(Mandatory=$false)]
    [switch]$noCoreModule = $false,

    #The type of Navigation Provider to use. Can be 'Static' or 'Dynamic'.
    [Parameter(Mandatory=$false)]
    [ValidateSet("Static", "Dynamic")]
    [string]$navigationProvider = "Static",

    #If specified, configure Distributed Caching with given Redis cache server. Format: 'hostname' or 'hostname:port'
    [Parameter(Mandatory=$false)]
    [string]$redisCacheServer
)

#Terminate script on first occurred exception
$ErrorActionPreference = "Stop"

#Process 'WhatIf' and 'Confirm' options
if (!($pscmdlet.ShouldProcess($distDestination, "Deploy DXA .NET Web Application"))) { return }

#Initialization
$distSource = Split-Path $MyInvocation.MyCommand.Path

$DomainName = (Get-WmiObject -Class Win32_ComputerSystem).Domain
$FullComputerName = $env:computername
if (![string]::IsNullOrEmpty($DomainName))
{
    $FullComputerName = $FullComputerName + "." + $DomainName
}

if (!$siteDomain) {
    $siteDomain = $FullComputerName
    $siteHeader = ""
} else {
    $siteHeader = $siteDomain
}

#Format data
$distSource = $distSource.TrimEnd("\")
$distDestination = $distDestination.TrimEnd("\")
$siteLogFile = Join-Path $logFolder $siteLogFile
$siteDomain = $siteDomain.ToLower()

#Set web site
Write-Host "Setting web site and web application..."
Import-Module "WebAdministration"
$webSite = Get-Item IIS:\Sites\$webName -ErrorAction SilentlyContinue
if ($webSite) {
    $recreate = New-Object System.Management.Automation.Host.ChoiceDescription "&Recreate", "Delete old web site and create new with specified parameters."
    $preserve = New-Object System.Management.Automation.Host.ChoiceDescription "&Preserve", "Use existing web site for web application deployment."
    $cancel = New-Object System.Management.Automation.Host.ChoiceDescription "&Cancel", "Cancel setup."
    $RecreatePreserveCancelOptions = [System.Management.Automation.Host.ChoiceDescription[]]($recreate, $preserve, $cancel)
    $choice = 1
    if ($webSiteAction -eq 'Ask') {
        $choice = $host.UI.PromptForChoice("Warning", "Web Site '$webName' already exists. Select 'Recreate' to overwrite website. Select 'Preserve' to use existing website. Select 'Cancel' to cancel setup.", $RecreatePreserveCancelOptions, 1)
    } else {
        $actionChoices = @{"Recreate"=0;"Preserve"=1;"Cancel"=2}
        $choice = $actionChoices[$webSiteAction]
    }
    if ($choice -eq 2) {
        Write-Host "Setup was canceled because Web Site '$webName' already exists."
        return
    }
    if ($choice -eq 0) {
        Write-Host "Recreating website..."
        $appPool = Get-Item IIS:\AppPools\$webName -ErrorAction SilentlyContinue
        if($appPool) { 
            $appPool.Stop()
            while (-not ($appPool.state -eq "Stopped")) { Start-Sleep -Milliseconds 100 }
        }
        Remove-Item IIS:\Sites\$webName -Recurse
        if (Test-Path $distDestination) {
            Remove-Item $distDestination -Recurse -Force
        }
        New-Item IIS:\Sites\$webName -Bindings @{protocol="http";bindingInformation=":"+$sitePort+":"+$siteHeader} -PhysicalPath $distDestination
    }
    if ($choice -eq 1) {
        Write-Host "Using existing website..."
        $sitePort = $webSite.bindings.Collection[0].bindingInformation.Split(":")[1]
        $siteHeader = $webSite.bindings.Collection[0].bindingInformation.Split(":")[2]
        $distDestination = $webSite.physicalPath.TrimEnd("\")
    }
    if ($siteHeader) {
        $siteDomain = $siteHeader.ToLower()
    }
} else {
    New-Item IIS:\Sites\$webName -Bindings @{protocol="http";bindingInformation=":"+$sitePort+":"+$siteHeader} -PhysicalPath $distDestination
}

#Copy web application files
Write-Host "Copying web application files to '$distDestination' ..."
if (!(Test-Path $distDestination)) {
    New-Item -ItemType Directory -Path $distDestination | Out-Null
}
Copy-Item $distSource\dist\* $distDestination -Recurse -Force
Copy-Item $distSource\web-ref\* $distDestination\bin -Recurse -Force

#Set Application Pool
Write-Host "Setting application pool '$webName' ..."
$appPool = Get-Item IIS:\AppPools\$webName -ErrorAction SilentlyContinue
if(!$appPool) {
    $appPool = New-Item IIS:\AppPools\$webName
}
$appPool.managedRuntimeVersion = "v4.0" #v2.0
$appPool.managedPipelineMode = 0 #0 - Integrated, 1 - Classic
$appPool.processModel.loadUserProfile = $true
$appPool.processModel.identityType = "NetworkService"
$appPool | Set-Item
Set-ItemProperty IIS:\Sites\$webName -Name applicationPool -value $webName
$appPool.Start()

#Set folder permissions
Write-Host "Setting folder permissions on '$distDestination' ..."
$Acl = Get-Acl $distDestination
$permission = "NetworkService" ,"FullControl","ContainerInherit,ObjectInherit","None","Allow"
$accessRule = New-Object System.Security.AccessControl.FileSystemAccessRule $permission    
$Acl.SetAccessRule($accessRule)
Set-Acl $distDestination $Acl

# Update Log.config
$logConfigFile = "$distDestination\Log.config"
Write-Host ("Updating '$logConfigFile' ...")
[xml]$logConfig = Get-Content $logConfigFile -ErrorAction Stop
$appenderNode = $logConfig.log4net.appender | ?{$_.name -eq "RollingFile"}
if ($appenderNode) 
{ 
    $appenderNode.file.SetAttribute("value", $siteLogFile)
    Write-Host "Set log file location to '$siteLogFile'" 
}
$logLevelNode = $logConfig.log4net.root.level
if ($logLevelNode)
{
    $logLevelNode.value = $logLevel
    Write-Host "Set log level '$logLevel'"
}
$logConfig.Save($logConfigFile)

# Update Web.config
function Set-AppSetting([string]$key, [string]$value)
{
    $appSettingsNode = $config.configuration.appSettings

    $appSettingNode = $appSettingsNode.SelectSingleNode("add[@key='$key']")
    if (!$appSettingNode) {
        $appSettingNode = $config.CreateElement("add")
        $appSettingNode.SetAttribute("key", "$key")
        $dummy = $appSettingsNode.AppendChild($appSettingNode)
    }
    $appSettingNode.SetAttribute("value", $value)
    Write-Host "Set app setting '$key' to '$value'"
}

function Remove-Module($name)
{
    $modulesElement = $config.SelectSingleNode("/configuration/system.webServer/modules")
    if ($modulesElement)
    {
        $moduleElement = $modulesElement.SelectSingleNode("add[@name='$name']")
        if ($moduleElement)
        {
            $dummy = $modulesElement.RemoveChild($moduleElement)
        }
        else
        {
            Write-Verbose "No element found for module '$name'."
        }

    }
    else
    {
        Write-Warning "/configuration/system.webServer/modules element not found."
    }
}

function Get-CilCacheSection()
{
    $cilCacheSectionName = "sdl.web.delivery/caching"
    $cilCacheSection = $config.SelectSingleNode("/configuration/$cilCacheSectionName")
    if (!$cilCacheSection)
    {
        throw "Section '$cilCacheSectionName' not found in configuration."
    }
    return $cilCacheSection
}

function Set-CacheExpiration($cacheName, $expiresInSeconds)
{
    $cilCacheSection = Get-CilCacheSection

    $cacheHandlerElement = $cilCacheSection.SelectSingleNode("handlers/add[@name='$cacheName']")
    if (!$cacheHandlerElement)
    {
        Write-Warning "Cache Handler named '$cacheName' not found in configuration."
        return
    }
    $cacheHandlerElement.policy.SetAttribute("absoluteExpiration", $expiresInSeconds)
    Write-Host "Set cache expiration for '$cacheName' to $expiresInSeconds seconds."
}


$webConfigFile = "$distDestination\Web.config"
Write-Host "Updating '$webConfigFile' ..."
[xml]$config = Get-Content $webConfigFile -ErrorAction Stop

Set-AppSetting "discovery-service-uri" $discoveryServiceUrl
Set-AppSetting "log-output" "$logFolder\cd_client.log"
Set-AppSetting "log-level" $logLevel

if ($oAuthClientId)
{
    Set-AppSetting "oauth-enabled" true
    Set-AppSetting "oauth-client-id" $oAuthClientId
    Set-AppSetting "oauth-client-secret" $oAuthClientSecret
}
else
{
    Set-AppSetting "oauth-enabled" false
    Set-AppSetting "oauth-client-id" ''
    Set-AppSetting "oauth-client-secret" ''
}

Write-Host "Deploy type: '$deployType'"
if ($deployType -eq "Staging")
{
    Set-CacheExpiration "regularCache" 5
    Set-CacheExpiration "regularDistributedCache" 5
    Set-CacheExpiration "longLivedCache" 30
    Set-CacheExpiration "longLivedDistributedCache" 30
    Set-AppSetting "DD4T.CacheSettings.Default" 5
}
else
{
    Set-CacheExpiration "regularCache" 300
    Set-CacheExpiration "regularDistributedCache" 300
    Set-CacheExpiration "longLivedCache" 900
    Set-CacheExpiration "longLivedDistributedCache" 900
    Set-AppSetting "DD4T.CacheSettings.Default" 300

    # Remove ADF HTTP module
    Remove-Module "AmbientFrameworkModule"
}

if ($redisCacheServer)
{
    $cilCacheSection = Get-CilCacheSection

    # Default Handler covers CIL internal caching
    $cilCacheSection.defaultHandler = "regularDistributedCache"

    # Use Distributed Caching for all but DD4T cache Regions
    $cilCacheSection.SelectNodes("regions/add[@cacheName='regularCache' and @name!='Page' and @name!='ComponentPresentation']") | ForEach-Object { $_.cacheName = "regularDistributedCache" }
    $cilCacheSection.SelectNodes("regions/add[@cacheName='longLivedCache']") | ForEach-Object { $_.cacheName = "longLivedDistributedCache" }

    $redisCacheServerParts = $redisCacheServer -split ':'
    $redisHost = $redisCacheServerParts[0]
    if ($redisCacheServerParts.Length -gt 1)
    {
        $redisPort = $redisCacheServerParts[1]
    }
    else
    {
        $redisPort = "6379" # Default Redis port
    }

    $cilCacheSection.SelectNodes("handlers/add[@type='RedisCacheHandler']") | ForEach-Object { 
        $_.endpoint.host = $redisHost
        $_.endpoint.port = $redisPort 
        }

    Write-Host "Configured Distributed Caching with Redis Cache Server '$redisHost' on port $redisPort."
}

$config.Save($webConfigFile)

#Update Unity.config
function Set-UnityTypeMapping([string] $type, [string] $mapTo, [xml] $configDoc) 
{
	$mainContainer = $configDoc.unity.containers.container | ? {$_.name -eq "main"}
	if (!$mainContainer) 
	{
        throw "Main container not found."
    }

	$typeElement = $mainContainer.types.type | ? {$_.type -eq $type}
	if ($typeElement)
    {
        Write-Host "Found existing type mapping: '$type' -> '$mapTo'"
    }
    else
	{
		$typeElement = $configDoc.CreateElement("type")
		$mainContainer.types.AppendChild($typeElement) | Out-Null
	}

	$typeElement.SetAttribute("type",$type)
	$typeElement.SetAttribute("mapTo",$mapTo)

    Write-Host "Set type mapping: '$type' -> '$mapTo'"
}

if ($navigationProvider -ne "Static")
{
    $unityConfigFile = "$distDestination\Unity.config"
    Write-Host "Updating '$unityConfigFile' ..."
    [xml]$unityConfigDoc = Get-Content $unityConfigFile -ErrorAction Stop
    Set-UnityTypeMapping "INavigationProvider" "$($navigationProvider)NavigationProvider" $unityConfigDoc
    $unityConfigDoc.Save($unityConfigFile)
}

if(!$noCoreModule)
{
    $coreModuleInstallPath = Join-Path $distSource "..\modules\Core\web-install.ps1"
    & $coreModuleInstallPath -distDestination $distDestination
}

Write-Host "Done."
