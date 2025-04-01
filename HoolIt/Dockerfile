FROM mcr.microsoft.com/dotnet/sdk:10.0.100-preview.2-alpine3.21 AS build

# Install NativeAOT build prerequisites
RUN apk update \
    && apk add --no-cache \
       clang zlib-dev 

WORKDIR /source

COPY . .
RUN cd 'HoolIt' && dotnet publish -r linux-musl-x64 -o /app 'HoolIt.csproj'

FROM mcr.microsoft.com/dotnet/runtime-deps:10.0.0-preview.2-alpine3.21
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["/app/HoolIt"]