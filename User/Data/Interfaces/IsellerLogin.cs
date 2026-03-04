using Microsoft.AspNetCore.Mvc;
using USER.Data.Dto;

namespace USER.Data.Interfaces
{
    public interface IsellerLogin
    {
        Task<ActionResult> Login(UserLoginDto loginDto,HttpClient httpClient);

    }
    public interface IadminLogin
    {
        Task<ActionResult> Login(UserLoginDto loginDto,HttpClient httpClient);

    }
}