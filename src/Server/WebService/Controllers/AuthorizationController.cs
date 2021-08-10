using DevInstance.SampleWebApp.Server.Exceptions;
using DevInstance.SampleWebApp.Server.Services;
using DevInstance.SampleWebApp.Shared.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace DevInstance.SampleWebApp.Server.Controllers
{
    [Route("api/user/account")]
    [ApiController]
    public class AuthorizationController : BaseController
    {
        AuthorizationService Service { get; }

        public AuthorizationController(AuthorizationService service)
        {
            Service = service;
        }

        [Route("signin")]
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Login(LoginParameters parameters)
        {
            try
            {
                return Ok(await Service.Login(parameters));
            }
            catch (UnauthorizedException ex)
            {
                return Unauthorized(ex.Message);
            }
        }

        [Route("register")]
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Register(RegisterParameters parameters)
        {
            try
            {
                return Ok(await Service.Register(parameters));
            }
            catch (BadRequestException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [Route("signout")]
        [Authorize]
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Logout()
        {
            await Service.Logout();
            return Ok();
        }

        [Route("user-info")]
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public UserInfoItem GetUserInfo()
        {
            return Service.GetUserInfo();
        }

        [HttpDelete]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Delete()
        {
            return Ok(await Service.Delete());
        }

        [Route("change-password")]
        [Authorize]
        [HttpPut]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> ChangePassword(ChangePasswordParameters parameters)
        {
            try
            {
                return Ok(await Service.ChangePassword(parameters));
            }
            catch (UnauthorizedException ex)
            {
                return Unauthorized(ex.Message);
            }
            catch (BadRequestException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [Route("forgot-password")]
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordParameters parameters)
        {
            try
            {
                return Ok(await Service.ForgotPasswordAsync(parameters, this.Request.Scheme, this.Request.Host.Host, this.Request.Host.Port));
            }
            catch (BadRequestException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [Route("reset-password")]
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ResetPassword(ResetPasswordModel parameters)
        {
            try
            {
                return Ok(await Service.ResetPasswordAsync(parameters));
            }
            catch (BadRequestException ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
