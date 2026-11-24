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




| Command | parts[2] | parts[4] | parts[6] | parts[8] | parts[10] |
|----------|----------|----------|----------|----------|-----------|
| PING | PING | - | - | - | - |
| ECHO hey | ECHO | hey | - | - | - |
| GET foo | GET | foo | - | - | - |
| SET foo bar | SET | foo | bar | - | - |
| SET foo bar PX 100 | SET | foo | bar | PX | 100 |
| RPUSH mylist apple | RPUSH | mylist | apple | - | - |
| RPUSH mylist a b c | RPUSH | mylist | a | b | c |




| Index | Meaning |
|---------|---------|
| parts[2] | Command |
| parts[4] | Argument 1 |
| parts[6] | Argument 2 |
| parts[8] | Argument 3 |
| parts[10] | Argument 4 |