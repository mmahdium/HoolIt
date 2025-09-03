FROM mcr.microsoft.com/dotnet/sdk:10.0.100-preview.7-alpine3.22-aot AS build

# Install NativeAOT build prerequisites
RUN apk update \
    && apk add --no-cache \
       clang zlib-dev 

WORKDIR /source

COPY . .
RUN dotnet publish -r linux-musl-x64 -o /app 'HoolIt.csproj'

FROM mcr.microsoft.com/dotnet/runtime-deps:10.0.0-preview.7-alpine3.22
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["/app/HoolIt"]