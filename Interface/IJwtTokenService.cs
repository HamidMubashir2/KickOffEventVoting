using KickOffEvent.Models;

namespace KickOffEvent.Interface
{
    public interface IJwtTokenService
    {
        string CreateToken(AppUser user);
    }
}
