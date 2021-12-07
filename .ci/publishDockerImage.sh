#!/bin/bash
set -e
set -u
set -x

docker push ghcr.io/nmshd/bkb-dev-gw:${TAG}
