
# `fwtest` Firewall tester
A tool for evaluating NAT and filter rules of a firewall from both sides.

> No description present because the audience is quite small.
> If you want to use this and need a description, feel free
> to open an issue and I will add a description :)

## Building
```bash
mkdir -p ./out && rm -r ./out/*
docker buildx build --output type=local,dest=./out --platform=amd64,arm64,i386,armhf,s390x .
for dir in ./out/*/; do mv "$dir"*.deb ./out; rmdir "$dir"; done
```
