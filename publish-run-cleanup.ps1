$publish_folder_name = 'publish'

dotnet publish -c Release -o $publish_folder_name/

./$publish_folder_name/BenchApp.exe

rm -Force -Recurse ./$publish_folder_name/