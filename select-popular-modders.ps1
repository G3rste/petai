$webData = Invoke-WebRequest -Uri "https://mods.vintagestory.at/api/mods"; 
$jsonData = ConvertFrom-Json $webData.content; 
$jsonData.mods | Select-Object author, downloads | Group-Object -Property author | %{
    New-Object psobject -Property @{
        author = $_.Name
        Downloads = ($_.Group | Measure-Object downloads -Sum).Sum
    }
} | Sort-Object Downloads -Descending | ForEach-Object { $line =0 } { $line++; $_ } | ft @{ n = 'line'; e= { $line } }, author, Downloads 