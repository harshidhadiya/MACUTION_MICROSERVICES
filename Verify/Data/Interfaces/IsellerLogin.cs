using Microsoft.AspNetCore.Mvc;
using VERIFY.Data.Dto;

namespace VERIFY.Data.Interfaces
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