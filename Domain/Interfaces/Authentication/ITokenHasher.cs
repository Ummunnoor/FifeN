namespace Domain.Interfaces.Authentication
{
    public interface ITokenHasher
    {
        string Hash(string token);
    }
}