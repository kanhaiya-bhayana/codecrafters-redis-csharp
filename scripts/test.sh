#!/bin/bash

HOST="127.0.0.1"
PORT=6379

while true; do
    read -rp "redis> " line

    [[ "$line" == "exit" ]] && break

    read -ra parts <<< "$line"

    argc=${#parts[@]}

    resp="*${argc}\r\n"

    for arg in "${parts[@]}"; do
        resp+="\$${#arg}\r\n${arg}\r\n"
    done

    printf "%b" "$resp" | nc 127.0.0.1 6379
    echo
done

