# Build script pour BpmEngine

Write-Host "Nettoyage..." -ForegroundColor Yellow
dotnet clean

Write-Host "`nRestauration des packages..." -ForegroundColor Yellow
dotnet restore

Write-Host "`nCompilation..." -ForegroundColor Yellow
dotnet build -c Release

Write-Host "`nCréation du package NuGet..." -ForegroundColor Yellow
dotnet pack -c Release -o ./nupkg

Write-Host "`nPackage créé avec succès!" -ForegroundColor Green
Write-Host "Emplacement: ./nupkg/" -ForegroundColor Cyan
