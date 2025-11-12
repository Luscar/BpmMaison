#!/bin/bash

echo "Nettoyage..."
dotnet clean

echo -e "\nRestauration des packages..."
dotnet restore

echo -e "\nCompilation..."
dotnet build -c Release

echo -e "\nCréation du package NuGet..."
dotnet pack -c Release -o ./nupkg

echo -e "\nPackage créé avec succès!"
echo "Emplacement: ./nupkg/"
