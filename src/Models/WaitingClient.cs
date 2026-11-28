namespace codecrafters_redis.DTOs;

public record WaitingClient(
    TaskCompletionSource<(string Key, string Value)> Tcs);