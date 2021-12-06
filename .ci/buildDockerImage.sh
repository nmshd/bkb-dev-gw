#!/bin/bash
set -e
set -u
set -x

docker build --file ./ApiGateway/Dockerfile --tag ghcr.io/nmshd/bkb-gw:${TAG-temp} .
