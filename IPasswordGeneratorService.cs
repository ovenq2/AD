namespace ADUserManagement.Services.Interfaces
{
    public interface IPasswordGeneratorService
    {
        string GeneratePassword(int length = 12);
    }
}