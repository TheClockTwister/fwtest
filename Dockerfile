FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0 AS builder
RUN apt-get update && apt-get install -y build-essential zlib1g-dev && apt-get clean -y
WORKDIR /work
COPY *.csproj .
RUN dotnet restore
COPY *.cs .
RUN dotnet publish -c Release

# FROM scratch
# COPY --from=builder /work/bin/Release/*/*/publish .

FROM debian:bookworm-slim AS packer
WORKDIR /work
RUN apt-get update && \
    apt-get install -y dpkg-dev && \
    apt-get clean && \
    mkdir -p /export
COPY control /package/DEBIAN/
RUN sed -i "s/{{ARCH}}/$(dpkg --print-architecture)/g" /package/DEBIAN/control

COPY --from=builder /work/bin/Release/*/*/publish .
RUN mkdir -p  /package/usr/bin/ && \
    mv fwtest /package/usr/bin/

RUN dpkg-deb --build /package && dpkg-name /package.deb

FROM scratch
COPY --from=packer /*.deb .
