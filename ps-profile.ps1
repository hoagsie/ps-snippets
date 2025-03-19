$global:BookmarkFile = "$env:USERPROFILE\ps-bookmarks.json"
$global:SnippetFile  = "$env:USERPROFILE\ps-snippets.json"

function Get-HolderData {
    param(
        [Parameter(Mandatory=$true)]
        [string]$Type
    )
    switch ($Type.ToLower()) {
        'marks' {
            if (-not $global:BookmarkMap) {
                if (Test-Path $global:BookmarkFile) {
                    try {
                        $global:BookmarkMap = Get-Content $global:BookmarkFile | ConvertFrom-Json -AsHashTable
                        if (-not $global:BookmarkMap) { $global:BookmarkMap = @{} }
                    }
                    catch {
                        $global:BookmarkMap = @{}
                    }
                }
                else {
                    $global:BookmarkMap = @{}
                }
            }
            return @{ Map = $global:BookmarkMap; File = $global:BookmarkFile }
        }
        'snip' {
            if (-not $global:SnippetMap) {
                if (Test-Path $global:SnippetFile) {
                    try {
                        $global:SnippetMap = Get-Content $global:SnippetFile | ConvertFrom-Json -AsHashTable
                        if (-not $global:SnippetMap) { $global:SnippetMap = @{} }
                    }
                    catch {
                        $global:SnippetMap = @{}
                    }
                }
                else {
                    $global:SnippetMap = @{}
                }
            }
            return @{ Map = $global:SnippetMap; File = $global:SnippetFile }
        }
        default {
            throw "Unknown type '$Type'. Valid types are: bookmarks, snippets."
        }
    }
}

function Save-HolderData {
    param(
        [Parameter(Mandatory=$true)]
        [string]$Type
    )
    switch ($Type.ToLower()) {
        'marks' {
            $global:BookmarkMap | ConvertTo-Json | Set-Content $global:BookmarkFile
        }
        'snip' {
            $global:SnippetMap | ConvertTo-Json | Set-Content $global:SnippetFile
        }
        default {
            throw "Unknown type '$Type'. Valid types are: bookmarks, snippets."
        }
    }
}

function seth {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory=$true, Position=0)]
        [string]$Type,
        [Parameter(Mandatory=$true, Position=1)]
        [string]$Key,
        [Parameter(Mandatory=$true, Position=2)]
        [string]$Value
    )
    # If setting a bookmark, resolve the path to ensure it points to an existing folder or file.
    if ($Type.ToLower() -eq "marks") {
        try {
            $resolved = (Resolve-Path -Path $Value -ErrorAction Stop).ProviderPath
            $Value = $resolved
        }
        catch {
            Write-Error "Could not resolve path: $Value"
            return
        }
    }

    $data = Get-HolderData -Type $Type
    $data.Map[$Key] = $Value
    Save-HolderData -Type $Type
    Write-Host "$Type entry '$Key' set to '$Value'"
}

function remh {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory=$true, Position=0)]
        [string]$Type,
        [Parameter(Mandatory=$true, Position=1)]
        [string]$Key
    )
    $data = Get-HolderData -Type $Type
    if ($data.Map.ContainsKey($Key)) {
        $data.Map.Remove($Key) | Out-Null
        Save-HolderData -Type $Type
        Write-Host "$Type entry '$Key' removed."
    }
    else {
        Write-Host "$Type entry '$Key' not found."
    }
}

function geth {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory=$true, Position=0)]
        [string]$Type,
        [Parameter(Position=1)]
        [string]$Filter
    )
    $data = Get-HolderData -Type $Type
    if ($data.Map.Count -eq 0) {
        Write-Host "No $Type entries defined."
    }
    else {
        # If a filter is provided, use it to match the keys.
        $items = if ($Filter) {
            $data.Map.GetEnumerator() | Where-Object { $_.Key -match $Filter }
        }
        else {
            $data.Map.GetEnumerator()
        }
        foreach ($item in $items) {
            Write-Host "$($item.Key) -> $($item.Value)"
        }
    }
}


Set-PSReadLineOption -PredictionSource HistoryAndPlugin

Import-Module D:\Side\PSBookmarkPredictor\bin\Release\net9.0\PSBookmarkPredictor.dll

Set-PSReadLineOption -PredictionViewStyle ListView
