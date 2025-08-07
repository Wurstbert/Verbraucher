# Putty muss installiert sein und in Path stehen (wird bei Installation normalerweise gleich reingeschrieben); Achtung, VS nach Installation neustarten!

# https://devcodef1.com/news/1234472/blazor-on-raspberry-pi-with-nginx-and-kestrel
# https://codedbeard.com/iot-with-blazor-on-raspberry-pi-part-2/
# https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/linux-nginx?view=aspnetcore-9.0&tabs=linux-ubuntu


dotnet publish --no-build Verbraucher.csproj --configuration Debug /p:PublishProfile=FolderProfile.pubxml

$config = Get-Content "E:\Lager\secrets.json" -Raw | ConvertFrom-Json

$user     = $config.user
$password = $config.password
$target   = $config.target

$commands = "rm -rf /home/wursti/Public/Verbraucher/*" #/home/wursti/.dotnet/dotnet /home/wursti/Public/Verbraucher/Verbraucher.dll;
plink $target -l $user -pw $password -batch "$commands"

ping raspberrypi.local
#echo "$user@${target}:/home/wursti/Public/Verbraucher"
pscp  -pw $password -r E:\Lager\Verbraucher\bin\Debug\net9.0\publish\* "$user@${target}:/home/wursti/Public/Verbraucher"

