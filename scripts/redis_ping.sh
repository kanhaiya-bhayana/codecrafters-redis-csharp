#!/bin/bash

HOST="127.0.0.1"
PORT=6379

response=$(printf '*1\r\n$4\r\nPING\r\n' | nc -w 2 "$HOST" "$PORT")

echo "Response: $response"

if echo "$response" | grep -q '^+PONG'; then
    echo "✅ Redis PING successful"
else
    echo "❌ Redis PING failed"
fi
