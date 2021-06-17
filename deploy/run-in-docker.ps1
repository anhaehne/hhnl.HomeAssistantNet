$DesktopPath = [System.Environment]::GetFolderPath([System.Environment+SpecialFolder]::Desktop)

docker build ..\. -t c-sharp-for-home-assistant && docker run --rm -it --name test -p 8099:8099 -e SUPERVISOR_TOKEN=$env:SUPERVISOR_TOKEN -e HOME_ASSISTANT_API="http://supervisor/core/api" -v $DesktopPath/data:/data c-sharp-for-home-assistant