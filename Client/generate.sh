VERSION=0.1.0

docker run --rm -v "${PWD}:/local" --network host -u $(id -u ${USER}):$(id -g ${USER})  openapitools/openapi-generator-cli generate \
-i http://localhost:5025/swagger/v1/swagger.json \
-g csharp-netcore \
-o /local/out --additional-properties=packageName=Coflnet.Sky.PlayerState.Client,packageVersion=$VERSION,licenseId=MIT

cd out
sed -i 's/GIT_USER_ID/Coflnet/g' src/Coflnet.Sky.PlayerState.Client/Coflnet.Sky.PlayerState.Client.csproj
sed -i 's/GIT_REPO_ID/SkyPlayerState/g' src/Coflnet.Sky.PlayerState.Client/Coflnet.Sky.PlayerState.Client.csproj
sed -i 's/>OpenAPI/>Coflnet/g' src/Coflnet.Sky.PlayerState.Client/Coflnet.Sky.PlayerState.Client.csproj

dotnet pack
cp src/Coflnet.Sky.PlayerState.Client/bin/Debug/Coflnet.Sky.PlayerState.Client.*.nupkg ..
