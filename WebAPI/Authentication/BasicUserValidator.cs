using System.Security.Claims;
using System.Security.Principal;
using Nancy.Authentication.Basic;

namespace WebAPI.Authentication
{
    public class BasicUserValidator : IUserValidator
    {
        public ClaimsPrincipal Validate(string username, string password)
        {
            // Todo: Implement config
            if (username == "username" && password == "password")
            {
                var principal = new ClaimsPrincipal(new GenericIdentity(username, "Basic"));
                var claimsPrincipal = principal;
                return claimsPrincipal;
            }

            return null;
        }
    }
}
