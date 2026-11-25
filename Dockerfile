# Stage 1: Build and Publish
FROM mcr.microsoft.com/dotnet/sdk:11.0 AS build
WORKDIR /src

# Copy directory configuration files for central package management and build props
COPY Directory.Build.props Directory.Packages.props ./
COPY src/Directory.Build.props src/Directory.Packages.props src/
COPY src/Documentation.Exporter.Abstractions/Documentation.Exporter.Abstractions.csproj src/Documentation.Exporter.Abstractions/
COPY src/Documentation.Exporter.Core/Documentation.Exporter.Core.csproj src/Documentation.Exporter.Core/
COPY src/Documentation.Exporter.Cli/Documentation.Exporter.Cli.csproj src/Documentation.Exporter.Cli/
COPY src/Documentation.Exporter.Plugins.Diagrams/Documentation.Exporter.Plugins.Diagrams.csproj src/Documentation.Exporter.Plugins.Diagrams/
COPY src/Documentation.Exporter.Plugins.Navigation.MkDocs/Documentation.Exporter.Plugins.Navigation.MkDocs.csproj src/Documentation.Exporter.Plugins.Navigation.MkDocs/

# Restore dependencies
RUN dotnet restore src/Documentation.Exporter.Cli/Documentation.Exporter.Cli.csproj

# Copy remaining source code
COPY src/Documentation.Exporter.Abstractions/ src/Documentation.Exporter.Abstractions/
COPY src/Documentation.Exporter.Core/ src/Documentation.Exporter.Core/
COPY src/Documentation.Exporter.Cli/ src/Documentation.Exporter.Cli/
COPY src/Documentation.Exporter.Plugins.Diagrams/ src/Documentation.Exporter.Plugins.Diagrams/
COPY src/Documentation.Exporter.Plugins.Navigation.MkDocs/ src/Documentation.Exporter.Plugins.Navigation.MkDocs/

# Optional SemVer override for containerized builds where .git/ is excluded by .dockerignore
ARG MINVERVERSIONOVERRIDE=""
ENV MINVERVERSIONOVERRIDE=${MINVERVERSIONOVERRIDE}

# Publish Release build into the root build/ artifacts directory
RUN dotnet publish src/Documentation.Exporter.Cli/Documentation.Exporter.Cli.csproj \
    -c Release \
    --no-restore

# Stage 2: Lean Linux Runtime
FROM mcr.microsoft.com/dotnet/runtime:11.0 AS final
WORKDIR /workspace

# Copy published application binaries from the centralized build/publish directory
COPY --from=build /src/build/publish/Documentation.Exporter.Cli/release /app

ENTRYPOINT ["/app/Documentation.Exporter.Cli"]
