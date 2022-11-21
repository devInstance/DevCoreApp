using DevInstance.DevCoreApp.Server.Exceptions;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace DevInstance.DevCoreApp.Server.Controllers
{
    public delegate ActionResult<T> WebHandler<T>();
    public delegate Task<ActionResult<T>> WebHandlerAsync<T>();

    public class BaseController : ControllerBase
    {
        protected BaseController()
        {
        }

        private ActionResult<T> HandleException<T>(Exception ex)
        {
            //TODO: Log error
            return Problem(detail: ex.StackTrace, title: ex.Message);
        }

        protected async Task<ActionResult<T>> HandleWebRequestAsync<T>(WebHandlerAsync<T> handler)
        {
            try
            {
                return await handler();
            }
            catch (RecordNotFoundException)
            {
                return NotFound();
            }
            catch (RecordConflictException)
            {
                return Conflict();
            }
            catch (UnauthorizedException ex)
            {
                return Unauthorized(ex.Message);
            }
            catch (BadRequestException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return HandleException<T>(ex);
            }
        }

        protected ActionResult<T> HandleWebRequest<T>(WebHandler<T> handler)
        {
            try
            {
                return handler();
            }
            catch (RecordNotFoundException)
            {
                return NotFound();
            }
            catch (RecordConflictException)
            {
                return Conflict();
            }
            catch (UnauthorizedException ex)
            {
                return Unauthorized(ex.Message);
            }
            catch (BadRequestException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return HandleException<T>(ex);
            }
        }
    }
}
