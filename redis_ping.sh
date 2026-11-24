#!/bin/bash

HOST="127.0.0.1"
PORT=6379

response=$(nc -w 2 "$HOST" "$PORT")

if echo "$response" | grep -q '^+PONG'; then
    echo "✅ Redis PING successful"
else
    echo "❌ Redis PING failed"
    echo "Response: $response"
fi
