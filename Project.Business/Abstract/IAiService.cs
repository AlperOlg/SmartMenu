namespace Project.Business.Abstract;

public interface IAiService
{
    public Task<string> GenerateResponseAsync(string prompt, int currentUserId, CancellationToken cancellationToken = default);

}
