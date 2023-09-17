command=""
for configX in $(basename -s .json -a $(dir configs/*.json))
do
  if [ -n $command ]; then
    command="dotnet ScraperService.dll $HOST $configX" 
  else
    command="${command} & dotnet ScraperService.dll $HOST $configX"
  fi
done
eval $command