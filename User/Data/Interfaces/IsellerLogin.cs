using Microsoft.AspNetCore.Mvc;
using USER.Data.Dto;
using USER.Messaging.Rpc;

namespace USER.Data.Interfaces
{
    public interface IsellerLogin
    {
        Task<ActionResult> Login(UserLoginDto loginDto, HttpClient? httpClient = null);
    }

    public interface IadminLogin
    {
        Task<ActionResult> Login(UserLoginDto loginDto, IAdminRpcService adminRpc);
    }
}