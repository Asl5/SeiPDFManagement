namespace SeiPDFManagement.Authorization
{
    public interface IUserAuthorizationService
    {
        Task<bool> IsEnabledAsync(string username);
    }
}
