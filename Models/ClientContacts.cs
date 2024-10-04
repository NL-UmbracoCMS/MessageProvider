namespace MessageProvider.Models;

public class ClientContacts
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string? UserName { get; set; }
    public string Email { get; set; } = null!;
    public string? Phone { get; set; }
    public string? Message { get; set; }
}
