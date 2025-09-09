using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Pidar.Models;
using System.Diagnostics;

namespace Pidar.Controllers
{
    [ApiExplorerSettings(IgnoreApi = true)]
    public class ErrorController : Controller
    {
        
        [Route("Error/{statusCode}")]
        public IActionResult HttpStatusCodeHandler(int statusCode)
        {
            var errorModel = new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
            };

            switch (statusCode)
            {
                case 404:
                    errorModel.ErrorMessage = "Sorry, the resource you requested could not be found";
                    break;
                case 500:
                    errorModel.ErrorMessage = "Sorry, something went wrong on the server";
                    break;
                default:
                    errorModel.ErrorMessage = $"Sorry, an error occurred (Status Code: {statusCode})";
                    break;
            }

            return View("Error", errorModel);
        }

        [ApiExplorerSettings(IgnoreApi = true)]
        [Route("Error")]
        public IActionResult Error()
        {
            var errorModel = new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
                ErrorMessage = "An unexpected error occurred"
            };

            // Retrieve the actual exception if available
            var exceptionHandlerPathFeature = HttpContext.Features.Get<IExceptionHandlerPathFeature>();
            if (exceptionHandlerPathFeature?.Error != null)
            {
                errorModel.ErrorMessage = exceptionHandlerPathFeature.Error.Message;
            }

            return View("Error", errorModel);
        }
    }
}
