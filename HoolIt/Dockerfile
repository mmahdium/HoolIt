FROM mcr.microsoft.com/dotnet/dotnet/sdk:10.0-alpine3.23-aot AS build

# Install NativeAOT build prerequisites
RUN apk update \
    && apk add --no-cache \
       clang zlib-dev 

WORKDIR /source

COPY . .
RUN dotnet publish -r linux-musl-x64 -o /app 'HoolIt.csproj'

FROM mcr.microsoft.com/dotnet/runtime-deps:10.0-alpine3.23
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["/app/HoolIt"]