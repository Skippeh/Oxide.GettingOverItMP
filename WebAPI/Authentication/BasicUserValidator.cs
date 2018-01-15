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
            var account = Data.UserCredentials.FirstOrDefault(credentials => credentials.Username.ToLowerInvariant() == username.ToLowerInvariant() && credentials.Password == password);
            if (account != null)
            {
                var principal = new ClaimsPrincipal(new GenericIdentity(account.Username, "Basic"));
                var claimsPrincipal = principal;
                return claimsPrincipal;
            }

            return null;
        }
    }
}
