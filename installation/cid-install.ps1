<#
.SYNOPSIS
   Enables the CID service for the DXA .NET Web Application on SDL Web 8 (CDaaS)
.EXAMPLE
   .\cid-install.ps1 -distDestination "C:\inetpub\wwwroot\DXA_Staging" -Verbose
#>

[CmdletBinding( SupportsShouldProcess=$true, PositionalBinding=$false)]
Param(
    #File system path of the root folder of DXA Website.
    [Parameter(Mandatory=$true, HelpMessage="File system path of the root folder of DXA Website")]
    [string]$distDestination,
   
    #Pattern to match CID Service proxy requests against and forward to CID service (default /cid*).
    [Parameter(Mandatory=$false, HelpMessage="Specify pattern for matching CID service proxy requests")]
    [string]$cidProxyPattern = "/cid*",

    #Provide a remapping for localhost allowing the CID service to request resources correctly. If this
    #is not specified DXA will attempt a DNS lookup automatically and replace localhost with DNS name.
    [Parameter(Mandatory=$false, HelpMessage="Specify localhost mapping for CID service")]
    [string]$localhostMapping
)

function GetOrCreate-Node([string]$path)
{
    $node = $config.SelectSingleNode($path)
    if(!$node)
    {
        $parts = $path.Split("/", [System.StringSplitOptions]::RemoveEmptyEntries)
        $path = ""        
        $parent = $config
        foreach($part in $parts)
        {
            $path += "/$part"
            $node = $config.SelectSingleNode($path)
            if(!$node)
            {
                $node = $config.CreateElement($part)
                $parent.AppendChild($node) | Out-Null
            }
            $parent = $node
        }        
    }
    return $node
}

function Set-Attribute([string]$path, [string]$attributeName, [string]$attributeValue)
{   
    $node = GetOrCreate-Node($path)
    $node.SetAttribute($attributeName, $attributeValue)    
}

function Set-AppSetting([string]$key, [string]$value)
{
    $appSettingsNode = GetOrCreate-Node("/configuration/appSettings")
    $appSettingNode = $appSettingsNode.SelectSingleNode("add[@key='$key']")
    if (!$appSettingNode) 
    {
        $appSettingNode = $config.CreateElement("add")
        $appSettingNode.SetAttribute("key", "$key")
        $appSettingsNode.AppendChild($appSettingNode) | Out-Null
    }
    $appSettingNode.SetAttribute("value", $value)
}

function Remove-AppSetting([string]$key)
{
    $appSettingsNode = GetOrCreate-Node("/configuration/appSettings")
    $appSettingNode = $appSettingsNode.SelectSingleNode("add[@key='$key']")
    if ($appSettingNode) 
    {
        $appSettingNode.ParentNode.RemoveChild($appSettingNode) | Out-Null
    }
}

function Set-UnityTypeMapping([string]$type, [string]$mappingValue)
{
    Write-Host "Adding unity type mapping for $type to $mappingValue"
    $node = $config.SelectSingleNode("/unity/containers/container/types/type[@type='$type']")
    if (!$node) 
    {
        $mapping = GetOrCreate-Node("/unity/containers/container/types")
        $node = $config.CreateElement("type")
        $node.SetAttribute("type", $type) 
        $child = $config.CreateElement("lifetime");
        $child.SetAttribute("type", "singleton");       
        $node.AppendChild($child) | Out-Null
        $mapping.AppendChild($node) | Out-Null   
    }
    $node.SetAttribute("mapTo", $mappingValue)
}

function Add-Module([string]$name, [string]$type)
{
    Write-Host "Adding module: $name"
    $modulesNode = GetOrCreate-Node("/configuration/system.webServer/modules")
    $moduleNode = $modulesNode.SelectSingleNode("add[@name='$name']")
    if (!$moduleNode) 
    {      
        $moduleNode = $config.CreateElement("add")
        $moduleNode.SetAttribute("name", "$name")        
        $modulesNode.AppendChild($moduleNode) | Out-Null
    }
    $moduleNode.SetAttribute("type", $type)
}

# Update Unity.config
$unityConfigFile = "$distDestination\Unity.config"
Write-Host ("Updating '$unityConfigFile' ...")
[xml]$config = Get-Content $unityConfigFile -ErrorAction Stop
Set-UnityTypeMapping "IMediaHelper" "ContextualMediaHelper"
$config.Save("$unityConfigFile")

# Update Web,config
$webConfigFile = "$distDestination\Web.config"
Write-Host "Updating '$webConfigFile' ..."
[xml]$config = Get-Content $webConfigFile -ErrorAction Stop

# Set to CID proxy pattern matching - We must set this !
Write-Host "Using service proxy pattern: '$cidProxyPattern'"
Set-AppSetting "cid-service-proxy-pattern" $cidProxyPattern

# Set the local host mapping
if ($localhostMapping) 
{
    Write-Host "Using localhost mapping: '$localhostMapping'"
    Set-AppSetting "cid-localhost" $localhostMapping
}
else 
{    
    # Removal of this setting will let DXA automatically perform a DNS lookup
    # and try to map it for us
    Remove-AppSetting "cid-localhost"
}

# Add the CID proxy module that will respond to requests matching the cidProxyPattern
Add-Module "ContextualImageProxyModule" "Sdl.Web.Context.Image.Proxy.ContextualImageProxyModule"

# We need this at the moment so our CID proxy can be passed a port number
Set-Attribute "/configuration/system.web/httpRuntime" "requestPathInvalidCharacters" "<,>,*,%,&,?"

# Save Web.config
$config.Save("$webConfigFile")

Write-Host "Done."
