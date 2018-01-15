using System.Linq;
using System.Security.Claims;
using System.Security.Principal;
using Nancy.Authentication.Basic;

namespace WebAPI.Authentication
{
    public class BasicUserValidator : IUserValidator
    {
        public ClaimsPrincipal Validate(string username, string password)
        {
            UserCredential account = Data.UserCredentials.FirstOrDefault(credentials => credentials.Username.ToLowerInvariant() == username.ToLowerInvariant() && credentials.Password == password);

            if (account != null)
            {
                return new ClaimsPrincipal(new GenericIdentity(account.Username, "Basic"));
            }

            return null;
        }
    }
}
