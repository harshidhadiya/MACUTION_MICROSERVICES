namespace USER.Messaging.Rpc;

public sealed class UserGetRequest
{
    public int UserId { get; set; }
}

public sealed class UserGetResponse
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? Email { get; set; }
    public string? Role { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? ProfilePicture { get; set; }
}
