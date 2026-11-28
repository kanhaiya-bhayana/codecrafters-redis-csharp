[![progress-banner](https://backend.codecrafters.io/progress/redis/b307259c-72a5-4ea3-84d7-bdab8e59856b)](https://app.codecrafters.io/users/kanhaiya-bhayana?r=2qF)

This is a starting point for C# solutions to the
["Build Your Own Redis" Challenge](https://codecrafters.io/challenges/redis).

In this challenge, you'll build a toy Redis clone that's capable of handling
basic commands like `PING`, `SET` and `GET`. Along the way we'll learn about
event loops, the Redis protocol and more.

**Note**: If you're viewing this repo on GitHub, head over to
[codecrafters.io](https://codecrafters.io) to try the challenge.

# Passing the first stage

The entry point for your Redis implementation is in `src/Program.cs`. Study and
uncomment the relevant code, then run the command below to execute the tests on
our servers:

```sh
codecrafters submit
```

That's all!

# Stage 2 & beyond

Note: This section is for stages 2 and beyond.

1. Ensure you have `dotnet (10.0)` installed locally
1. Run `./your_program.sh` to run your Redis server, which is implemented in
   `src/Program.cs`.
1. Run `codecrafters submit` to submit your solution to CodeCrafters. Test
   output will be streamed to your terminal.

# Supported Redis Commands

| Command | Syntax | Description |
|---------|--------|-------------|
| PING | `PING` | Replies with `PONG`. |
| ECHO | `ECHO message` | Returns the provided message. |
| SET | `SET key value [PX milliseconds]` | Stores a string value with an optional expiration. `PX milliseconds` and `EX seconds` are supported. |
| GET | `GET key` | Returns a string value, or null if it is missing or expired. |
| RPUSH | `RPUSH key value [value ...]` | Appends one or more values to a list and returns its length. |
| LPUSH | `LPUSH key value [value ...]` | Prepends one or more values to a list and returns its length. |
| LRANGE | `LRANGE key start stop` | Returns the list elements in the inclusive range. Negative indexes are supported. |
| LLEN | `LLEN key` | Returns the number of elements in a list. |
| LPOP | `LPOP key [count]` | Removes and returns one element, or up to `count` elements. |
| BLPOP | `BLPOP key` | Returns the first available list element immediately, or waits until one is pushed to the key. |
