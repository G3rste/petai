 $webData = Invoke-WebRequest -Uri "http://masterserver.vintagestory.at/api/v1"; 
 $jsonData = ConvertFrom-Json $webData.content; 
 $jsonData.data.mods | group id | sort count -Descending -First 75| select count, Name | foreach { $line =0 } { $line++; $_ } | ft @{ n = 'line'; e= { $line } }, count, Name