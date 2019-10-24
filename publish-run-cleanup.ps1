$publish_folder_name = 'publish'

dotnet publish -c Release -v q -o ./$publish_folder_name/

. ./$publish_folder_name/BenchApp.exe

rm -Force -Recurse ./$publish_folder_name/