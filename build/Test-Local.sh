#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."
: "${ACADEMIC_JAVA:?Point ACADEMIC_JAVA at a Java 17+ executable}"
: "${ACADEMIC_NLP_JAR:?Point ACADEMIC_NLP_JAR at the built sidecar jar}"
dotnet restore AcademicParaphraser.Engine.sln
for configuration in Debug Release; do
 dotnet build AcademicParaphraser.Engine.sln -c "$configuration" --no-restore -warnaserror
 dotnet test tests/AcademicParaphraser.Tests/AcademicParaphraser.Tests.csproj -c "$configuration" --no-build --logger "trx;LogFileName=$configuration.trx" --results-directory artifacts/tests
done
