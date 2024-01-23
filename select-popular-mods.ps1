 $webData = Invoke-WebRequest -Uri "http://masterserver.vintagestory.at/api/v1"; 
 $jsonData = ConvertFrom-Json $webData.content; 
 $jsonData.data.mods | group id | sort count -Descending| select Name, Count -First 75 | foreach { $line =0 } { $line++; $_ } | ft @{ n = 'line'; e= { $line } }, Count, Name